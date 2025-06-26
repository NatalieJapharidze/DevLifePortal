using DevLife.API.Data;
using DevLife.API.Endpoints;
using DevLife.API.Services;
using Scalar.AspNetCore;

namespace DevLife.API.Extensions
{
    public static class WebApplicationExtensions
    {
        public static async Task InitializeDatabasesAsync(this WebApplication app)
        {
            using var scope = app.Services.CreateScope();
            try
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                await context.Database.EnsureCreatedAsync();
                Console.WriteLine("PostgreSQL database initialized");

                var mongoService = scope.ServiceProvider.GetRequiredService<MongoDbService>();
                await mongoService.SeedDataAsync();
                Console.WriteLine("MongoDB collections seeded");

                var redisService = scope.ServiceProvider.GetRequiredService<RedisService>();
                await redisService.SetCachedDataAsync("test:connection", new { status = "ok" }, TimeSpan.FromMinutes(1));
                Console.WriteLine("Redis connection established");

                var casinoService = scope.ServiceProvider.GetRequiredService<CasinoService>();
                await casinoService.SeedChallengesAsync();
                Console.WriteLine("Casino challenges seeded");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Database initialization error: {ex.Message}");
            }
        }

        public static void ConfigureDocumentation(this WebApplication app)
        {
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "DevLife Portal API v1");
                    c.RoutePrefix = "swagger";
                    c.DocumentTitle = "DevLife Portal API";
                });

                app.MapGet("/", () => Results.Redirect("/swagger"))
                    .WithName("Root")
                    .ExcludeFromDescription();
            }
        }

        public static void MapEndpoints(this WebApplication app)
        {
            app.MapAuthEndpoints();
            app.MapDashboardEndpoints();
            app.MapCasinoEndpoints();
            app.MapDatingEndpoints();
            app.MapSystemEndpoints();
        }
    }
}
