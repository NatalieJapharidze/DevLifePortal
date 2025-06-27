using DevLife.API.Models;
using DevLife.API.Services;

namespace DevLife.API.Endpoints
{
    public static class AuthEndpoints
    {
        public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/api/auth")
                .WithTags("Authentication");

            group.MapPost("/register", RegisterAsync)
                .WithName("Register")
                .WithSummary("Register new user");

            group.MapPost("/login", LoginAsync)
                .WithName("Login")
                .WithSummary("Login user");

            group.MapPost("/logout", LogoutAsync)
                .WithName("Logout")
                .WithSummary("Logout user");
        }

        private static async Task<IResult> RegisterAsync(
            AuthService authService,
            string username,
            string firstName,
            string lastName,
            DateTime birthDate,
            string techStack,
            ExperienceLevel experienceLevel)
        {
            var user = await authService.RegisterAsync(username, firstName, lastName,
                birthDate, techStack, experienceLevel);

            if (user == null)
            {
                return Results.BadRequest(new { message = "Username already exists" });
            }

            var token = await authService.LoginAsync(user.Username);
            return Results.Ok(new { user, token });
        }

        private static async Task<IResult> LoginAsync(AuthService authService, string username)
        {
            var token = await authService.LoginAsync(username);
            if (token == null)
            {
                return Results.BadRequest(new { message = "User not found" });
            }

            var user = await authService.GetUserByTokenAsync(token);
            return Results.Ok(new { user, token });
        }

        private static async Task<IResult> LogoutAsync(HttpContext context, AuthService authService)
        {
            var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "");
            if (string.IsNullOrEmpty(token))
            {
                return Results.Unauthorized();
            }

            var result = await authService.LogoutAsync(token);
            return Results.Ok(new { message = "Logged out successfully", success = result });
        }
    }
}
