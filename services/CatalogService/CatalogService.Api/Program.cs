using CatalogService.Api.Health;
using CatalogService.Api.Middlewares;
using CatalogService.Api.Responses;
using CatalogService.Application.Brands;
using CatalogService.Application.Categories;
using CatalogService.Application.Messaging;
using CatalogService.Application.Messaging.Events;
using CatalogService.Application.Products;
using CatalogService.Infrastructure;
using Elastic.CommonSchema.Serilog; // EcsTextFormatter
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Serilog;
using Serilog.Sinks.Elasticsearch;
using System.Text.Json; // ElasticsearchSinkOptions, AutoRegisterTemplateVersion

try
{
    Log.Information("Starting up CatalogService.Api");

    var builder = WebApplication.CreateBuilder(args);

// Serilog (builder aşaması) 
var esUri = builder.Configuration.GetValue<string>("Serilog:Elasticsearch:Uri"); // örn: http://localhost:9200
var env = builder.Environment.EnvironmentName ?? "Development";


// Basit config: Console + Elasticsearch (ECS formatter)
Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .Enrich.WithEnvironmentName()
    .Enrich.WithMachineName()
    .MinimumLevel.Information()
    .WriteTo.Console(new EcsTextFormatter())
    .WriteTo.Elasticsearch(new ElasticsearchSinkOptions(new Uri(esUri))
    {
        AutoRegisterTemplate = true,
        AutoRegisterTemplateVersion = AutoRegisterTemplateVersion.ESv7,
        IndexFormat = $"ms-catalogservice-{env.ToLowerInvariant()}-{DateTime.UtcNow:yyyy.MM.dd}",
        TypeName = null, // _type alanını gönderme
        CustomFormatter = new EcsTextFormatter(),
        FailureCallback = e => Console.Error.WriteLine($"[ES Sink Failure] {e.MessageTemplate}"),
        EmitEventFailure = EmitEventFailureHandling.WriteToSelfLog
                     | EmitEventFailureHandling.RaiseCallback
    })
    .CreateLogger();

builder.Host.UseSerilog();

var services = builder.Services;

//HealthChecks (Elasticsearch + RabbitMQ)
services.AddHealthChecks()
    .AddCheck<ElasticsearchHealthCheck>("ElasticsearchHealthCheck")
    .AddCheck<RabbitMqHealthCheck>("RabbitMqHealthCheck");

// Scrutor: Application assembly’sindeki *Service ile biten sınıfları otomatik olarak interface’lerine bağla
services.Scan(scan => scan
    .FromAssembliesOf(typeof(IProductService))
    .AddClasses(c => c.Where(type => type.Name.EndsWith("Service")))
    .AsImplementedInterfaces()
    .WithScopedLifetime());

// Infrastructure bağımlılıkları
services.AddInfrastructure(builder.Configuration);

// Minimal API
services.AddEndpointsApiExplorer();
services.AddSwaggerGen();


services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "CatalogService API",
        Version = "v1",
        Description = "Products/Brands/Categories + demo event publish (RabbitMQ). CorrelationId: `X-Correlation-Id`"
    });
});

var app = builder.Build();

// CorrelationId üret → LogEnricher → Exception
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<CorrelationLogEnricherMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseSerilogRequestLogging(); // CorrelationId enrichtment ile uyumlu çalışır

// Swagger sadece dummy fazda açık
app.UseSwagger();
app.UseSwaggerUI();

// Dummy endpoint: GET /api/products
app.MapGet("/api/products", async (IProductService service, HttpContext ctx) =>
{
    var data = await service.GetAllAsync(ctx.RequestAborted);
    var correlationId = ctx.Response.Headers["X-Correlation-Id"].ToString();
    return Results.Ok(ApiResponse<IReadOnlyList<ProductResponse>>.Ok(data, correlationId));
})
.WithName("GetProducts")
.WithSummary("List all products")
.WithTags("Catalog")
.Produces<ApiResponse<IReadOnlyList<ProductResponse>>>(StatusCodes.Status200OK);

// GET /api/brands
app.MapGet("/api/brands", async (IBrandService service, HttpContext ctx) =>
{
    var data = await service.GetAllAsync(ctx.RequestAborted);
    var correlationId = ctx.Response.Headers["X-Correlation-Id"].ToString();
    return Results.Ok(ApiResponse<IReadOnlyList<BrandResponse>>.Ok(data, correlationId));
})
.WithName("GetBrands")
.WithSummary("List all brands")
.WithTags("Catalog")
.Produces<ApiResponse<IReadOnlyList<BrandResponse>>>(StatusCodes.Status200OK);

// GET /api/categories
app.MapGet("/api/categories", async (ICategoryService service, HttpContext ctx) =>
{
    var data = await service.GetAllAsync(ctx.RequestAborted);
    var correlationId = ctx.Response.Headers["X-Correlation-Id"].ToString();
    return Results.Ok(ApiResponse<IReadOnlyList<CategoryResponse>>.Ok(data, correlationId));
})
.WithName("GetCategories")
.WithSummary("List all categories")
.WithTags("Catalog")
.Produces<ApiResponse<IReadOnlyList<CategoryResponse>>>(StatusCodes.Status200OK);

// Demo publish: POST /api/products/{id}/view
app.MapPost("/api/products/{id:int}/view", async (int id, IEventBusPublisher bus, HttpContext ctx) =>
{
    var correlationId = ctx.Response.Headers["X-Correlation-Id"].ToString();
    var @event = new ProductViewedEvent(id, DateTime.UtcNow, correlationId);
    await bus.PublishAsync(@event, key: $"catalog.product.{id}.viewed", correlationId: correlationId, ct: ctx.RequestAborted);
    return Results.Accepted($"/api/products/{id}", ApiResponse<object>.Ok(new { Published = true, ProductId = id }, correlationId));
})
.WithName("PublishProductViewed")
.WithSummary("Publish ProductViewed event (demo)")
.WithTags("Events")
.Produces<ApiResponse<object>>(StatusCodes.Status202Accepted);


// /health/live → sadece liveness (check çalışmaz)
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    // hiçbir check çalıştırma; 200 dönsün
    Predicate = _ => false
});

// /health/ready → ES + RabbitMQ sonuçlarını detaylı JSON yaz
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var payload = new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(e => new {
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

app.Run();

}
catch (Exception ex)
{
    // Startup sırasında fatal error logla
    Log.Fatal(ex, "CatalogService.Api terminated unexpectedly");
}
finally
{
    // Mutlaka flush et
    Log.CloseAndFlush();
}
