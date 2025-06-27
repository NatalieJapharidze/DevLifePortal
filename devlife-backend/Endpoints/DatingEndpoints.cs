using DevLife.API.Services;

namespace DevLife.API.Endpoints
{
    public static class DatingEndpoints
    {
        public static void MapDatingEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/api/dating")
                .WithTags("Dating");

            group.MapGet("/profiles", GetProfilesAsync)
                .WithName("GetProfiles")
                .WithSummary("Get dating profiles")
                .WithDescription("Get developer profiles for dating based on tech stack compatibility");
        }

        private static async Task<IResult> GetProfilesAsync(HttpContext context, AuthService authService, MongoDbService mongoService)
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

            var profiles = await mongoService.GetProfilesByTechStackAsync(user.TechStack, 5);
            return Results.Ok(new { profiles });
        }
    }
}