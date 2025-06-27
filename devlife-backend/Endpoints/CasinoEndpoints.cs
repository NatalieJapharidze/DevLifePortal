using DevLife.API.Services;
using DevLife.API.Models;
using DevLife.API.Models.DTOs;

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
                .WithSummary("Get random coding challenge")
                .WithDescription("Get a random code challenge from MongoDB, PostgreSQL, or AI");

            group.MapGet("/daily-challenge", GetDailyChallengeAsync)
                .WithName("GetDailyChallenge")
                .WithSummary("Get daily challenge")
                .WithDescription("Get today's special challenge with 3x point multiplier");

            group.MapPost("/play", PlayGameAsync)
                .WithName("PlayGame")
                .WithSummary("Play casino game")
                .WithDescription("Submit answer and bet points on any type of challenge");

            group.MapGet("/leaderboard", GetLeaderboardAsync)
                .WithName("GetLeaderboard")
                .WithSummary("Get leaderboard")
                .WithDescription("Get top 10 players with points and statistics");

            group.MapGet("/stats", GetUserStatsAsync)
                .WithName("GetUserStats")
                .WithSummary("Get user statistics")
                .WithDescription("Get detailed statistics for current user");

            group.MapGet("/points", GetUserPointsAsync)
                .WithName("GetUserPoints")
                .WithSummary("Get user points")
                .WithDescription("Get current point balance for user");

            group.MapGet("/test-priority", TestPriorityOrderAsync)
                .WithName("TestPriorityOrder")
                .WithSummary("Test challenge source priority")
                .WithDescription("Show which source is used: AI -> MongoDB -> PostgreSQL");
            group.MapGet("/ai-status", GetAIStatusAsync)
                .WithName("GetAIStatus")
                .WithSummary("Get AI challenge status")
                .WithDescription("Check AI rate limit and usage");
        }
        private static async Task<IResult> TestPriorityOrderAsync(HttpContext context, AuthService authService, CasinoService casinoService)
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

            var testResults = new List<object>();

            for (int i = 0; i < 5; i++)
            {
                var challenge = await casinoService.GetRandomChallengeAsync(user.TechStack, user.ExperienceLevel);

                if (challenge != null)
                {
                    string source = challenge.Id switch
                    {
                        0 => "🤖 AI Generated",
                        -1 => "📚 MongoDB Collection",
                        _ => "🗄️ PostgreSQL Database"
                    };

                    testResults.Add(new
                    {
                        attempt = i + 1,
                        source = source,
                        challengeId = challenge.Id,
                        title = challenge.Title,
                        techStack = challenge.TechStack,
                        difficulty = challenge.Difficulty.ToString()
                    });
                }
                else
                {
                    testResults.Add(new
                    {
                        attempt = i + 1,
                        source = "❌ All sources failed",
                        challengeId = (int?)null,
                        title = "No challenge available",
                        techStack = user.TechStack,
                        difficulty = user.ExperienceLevel.ToString()
                    });
                }
            }

            return Results.Ok(new
            {
                message = "Priority Order: AI → MongoDB → PostgreSQL",
                userTechStack = user.TechStack,
                userLevel = user.ExperienceLevel.ToString(),
                testResults = testResults,
                explanation = new
                {
                    priority1 = "🤖 AI Generated (ID = 0) - Tried first",
                    priority2 = "📚 MongoDB Collection (ID = -1) - If AI fails",
                    priority3 = "🗄️ PostgreSQL Database (ID > 0) - If both fail"
                }
            });
        }

        private static async Task<IResult> GetAIStatusAsync(RedisService redisService)
        {
            try
            {
                var cacheKey = $"ai_challenge_limit:{DateTime.UtcNow:yyyy-MM-dd-HH}";
                var currentCount = await redisService.GetGameStatsAsync(cacheKey);
                var limit = 100;
                var remaining = limit - currentCount;

                return Results.Ok(new
                {
                    aiStatus = new
                    {
                        currentHour = DateTime.UtcNow.ToString("yyyy-MM-dd HH:00"),
                        usedThisHour = currentCount,
                        limitPerHour = limit,
                        remaining = Math.Max(0, remaining),
                        percentageUsed = Math.Round((double)currentCount / limit * 100, 1),
                        available = remaining > 0
                    },
                    message = remaining > 0 ?
                        $"✅ AI available ({remaining} challenges remaining this hour)" :
                        "⚠️ AI rate limit reached, will use MongoDB/PostgreSQL"
                });
            }
            catch (Exception ex)
            {
                return Results.Ok(new
                {
                    aiStatus = "error",
                    message = $"Could not check AI status: {ex.Message}"
                });
            }
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

            string challengeSource = challenge.Id switch
            {
                0 => "AI Generated",
                -1 => "MongoDB Collection",
                _ => "PostgreSQL Database"
            };

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
                    source = challengeSource,
                    isAI = challenge.Id == 0,
                    isMongo = challenge.Id == -1,
                    isPostgres = challenge.Id > 0
                },
                userPoints
            });
        }

        private static async Task<IResult> GetDailyChallengeAsync(HttpContext context, AuthService authService, CasinoService casinoService)
        {
            var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "");
            if (string.IsNullOrEmpty(token)) return Results.Unauthorized();

            var user = await authService.GetUserByTokenAsync(token);
            if (user == null) return Results.Unauthorized();

            var dailyChallenge = await casinoService.GetDailyChallengeAsync();
            var userStats = await casinoService.GetUserStatsAsync(user.Id);

            if (dailyChallenge == null)
            {
                return Results.NotFound(new { message = "No daily challenge available" });
            }

            string challengeSource = dailyChallenge.Id switch
            {
                0 => "AI Generated",
                -1 => "MongoDB Collection",
                _ => "PostgreSQL Database"
            };

            return Results.Ok(new
            {
                challenge = new
                {
                    dailyChallenge.Id,
                    dailyChallenge.Title,
                    dailyChallenge.Description,
                    dailyChallenge.CodeSnippet1,
                    dailyChallenge.CodeSnippet2,
                    dailyChallenge.TechStack,
                    dailyChallenge.Difficulty,
                    source = challengeSource
                },
                bonusMultiplier = 3,
                hasPlayedToday = userStats?.PlayedToday ?? false,
                userPoints = await casinoService.GetUserPointsAsync(user.Id)
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
                    var challenge = new CasinoChallenge
                    {
                        Id = request.ChallengeId,
                        Title = request.ChallengeData.Title,
                        Description = request.ChallengeData.Description,
                        CodeSnippet1 = request.ChallengeData.CodeSnippet1,
                        CodeSnippet2 = request.ChallengeData.CodeSnippet2,
                        CorrectAnswer = request.ChallengeData.CorrectAnswer,
                        Explanation = request.ChallengeData.Explanation,
                        TechStack = request.ChallengeData.TechStack,
                        Difficulty = request.ChallengeData.Difficulty
                    };

                    game = await casinoService.PlayChallengeAsync(user.Id, challenge, request.UserAnswer, request.BetPoints);
                }
                else
                {
                    game = await casinoService.PlayGameAsync(user.Id, request.ChallengeId, request.UserAnswer, request.BetPoints);
                }

                var newPoints = await casinoService.GetUserPointsAsync(user.Id);
                var explanation = request.ChallengeData?.Explanation ?? "Challenge completed!";

                string gameType = request.ChallengeId switch
                {
                    0 => "AI Challenge",
                    -1 => "MongoDB Challenge",
                    _ => "Regular Challenge"
                };

                return Results.Ok(new
                {
                    isCorrect = game.IsCorrect,
                    pointsWon = game.PointsWon,
                    newTotal = newPoints,
                    explanation = explanation,
                    gameType = gameType,
                    message = game.IsCorrect ?
                        $"🎉 გილოცავ! შენ გამოიცანი {gameType}! +{game.PointsWon} points!" :
                        $"😔 ვერ გამოიცანი {gameType}, მომავალში იყავი უფრო ფრთხილი! -{Math.Abs(game.PointsWon)} points"
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

        private static async Task<IResult> GetUserStatsAsync(HttpContext context, AuthService authService, CasinoService casinoService)
        {
            var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "");
            if (string.IsNullOrEmpty(token)) return Results.Unauthorized();

            var user = await authService.GetUserByTokenAsync(token);
            if (user == null) return Results.Unauthorized();

            var stats = await casinoService.GetUserStatsAsync(user.Id);

            return Results.Ok(new
            {
                stats,
                totalPoints = await casinoService.GetUserPointsAsync(user.Id)
            });
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