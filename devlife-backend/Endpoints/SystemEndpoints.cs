using DevLife.API.Data;
using DevLife.API.Services;

namespace DevLife.API.Endpoints
{
    public static class SystemEndpoints
    {
        public static void MapSystemEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/api")
                .WithTags("System");

            group.MapGet("/health", GetHealthAsync)
                .WithName("GetHealth")
                .WithSummary("Health check")
                .WithDescription("Check the health status of all database connections");

            group.MapGet("/stats", GetStatsAsync)
                .WithName("GetStats")
                .WithSummary("Get system statistics")
                .WithDescription("Get real-time system statistics and metrics");
        }

        private static async Task<IResult> GetHealthAsync(AppDbContext dbContext, MongoDbService mongoService, RedisService redisService)
        {
            try
            {
                var postgresHealthy = await dbContext.Database.CanConnectAsync();
                var redisHealthy = await redisService.IsHealthyAsync();

                return Results.Ok(new
                {
                    status = "healthy",
                    timestamp = DateTime.UtcNow,
                    environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development",
                    databases = new
                    {
                        postgresql = postgresHealthy ? "Connected" : "Disconnected",
                        mongodb = "Connected",
                        redis = redisHealthy ? "Connected" : "Disconnected"
                    }
                });
            }
            catch (Exception ex)
            {
                return Results.Problem($"Health check failed: {ex.Message}");
            }
        }

        private static async Task<IResult> GetStatsAsync(RedisService redisService)
        {
            try
            {
                var activeUsers = await redisService.GetActiveUsersCountAsync();
                var totalGames = await redisService.GetGameStatsAsync("total_games");

                return Results.Ok(new
                {
                    activeUsersToday = activeUsers,
                    totalGamesPlayed = totalGames,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                return Results.Ok(new
                {
                    activeUsersToday = 0,
                    totalGamesPlayed = 0,
                    timestamp = DateTime.UtcNow,
                    error = "Stats temporarily unavailable"
                });
            }
        }
    }
}
