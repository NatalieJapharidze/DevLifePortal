using StackExchange.Redis;
using System.Text.Json;

namespace DevLife.API.Services
{
    public class SessionData
    {
        public int UserId { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class RedisService : IDisposable
    {
        private readonly IDatabase _database;
        private readonly IConnectionMultiplexer _redis;
        private readonly JsonSerializerOptions _jsonOptions;

        public RedisService(IConfiguration configuration)
        {
            try
            {
                var connectionString = configuration.GetConnectionString("Redis") ?? "localhost:6379";
                _redis = ConnectionMultiplexer.Connect(connectionString);
                _database = _redis.GetDatabase();

                _jsonOptions = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = false
                };
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to connect to Redis: {ex.Message}", ex);
            }
        }

        public async Task<bool> SetSessionAsync(string sessionToken, int userId, TimeSpan expiration)
        {
            try
            {
                var sessionData = new SessionData { UserId = userId, CreatedAt = DateTime.UtcNow };
                var json = JsonSerializer.Serialize(sessionData, _jsonOptions);
                return await _database.StringSetAsync($"session:{sessionToken}", json, expiration);
            }
            catch
            {
                return false;
            }
        }

        public async Task<int?> GetUserIdFromSessionAsync(string sessionToken)
        {
            try
            {
                var sessionData = await _database.StringGetAsync($"session:{sessionToken}");
                if (!sessionData.HasValue) return null;

                var data = JsonSerializer.Deserialize<SessionData>(sessionData!, _jsonOptions);
                return data?.UserId;
            }
            catch
            {
                return null;
            }
        }

        public async Task<bool> DeleteSessionAsync(string sessionToken)
        {
            try
            {
                return await _database.KeyDeleteAsync($"session:{sessionToken}");
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> ExtendSessionAsync(string sessionToken, TimeSpan expiration)
        {
            try
            {
                return await _database.KeyExpireAsync($"session:{sessionToken}", expiration);
            }
            catch
            {
                return false;
            }
        }

        public async Task<T?> GetCachedDataAsync<T>(string key) where T : class
        {
            try
            {
                var cachedData = await _database.StringGetAsync(key);
                if (!cachedData.HasValue) return null;

                return JsonSerializer.Deserialize<T>(cachedData!, _jsonOptions);
            }
            catch
            {
                return null;
            }
        }

        public async Task<bool> SetCachedDataAsync<T>(string key, T data, TimeSpan expiration) where T : class
        {
            try
            {
                var json = JsonSerializer.Serialize(data, _jsonOptions);
                return await _database.StringSetAsync(key, json, expiration);
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> DeleteCachedDataAsync(string key)
        {
            try
            {
                return await _database.KeyDeleteAsync(key);
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> CacheUserPointsAsync(int userId, int points)
        {
            try
            {
                return await _database.StringSetAsync($"user:points:{userId}", points.ToString(), TimeSpan.FromMinutes(30));
            }
            catch
            {
                return false;
            }
        }

        public async Task<int?> GetCachedUserPointsAsync(int userId)
        {
            try
            {
                var points = await _database.StringGetAsync($"user:points:{userId}");
                if (points.HasValue && int.TryParse(points, out var result))
                {
                    return result;
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        public async Task<bool> CacheLeaderboardAsync(object leaderboard)
        {
            try
            {
                var json = JsonSerializer.Serialize(leaderboard, _jsonOptions);
                return await _database.StringSetAsync("casino:leaderboard", json, TimeSpan.FromMinutes(10));
            }
            catch
            {
                return false;
            }
        }

        public async Task<T?> GetCachedLeaderboardAsync<T>() where T : class
        {
            try
            {
                return await GetCachedDataAsync<T>("casino:leaderboard");
            }
            catch
            {
                return null;
            }
        }

        public async Task<bool> CacheDailyHoroscopeAsync(string zodiacSign, string horoscope)
        {
            try
            {
                var key = $"horoscope:{zodiacSign}:{DateTime.UtcNow:yyyy-MM-dd}";
                return await _database.StringSetAsync(key, horoscope, TimeSpan.FromHours(24));
            }
            catch
            {
                return false;
            }
        }

        public async Task<string?> GetCachedDailyHoroscopeAsync(string zodiacSign)
        {
            try
            {
                var key = $"horoscope:{zodiacSign}:{DateTime.UtcNow:yyyy-MM-dd}";
                var horoscope = await _database.StringGetAsync(key);
                return horoscope.HasValue ? horoscope.ToString() : null;
            }
            catch
            {
                return null;
            }
        }

        public async Task<bool> CheckRateLimitAsync(int userId, string action, int maxAttempts, TimeSpan window)
        {
            try
            {
                var key = $"ratelimit:{action}:{userId}:{DateTime.UtcNow:yyyy-MM-dd-HH}";
                var current = await _database.StringIncrementAsync(key);

                if (current == 1)
                {
                    await _database.KeyExpireAsync(key, window);
                }

                return current <= maxAttempts;
            }
            catch
            {
                return true;
            }
        }

        public async Task<bool> IncrementGameStatsAsync(string statKey)
        {
            try
            {
                await _database.StringIncrementAsync($"stats:{statKey}");
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<long> GetGameStatsAsync(string statKey)
        {
            try
            {
                var value = await _database.StringGetAsync($"stats:{statKey}");
                if (value.HasValue && long.TryParse(value, out var result))
                {
                    return result;
                }
                return 0;
            }
            catch
            {
                return 0;
            }
        }

        public async Task<bool> MarkUserActiveAsync(int userId)
        {
            try
            {
                var key = $"active:users:{DateTime.UtcNow:yyyy-MM-dd}";
                return await _database.SetAddAsync(key, userId);
            }
            catch
            {
                return false;
            }
        }

        public async Task<long> GetActiveUsersCountAsync()
        {
            try
            {
                var key = $"active:users:{DateTime.UtcNow:yyyy-MM-dd}";
                return await _database.SetLengthAsync(key);
            }
            catch
            {
                return 0;
            }
        }

        public async Task<bool> IsHealthyAsync()
        {
            try
            {
                await _database.PingAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> CleanupExpiredDataAsync()
        {
            try
            {
                return await IsHealthyAsync();
            }
            catch
            {
                return false;
            }
        }

        public void Dispose()
        {
            _redis?.Dispose();
            GC.SuppressFinalize(this);
        }
    }

    public static class RedisSessionExtensions
    {
        public static async Task<bool> CreateUserSessionAsync(this RedisService redis, string sessionToken, int userId)
        {
            return await redis.SetSessionAsync(sessionToken, userId, TimeSpan.FromDays(7));
        }

        public static async Task<bool> ValidateSessionAsync(this RedisService redis, string sessionToken)
        {
            var userId = await redis.GetUserIdFromSessionAsync(sessionToken);
            return userId.HasValue;
        }
    }
}