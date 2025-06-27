using Microsoft.EntityFrameworkCore;
using DevLife.API.Data;
using DevLife.API.Models;

namespace DevLife.API.Services
{
    public class CasinoService
    {
        private readonly AppDbContext _context;
        private readonly MongoDbService _mongoService;
        private readonly RedisService _redisService;
        private readonly GeminiService _geminiService;

        public CasinoService(AppDbContext context, MongoDbService mongoService, RedisService redisService, GeminiService geminiService)
        {
            _context = context;
            _mongoService = mongoService;
            _redisService = redisService;
            _geminiService = geminiService;
        }

        #region Challenge Management

        public async Task<CasinoChallenge?> GetRandomChallengeAsync(string techStack, ExperienceLevel experienceLevel)
        {

            var aiChallenge = await GetAIChallenge(techStack, experienceLevel);
            if (aiChallenge != null)
            {
                Console.WriteLine("✅ Using AI generated challenge");
                return aiChallenge;
            }

            var mongoChallenge = await GetMongoChallenge(techStack, experienceLevel);
            if (mongoChallenge != null)
            {
                Console.WriteLine("✅ Using MongoDB challenge (AI failed)");
                return mongoChallenge;
            }

            var postgresChallenge = await GetPostgresChallenge(techStack, experienceLevel);
            if (postgresChallenge != null)
            {
                Console.WriteLine("✅ Using PostgreSQL challenge (AI and MongoDB failed)");
                return postgresChallenge;
            }

            Console.WriteLine("❌ All challenge sources failed");
            return null;
        }

