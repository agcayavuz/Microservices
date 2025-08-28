using StackExchange.Redis;

namespace BasketService.Infrastructure.Redis
{

    internal sealed class DistributedLock : IAsyncDisposable
    {
        private readonly IDatabase _db;
        private readonly string _lockKey;
        private readonly string _token;
        private readonly TimeSpan _expiry;

        private const string ReleaseScript = @"
        if redis.call('GET', KEYS[1]) == ARGV[1] then
            return redis.call('DEL', KEYS[1])
        else
            return 0
        end";

        private DistributedLock(IDatabase db, string lockKey, string token, TimeSpan expiry)
        {
            _db = db;
            _lockKey = lockKey;
            _token = token;
            _expiry = expiry;
        }

        public static async Task<DistributedLock?> AcquireAsync(
            IDatabase db, string lockKey, TimeSpan expiry,
            TimeSpan retryDelay, TimeSpan maxWait)
        {
            var token = Guid.NewGuid().ToString("N");
            var stop = DateTime.UtcNow + maxWait;

            while (DateTime.UtcNow < stop)
            {
                if (await db.StringSetAsync(lockKey, token, expiry, When.NotExists))
                    return new DistributedLock(db, lockKey, token, expiry);

                await Task.Delay(retryDelay);
            }
            return null;
        }

        public async ValueTask DisposeAsync()
        {
            try
            {
                await _db.ScriptEvaluateAsync(
                    ReleaseScript,
                    new RedisKey[] { _lockKey },
                    new RedisValue[] { _token });
            }
            catch { /* ignore */ }
        }
    }
}
