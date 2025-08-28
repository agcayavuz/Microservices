using BasketService.Api.Common;
using BasketService.Api.Health;
using BasketService.Api.Middleware;
using BasketService.Application.Interfaces;
using BasketService.Application.Options;
using BasketService.Contracts.Requests;
using BasketService.Infrastructure.Services;
using Elastic.CommonSchema.Serilog;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Sinks.Elasticsearch;
using StackExchange.Redis;
using System.Text.Json;

try
{
    #region Logging (Serilog + ECS + Elasticsearch)
    Log.Information("Starting up BasketService.Api");

    var builder = WebApplication.CreateBuilder(args);

    var esUri = builder.Configuration.GetValue<string>("Serilog:Elasticsearch:Uri");
    var env = builder.Environment.EnvironmentName ?? "Development";

    Log.Logger = new LoggerConfiguration()
        .Enrich.FromLogContext()
        .Enrich.WithEnvironmentName()
        .Enrich.WithMachineName()
        .Enrich.WithProperty("Service", "BasketService")
        .MinimumLevel.Information()
        .WriteTo.Console(new EcsTextFormatter())
        .WriteTo.Elasticsearch(new ElasticsearchSinkOptions(new Uri(esUri))
        {
            AutoRegisterTemplate = true,
            AutoRegisterTemplateVersion = AutoRegisterTemplateVersion.ESv7,
            IndexFormat = $"ms-basketservice-{env.ToLowerInvariant()}-{DateTime.UtcNow:yyyy.MM.dd}",
            TypeName = null,
            CustomFormatter = new EcsTextFormatter(),
            FailureCallback = e => Console.Error.WriteLine($"[ES Sink Failure] {e.MessageTemplate}"),
            EmitEventFailure = EmitEventFailureHandling.WriteToSelfLog | EmitEventFailureHandling.RaiseCallback
        })
        .CreateLogger();

    builder.Host.UseSerilog();
    #endregion

    #region Services (DI, Options, HealthChecks, Swagger)
    var services = builder.Services;

    // Redis connection (singleton)
    services.AddSingleton<IConnectionMultiplexer>(sp =>
    {
        var cfg = sp.GetRequiredService<IConfiguration>();
        var cs = cfg.GetValue<string>("Redis:ConnectionString")!;
        return ConnectionMultiplexer.Connect(cs);
    });

    // FluentValidation: validator’ları tara (Application assembly’sinden)
    services.AddValidatorsFromAssembly(typeof(BasketService.Application.Validation.BasketItemDtoValidator).Assembly);

    // Options
    services.Configure<BasketOptions>(builder.Configuration.GetSection("Basket"));

    // Basket service → Redis implementasyonu (tek kayıt)
    services.AddSingleton<IBasketService, RedisBasketService>();

    // Scrutor: Application assembly'sindeki *Service sınıflarını tara;
    // RedisBasketService'i hariç tut (zaten explicit singleton kaydettik)
    services.Scan(scan => scan
        .FromAssembliesOf(typeof(IBasketService))
        .AddClasses(c => c.Where(t =>
            t.Name.EndsWith("Service") &&
            t != typeof(RedisBasketService)))
        .AsImplementedInterfaces()
        .WithScopedLifetime());

    // HttpClient (ES health check için)
    services.AddHttpClient(nameof(ElasticsearchHealthCheck), (sp, client) =>
    {
        var cfg2 = sp.GetRequiredService<IConfiguration>();
        var esUri2 = cfg2.GetValue<string>("Serilog:Elasticsearch:Uri");
        if (!string.IsNullOrWhiteSpace(esUri2))
            client.BaseAddress = new Uri(esUri2);
        client.Timeout = TimeSpan.FromSeconds(2);
    });

    // HealthChecks
    services.AddHealthChecks()
        .AddCheck<ElasticsearchHealthCheck>("ElasticsearchHealthCheck")
        .AddCheck<RedisHealthCheck>("RedisHealthCheck");

    // Minimal API + Swagger
    services.AddEndpointsApiExplorer();
    services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "BasketService API",
            Version = "v1",
            Description = "Basket operations (Redis-backed). CorrelationId header: `X-Correlation-Id`"
        });
    });
    #endregion

    var app = builder.Build();

    #region Middleware (order matters)
    app.UseMiddleware<CorrelationIdMiddleware>();            // CorrelationId üret/yanıta yaz
    app.UseMiddleware<CorrelationLogEnricherMiddleware>();   // Log context'e correlation_id ekle
    app.UseMiddleware<ExceptionHandlingMiddleware>();        // Global hata → tek tip response
    app.UseMiddleware<RequestResponseLoggingMiddleware>();   // İstek/yanıt log (konfige bağlı)
    app.UseSerilogRequestLogging();

    app.UseSwagger();
    app.UseSwaggerUI();
    #endregion

    #region Endpoints (v1)
    var group = app.MapGroup("/api/v1/baskets");

    // GET /{customerId} → sepete bak
    group.MapGet("/{customerId}", async (string customerId, IBasketService svc, HttpContext ctx) =>
    {
        var data = await svc.GetAsync(customerId, ctx.RequestAborted);
        var cid = ctx.Response.Headers[CorrelationIdMiddleware.HeaderName].ToString();

        return data is null
            ? Results.NotFound(ApiResponse<object>.Fail("not_found", "Basket bulunamadı", new { customerId }, cid))
            : Results.Ok(ApiResponse<object>.Ok(data, cid));
    })
    .WithName("GetBasket")
    .WithSummary("Get basket by customerId")
    .WithTags("Basket")
    .Produces<ApiResponse<object>>(StatusCodes.Status200OK)
    .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound);

    // POST / → "Sepete Ekle" (Merge: varsa miktarı artırır; yoksa ekler)
    group.MapPost("/", async (CreateOrReplaceBasketRequest req, IBasketService svc, HttpContext ctx) =>
    {
        var data = await svc.CreateOrReplaceAsync(req.CustomerId, req.Items, ctx.RequestAborted);
        var cid = ctx.Response.Headers[CorrelationIdMiddleware.HeaderName].ToString();
        return Results.Ok(ApiResponse<object>.Ok(data, cid));
    })
    .AddEndpointFilter<ValidationFilter<CreateOrReplaceBasketRequest>>()
    .WithName("CreateOrReplaceBasket")
    .WithSummary("Sepete ekle: Ürün varsa miktarı artırır; yoksa yeni ekler (varsayılan davranış: Merge).")
    .WithTags("Basket")
    .Produces<ApiResponse<object>>(StatusCodes.Status200OK)
    .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest);

    // PUT /{customerId}/items → OVERWRITE/SET (idempotent)
    group.MapPut("/{customerId}/items",
        async (string customerId,
               UpsertBasketItemsRequest req,
               IBasketService svc,
               HttpContext ctx) =>
        {
            var data = await svc.UpsertItemsAsync(customerId, req.Items, ctx.RequestAborted);
            var cid = ctx.Response.Headers[CorrelationIdMiddleware.HeaderName].ToString();
            return Results.Ok(ApiResponse<object>.Ok(data, cid));
        })
    .AddEndpointFilter<ValidationFilter<UpsertBasketItemsRequest>>()
    .WithName("UpsertBasketItems")
    .WithSummary("Sepetteki ürün listesini OVERWRITE/SET eder (idempotent).")
    .WithTags("Basket")
    .Produces<ApiResponse<object>>(StatusCodes.Status200OK)
    .AddEndpointFilter<ValidationFilter<UpsertBasketItemsRequest>>();

    // DELETE /{customerId} → sepeti sil
    group.MapDelete("/{customerId}", async (string customerId, IBasketService svc, HttpContext ctx) =>
    {
        var ok = await svc.DeleteAsync(customerId, ctx.RequestAborted);
        var cid = ctx.Response.Headers[CorrelationIdMiddleware.HeaderName].ToString();

        return ok
            ? Results.Ok(ApiResponse<object>.Ok(new { deleted = true }, cid))
            : Results.NotFound(ApiResponse<object>.Fail("not_found", "Silinecek sepet bulunamadı", new { customerId }, cid));
    })
    .WithName("DeleteBasket")
    .WithSummary("Delete basket")
    .WithTags("Basket")
    .Produces<ApiResponse<object>>(StatusCodes.Status200OK)
    .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound);

    // DELETE /{customerId}/items/{productId} → ürünü tamamen kaldır
    group.MapDelete("/{customerId}/items/{productId}", async (string customerId, string productId, IBasketService svc, HttpContext ctx) =>
    {
        var ok = await svc.RemoveItemAsync(customerId, productId, ctx.RequestAborted);
        var cid = ctx.Response.Headers[CorrelationIdMiddleware.HeaderName].ToString();

        return ok
            ? Results.Ok(ApiResponse<object>.Ok(new { removed = true }, cid))
            : Results.NotFound(ApiResponse<object>.Fail("not_found", "Ürün sepet içinde bulunamadı", new { customerId, productId }, cid));
    })
    .WithName("RemoveBasketItem")
    .WithSummary("Remove item from basket")
    .WithTags("Basket")
    .Produces<ApiResponse<object>>(StatusCodes.Status200OK)
    .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound);

    // POST /{customerId}/items/{productId}:decrease → miktar azalt (0 ise kaldır)
    group.MapPost("/{customerId}/items/{productId}:decrease",
        async (string customerId,
               string productId,
               ChangeQuantityRequest req,
               IBasketService svc,
               HttpContext ctx) =>
        {
            var ok = await svc.DecreaseItemQuantityAsync(customerId, productId, req.Quantity, ctx.RequestAborted);
            var cid = ctx.Response.Headers[CorrelationIdMiddleware.HeaderName].ToString();

            return ok
                ? Results.Ok(ApiResponse<object>.Ok(new { decreased = req.Quantity, productId }, cid))
                : Results.NotFound(ApiResponse<object>.Fail("not_found",
                    "Ürün bulunamadı veya azaltılamadı",
                    new { customerId, productId, req.Quantity }, cid));
        })
    .AddEndpointFilter<ValidationFilter<ChangeQuantityRequest>>()
    .WithName("DecreaseBasketItemQuantity")
    .WithSummary("Sepetteki ürün miktarını azaltır; 0'a inerse ürünü kaldırır.")
    .WithTags("Basket")
    .Produces<ApiResponse<object>>(StatusCodes.Status200OK)
    .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest)
    .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound);

    // POST /{customerId}/items/{productId}:increase → miktar artır
    group.MapPost("/{customerId}/items/{productId}:increase",
        async (string customerId,
               string productId,
               ChangeQuantityRequest req,
               IBasketService svc, HttpContext ctx) =>
        {
            var ok = await svc.IncreaseItemQuantityAsync(customerId, productId, req.Quantity, ctx.RequestAborted);
            var cid = ctx.Response.Headers[CorrelationIdMiddleware.HeaderName].ToString();

            return ok
                ? Results.Ok(ApiResponse<object>.Ok(new { increased = req.Quantity, productId }, cid))
                : Results.NotFound(ApiResponse<object>.Fail("not_found",
                    "Ürün bulunamadı veya artırılamadı",
                    new { customerId, productId, req.Quantity }, cid));
        })
    .AddEndpointFilter<ValidationFilter<ChangeQuantityRequest>>()
    .WithName("IncreaseBasketItemQuantity")
    .WithSummary("Sepetteki ürün miktarını artırır.")
    .WithTags("Basket")
    .Produces<ApiResponse<object>>(StatusCodes.Status200OK)
    .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest)
    .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound);
    #endregion

    #region Health endpoints
    app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });

    app.MapHealthChecks("/health/ready", new HealthCheckOptions
    {
        ResponseWriter = async (context, report) =>
        {
            context.Response.ContentType = "application/json";
            var payload = new
            {
                status = report.Status.ToString(),
                checks = report.Entries.Select(e => new
                {
                    name = e.Key,
                    status = e.Value.Status.ToString(),
                    error = e.Value.Exception?.Message,
                    durationMs = e.Value.Duration.TotalMilliseconds
                }),
                totalDurationMs = report.TotalDuration.TotalMilliseconds
            };
            await context.Response.WriteAsync(JsonSerializer.Serialize(payload));
        }
    });
    #endregion

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "BasketService.Api terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