        private async Task<CasinoChallenge?> GetMongoChallenge(string techStack, ExperienceLevel experienceLevel)
        {
            try
            {
                Console.WriteLine($"📚 Searching MongoDB for {techStack} {experienceLevel} challenge");
                var mongoSnippet = await _mongoService.GetRandomCodeSnippetAsync(techStack, experienceLevel.ToString());

                if (mongoSnippet != null)
                {
                    Console.WriteLine($"✅ MongoDB challenge found: {mongoSnippet.Title}");
                    return new CasinoChallenge
                    {
                        Id = -1,
                        TechStack = mongoSnippet.TechStack,
                        Title = mongoSnippet.Title,
                        Description = mongoSnippet.Description,
                        CodeSnippet1 = mongoSnippet.Code1,
                        CodeSnippet2 = mongoSnippet.Code2,
                        CorrectAnswer = mongoSnippet.CorrectAnswer,
                        Explanation = mongoSnippet.Explanation,
                        Difficulty = Enum.Parse<ExperienceLevel>(mongoSnippet.Difficulty),
                        CreatedAt = DateTime.UtcNow
                    };
                }
                else
                {
                    Console.WriteLine($"❌ No MongoDB challenge found for {techStack} {experienceLevel}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ MongoDB challenge error: {ex.Message}");
            }
            return null;
        }

        private async Task<CasinoChallenge?> GetPostgresChallenge(string techStack, ExperienceLevel experienceLevel)
        {
            try
            {
                Console.WriteLine($"🗄️ Searching PostgreSQL for {techStack} {experienceLevel} challenge");

                var challenges = await _context.CasinoChallenges
                    .Where(c => c.TechStack == techStack && c.Difficulty == experienceLevel)
                    .ToListAsync();

                if (!challenges.Any())
                {
                    Console.WriteLine($"⚠️ No exact match, getting any PostgreSQL challenge");
                    challenges = await _context.CasinoChallenges.ToListAsync();
                }

                if (challenges.Any())
                {
                    var random = new Random();
                    var selected = challenges[random.Next(challenges.Count)];
                    Console.WriteLine($"✅ PostgreSQL challenge found: {selected.Title}");
                    return selected;
                }
                else
                {
                    Console.WriteLine("❌ No PostgreSQL challenges available");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ PostgreSQL challenge error: {ex.Message}");
            }
            return null;
        }

        private async Task<CasinoChallenge?> GetAIChallenge(string techStack, ExperienceLevel experienceLevel)
        {
            try
            {
                var cacheKey = $"ai_challenge_limit:{DateTime.UtcNow:yyyy-MM-dd-HH}";
                var currentCount = await _redisService.GetGameStatsAsync(cacheKey);

                if (currentCount >= 100)
                {
                    Console.WriteLine($"⚠️ AI challenge rate limit reached: {currentCount}/100 this hour");
                    return null;
                }

                Console.WriteLine($"🤖 Generating AI challenge for {techStack} {experienceLevel} ({currentCount}/100 this hour)");
                var aiChallenge = await _geminiService.GenerateCodeChallengeAsync(techStack, experienceLevel);

                if (aiChallenge != null)
                {
                    await _redisService.IncrementGameStatsAsync(cacheKey);

                    aiChallenge.Id = 0;
                    Console.WriteLine($"✅ AI challenge generated: {aiChallenge.Title}");
                    return aiChallenge;
                }
                else
                {
                    Console.WriteLine("❌ AI failed to generate challenge");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ AI challenge error: {ex.Message}");
            }
            return null;
        }

        public async Task<CasinoChallenge?> GetDailyChallengeAsync()
        {
            var today = DateTime.UtcNow.Date;

            var existingDaily = await _context.DailyChallenges
                .Include(d => d.Challenge)
                .FirstOrDefaultAsync(d => d.Date == today && d.IsActive);

            if (existingDaily?.Challenge != null)
            {
                Console.WriteLine("✅ Using existing daily challenge");
                return existingDaily.Challenge;
            }

            CasinoChallenge? selectedChallenge = null;

            selectedChallenge = await GetAIChallenge("JavaScript", ExperienceLevel.Middle);
            if (selectedChallenge != null)
            {
                Console.WriteLine("✅ Daily challenge: AI Generated");
            }
            else
            {
                selectedChallenge = await GetMongoChallenge("JavaScript", ExperienceLevel.Middle);
                if (selectedChallenge != null)
                {
                    Console.WriteLine("✅ Daily challenge: MongoDB (AI failed)");
                }
                else
                {
                    var allChallenges = await _context.CasinoChallenges.ToListAsync();
                    if (allChallenges.Any())
                    {
                        var random = new Random();
                        selectedChallenge = allChallenges[random.Next(allChallenges.Count)];
                        Console.WriteLine("✅ Daily challenge: PostgreSQL (AI and MongoDB failed)");
                    }
                }
            }

            if (selectedChallenge != null)
            {
                var dailyChallenge = new DailyChallenge
                {
                    Date = today,
                    ChallengeId = selectedChallenge.Id,
                    BonusMultiplier = 3,
                    IsActive = true
                };

                _context.DailyChallenges.Add(dailyChallenge);
                await _context.SaveChangesAsync();

                return selectedChallenge;
            }

            Console.WriteLine("❌ All sources failed for daily challenge");
            return null;
        }

        #endregion

        #region Game Play

        public async Task<CasinoGame> PlayGameAsync(int userId, int challengeId, int userAnswer, int betPoints)
        {
            CasinoChallenge? challenge = null;

            if (challengeId == 0)
            {
                throw new ArgumentException("AI challenges should use PlayAIChallengeAsync method");
            }
            else if (challengeId == -1)
            {
                throw new ArgumentException("MongoDB challenges should be passed with challenge data");
            }
            else
            {
                challenge = await _context.CasinoChallenges.FindAsync(challengeId);
                if (challenge == null)
                {
                    throw new ArgumentException("Challenge not found");
                }
            }

            return await ProcessGamePlay(userId, challenge, userAnswer, betPoints, "Casino");
        }

        public async Task<CasinoGame> PlayChallengeAsync(int userId, CasinoChallenge challenge, int userAnswer, int betPoints)
        {
            string gameType = challenge.Id switch
            {
                0 => "AI_Challenge",
                -1 => "MongoDB_Challenge",
                _ => "Casino"
            };

            return await ProcessGamePlay(userId, challenge, userAnswer, betPoints, gameType);
        }

        public async Task<CasinoGame> PlayAIChallengeAsync(int userId, CasinoChallenge aiChallenge, int userAnswer, int betPoints)
        {
            return await ProcessGamePlay(userId, aiChallenge, userAnswer, betPoints, "AI_Challenge");
        }

        private async Task<CasinoGame> ProcessGamePlay(int userId, CasinoChallenge challenge, int userAnswer, int betPoints, string gameType)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                throw new ArgumentException("User not found");
            }

            var currentPoints = await GetUserPointsAsync(userId);
            if (currentPoints < betPoints)
            {
                throw new InvalidOperationException("არ გაქვს საკმარისი points! 💰");
            }

            var isCorrect = userAnswer == challenge.CorrectAnswer;

            var zodiacMultiplier = GetZodiacLuckMultiplier(user.ZodiacSign);
            var basePoints = isCorrect ? betPoints * 2 : -betPoints;
            var finalPoints = (int)(basePoints * zodiacMultiplier);

            if (gameType == "AI_Challenge" && isCorrect)
            {
                finalPoints = (int)(finalPoints * 1.1);
            }

            var today = DateTime.UtcNow.Date;
            var isDailyChallenge = await _context.DailyChallenges
                .AnyAsync(d => d.Date == today && d.ChallengeId == challenge.Id && d.IsActive);

            if (isDailyChallenge && isCorrect)
            {
                finalPoints *= 3;
            }

            var userStats = await GetUserStatsAsync(userId);
            if (isCorrect && userStats.CurrentStreak >= 3)
            {
                var streakBonus = Math.Min(userStats.CurrentStreak / 3, 5);
                finalPoints += (int)(finalPoints * 0.1 * streakBonus);
            }

            var game = new CasinoGame
            {
                UserId = userId,
                ChallengeId = challenge.Id,
                UserAnswer = userAnswer,
                BetPoints = betPoints,
                IsCorrect = isCorrect,
                PointsWon = finalPoints,
                PlayedAt = DateTime.UtcNow
            };

            _context.CasinoGames.Add(game);

            var score = new Score
            {
                UserId = userId,
                GameType = gameType,
                Points = finalPoints,
                CreatedAt = DateTime.UtcNow
            };

            _context.Scores.Add(score);

            await UpdateUserStatsAsync(userId, isCorrect, finalPoints);

            var newPoints = await GetUserPointsAsync(userId);
            await _redisService.CacheUserPointsAsync(userId, newPoints);

            await _redisService.IncrementGameStatsAsync("total_games");
            if (isCorrect)
            {
                await _redisService.IncrementGameStatsAsync("games_won");
            }

            await _context.SaveChangesAsync();
            return game;
        }

        #endregion

        #region Points Management

        public async Task<int> GetUserPointsAsync(int userId)
        {
            var cachedPoints = await _redisService.GetCachedUserPointsAsync(userId);
            if (cachedPoints.HasValue)
            {
                return cachedPoints.Value;
            }

            var totalPoints = await _context.Scores
                .Where(s => s.UserId == userId)
                .SumAsync(s => s.Points);

            await _redisService.CacheUserPointsAsync(userId, totalPoints);

            return totalPoints;
        }

        public async Task<List<object>> GetLeaderboardAsync(int limit = 10)
        {
            var cachedLeaderboard = await _redisService.GetCachedLeaderboardAsync<List<object>>();
            if (cachedLeaderboard != null)
            {
                return cachedLeaderboard;
            }

            var leaderboard = await _context.Scores
                .GroupBy(s => s.UserId)
                .Select(g => new
                {
                    UserId = g.Key,
                    TotalPoints = g.Sum(s => s.Points),
                    GamesPlayed = g.Count(),
                    AIGames = g.Count(s => s.GameType == "AI_Challenge"),
                    MongoGames = g.Count(s => s.GameType == "MongoDB_Challenge"),
                    RegularGames = g.Count(s => s.GameType == "Casino")
                })
                .OrderByDescending(x => x.TotalPoints)
                .Take(limit)
                .ToListAsync();

            var result = new List<object>();
            foreach (var item in leaderboard)
            {
                var user = await _context.Users.FindAsync(item.UserId);
                var stats = await GetUserStatsAsync(item.UserId);

                result.Add(new
                {
                    username = user?.Username ?? "Unknown",
                    firstName = user?.FirstName ?? "Unknown",
                    zodiacSign = user?.ZodiacSign.ToString() ?? "Unknown",
                    totalPoints = item.TotalPoints,
                    gamesPlayed = item.GamesPlayed,
                    aiGames = item.AIGames,
                    mongoGames = item.MongoGames,
                    regularGames = item.RegularGames,
                    currentStreak = stats.CurrentStreak,
                    bestStreak = stats.BestStreak,
                    winRate = stats.TotalGamesPlayed > 0 ?
                        Math.Round((double)stats.GamesWon / stats.TotalGamesPlayed * 100, 1) : 0
                });
            }

            await _redisService.CacheLeaderboardAsync(result);

            return result;
        }

        #endregion

        #region User Statistics

        public async Task<UserStats> GetUserStatsAsync(int userId)
        {
            var stats = await _context.UserStats.FirstOrDefaultAsync(s => s.UserId == userId);

            if (stats == null)
            {
                stats = new UserStats
                {
                    UserId = userId,
                    TotalGamesPlayed = 0,
                    GamesWon = 0,
                    CurrentStreak = 0,
                    BestStreak = 0,
                    TotalPointsEarned = 0,
                    TotalPointsLost = 0,
                    PlayedToday = false,
                    LastPlayedAt = DateTime.UtcNow
                };

                _context.UserStats.Add(stats);
                await _context.SaveChangesAsync();
            }

            return stats;
        }

        public async Task<bool> UpdateUserStatsAsync(int userId, bool won, int pointsChange)
        {
            var stats = await GetUserStatsAsync(userId);
            var today = DateTime.UtcNow.Date;

            if (stats.LastPlayedAt.Date < today)
            {
                stats.PlayedToday = false;
            }

            stats.TotalGamesPlayed++;
            stats.PlayedToday = true;
            stats.LastPlayedAt = DateTime.UtcNow;

            if (won)
            {
                stats.GamesWon++;
                stats.CurrentStreak++;
                stats.TotalPointsEarned += Math.Max(0, pointsChange);

                if (stats.CurrentStreak > stats.BestStreak)
                {
                    stats.BestStreak = stats.CurrentStreak;
                }
            }
            else
            {
                stats.CurrentStreak = 0;
                stats.TotalPointsLost += Math.Abs(Math.Min(0, pointsChange));
            }

            _context.UserStats.Update(stats);
            return await _context.SaveChangesAsync() > 0;
        }

        #endregion

        #region Data Seeding

        public async Task SeedChallengesAsync()
        {
            var existingCount = await _context.CasinoChallenges.CountAsync();
            if (existingCount > 0) return;

            var challenges = new List<CasinoChallenge>
            {
                new()
                {
                    TechStack = "React",
                    Title = "React Hook State Update",
                    Description = "რომელი კოდი მუშაობს სწორად state update-ისთვის?",
                    CodeSnippet1 = @"const [count, setCount] = useState(0);
const increment = () => {
    setCount(count + 1);
    setCount(count + 1);
};",
                    CodeSnippet2 = @"const [count, setCount] = useState(0);
const increment = () => {
    setCount(prev => prev + 1);
    setCount(prev => prev + 1);
};",
                    CorrectAnswer = 2,
                    Explanation = "React state updates are asynchronous. ყოველთვის function form გამოიყენე multiple updates-ისთვის",
                    Difficulty = ExperienceLevel.Junior
                },
                
                new()
                {
                    TechStack = ".NET",
                    Title = "LINQ Query Performance",
                    Description = "რომელი LINQ query უფრო ეფექტურია?",
                    CodeSnippet1 = @"var result = users.Where(u => u.Age > 18)
                        .Select(u => u.Name)
                        .ToList();",
                    CodeSnippet2 = @"var result = users.Select(u => u.Name)
                        .Where(u => users.First(x => x.Name == u).Age > 18)
                        .ToList();",
                    CorrectAnswer = 1,
                    Explanation = "ყოველთვის Where-ის შემდეგ Select გამოიყენე performance-ისთვის",
                    Difficulty = ExperienceLevel.Middle
                },

                new()
                {
                    TechStack = "JavaScript",
                    Title = "Array Method Chaining",
                    Description = "რომელი კოდი დააბრუნებს [2, 4, 6]?",
                    CodeSnippet1 = @"[1, 2, 3, 4, 5]
    .filter(x => x % 2 === 0)
    .map(x => x * 2)",
                    CodeSnippet2 = @"[1, 2, 3, 4, 5]
    .map(x => x * 2)
    .filter(x => x % 2 === 0)",
                    CorrectAnswer = 2,
                    Explanation = "map first doubles all numbers, then filter keeps even ones: [2,4,6,8,10] → [2,4,6,8,10]",
                    Difficulty = ExperienceLevel.Junior
                }
            };

            _context.CasinoChallenges.AddRange(challenges);
            await _context.SaveChangesAsync();

            Console.WriteLine($"Seeded {challenges.Count} casino challenges");
        }

        #endregion

        #region Helper Methods

        private static double GetZodiacLuckMultiplier(ZodiacSign zodiacSign)
        {
            return zodiacSign switch
            {
                ZodiacSign.Leo => 1.3,
                ZodiacSign.Sagittarius => 1.25,
                ZodiacSign.Aries => 1.2,
                ZodiacSign.Gemini => 1.15,
                ZodiacSign.Libra => 1.1,
                ZodiacSign.Aquarius => 1.1,
                ZodiacSign.Scorpio => 1.05,
                ZodiacSign.Pisces => 1.05,
                ZodiacSign.Cancer => 1.05,
                ZodiacSign.Taurus => 1.02,
                ZodiacSign.Capricorn => 1.02,
                ZodiacSign.Virgo => 1.0,
                _ => 1.05
            };
        }

        #endregion
    }
}