using DevLife.API.Services;

namespace DevLife.API.Endpoints
{
    public static class DashboardEndpoints
    {
        public static void MapDashboardEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/api")
                .WithTags("Dashboard");

            group.MapGet("/dashboard", GetDashboardAsync)
                .WithName("GetDashboard")
                .WithSummary("Get user dashboard")
                .WithDescription("Get personalized dashboard with horoscope and welcome message");
        }

        private static async Task<IResult> GetDashboardAsync(HttpContext context, AuthService authService, RedisService redisService)
        {
            var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "");
            if (string.IsNullOrEmpty(token))
            {
                return Results.Unauthorized();
            }

            var user = await authService.GetUserByTokenAsync(token);
            if (user == null)
            {
                return Results.Unauthorized();
            }

            await redisService.MarkUserActiveAsync(user.Id);

            var cachedHoroscope = await redisService.GetCachedDailyHoroscopeAsync(user.ZodiacSign.ToString());
            string horoscope;

            if (cachedHoroscope != null)
            {
                horoscope = cachedHoroscope;
            }
            else
            {
                horoscope = ZodiacService.GetDailyHoroscope(user.ZodiacSign);
                await redisService.CacheDailyHoroscopeAsync(user.ZodiacSign.ToString(), horoscope);
            }

            var welcomeMessage = $"გამარჯობა {user.FirstName}! {user.ZodiacSign}, დღეს {horoscope}";

            return Results.Ok(new
            {
                welcomeMessage,
                user,
                dailyHoroscope = horoscope,
                luckyTech = ZodiacService.GetLuckyTechnology()
            });
        }
    }
}
