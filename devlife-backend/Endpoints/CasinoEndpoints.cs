using DevLife.API.Models.DTOs;
using DevLife.API.Models;
using DevLife.API.Services;

namespace DevLife.API.Endpoints
{
    public static class CasinoEndpoints
    {
        public static void MapCasinoEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/api/casino")
                .WithTags("Casino")
                .WithOpenApi();

            group.MapGet("/challenge", GetChallengeAsync)
                .WithName("GetChallenge")
                .WithSummary("Get random coding challenge");

            group.MapPost("/play", PlayGameAsync)
                .WithName("PlayGame")
                .WithSummary("Play casino game");

            group.MapGet("/leaderboard", GetLeaderboardAsync)
                .WithName("GetLeaderboard")
                .WithSummary("Get leaderboard");

            group.MapGet("/points", GetUserPointsAsync)
                .WithName("GetUserPoints")
                .WithSummary("Get user points");
        }

        private static async Task<IResult> GetChallengeAsync(HttpContext context, AuthService authService, CasinoService casinoService)
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

            var challenge = await casinoService.GetRandomChallengeAsync(user.TechStack, user.ExperienceLevel);
            if (challenge == null)
            {
                return Results.NotFound(new { message = "No challenges available" });
            }

            var userPoints = await casinoService.GetUserPointsAsync(user.Id);

            return Results.Ok(new
            {
                challenge = new
                {
                    challenge.Id,
                    challenge.Title,
                    challenge.Description,
                    challenge.CodeSnippet1,
                    challenge.CodeSnippet2,
                    challenge.TechStack,
                    challenge.Difficulty,
                    isAI = challenge.Id == 0
                },
                userPoints
            });
        }

        private static async Task<IResult> PlayGameAsync(HttpContext context, AuthService authService, CasinoService casinoService, PlayCasinoRequest request)
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

            var currentPoints = await casinoService.GetUserPointsAsync(user.Id);
            if (currentPoints < request.BetPoints)
            {
                return Results.BadRequest(new { message = "არ გაქვს საკმარისი points! 💰" });
            }

            try
            {
                CasinoGame game;

                if (request.ChallengeData != null)
                {
                    var aiChallenge = new CasinoChallenge
                    {
                        Id = 0,
                        Title = request.ChallengeData.Title,
                        Description = request.ChallengeData.Description,
                        CodeSnippet1 = request.ChallengeData.CodeSnippet1,
                        CodeSnippet2 = request.ChallengeData.CodeSnippet2,
                        CorrectAnswer = request.ChallengeData.CorrectAnswer,
                        Explanation = request.ChallengeData.Explanation,
                        TechStack = request.ChallengeData.TechStack,
                        Difficulty = request.ChallengeData.Difficulty
                    };

                    game = await casinoService.PlayAIChallengeAsync(user.Id, aiChallenge, request.UserAnswer, request.BetPoints);
                }
                else
                {
                    game = await casinoService.PlayGameAsync(user.Id, request.ChallengeId, request.UserAnswer, request.BetPoints);
                }

                var newPoints = await casinoService.GetUserPointsAsync(user.Id);
                var explanation = request.ChallengeData?.Explanation ?? "Challenge completed!";

                return Results.Ok(new
                {
                    isCorrect = game.IsCorrect,
                    pointsWon = game.PointsWon,
                    newTotal = newPoints,
                    explanation = explanation,
                    message = game.IsCorrect ?
                        "🎉 გილოცავ! შენ გამოიცანი! +" + game.PointsWon + " points!" :
                        "😔 ვერ გამოიცანი, მომავალში იყავი უფრო ფრთხილი! -" + Math.Abs(game.PointsWon) + " points"
                });
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
        }

        private static async Task<IResult> GetLeaderboardAsync(CasinoService casinoService, RedisService redisService)
        {
            var cachedLeaderboard = await redisService.GetCachedLeaderboardAsync<object>();
            if (cachedLeaderboard != null)
            {
                return Results.Ok(new { leaderboard = cachedLeaderboard });
            }

            var leaderboard = await casinoService.GetLeaderboardAsync();
            await redisService.CacheLeaderboardAsync(leaderboard);

            return Results.Ok(new { leaderboard });
        }

        private static async Task<IResult> GetUserPointsAsync(HttpContext context, AuthService authService, CasinoService casinoService)
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

            var points = await casinoService.GetUserPointsAsync(user.Id);
            return Results.Ok(new { points });
        }
    }
}
