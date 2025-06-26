using Microsoft.EntityFrameworkCore;
using DevLife.API.Data;
using DevLife.API.Models;

namespace DevLife.API.Services
{
    public class AuthService
    {
        private readonly AppDbContext _context;
        private readonly RedisService _redisService;

        public AuthService(AppDbContext context, RedisService redisService)
        {
            _context = context;
            _redisService = redisService;
        }

        public async Task<User?> RegisterAsync(string username, string firstName, string lastName,
            DateTime birthDate, string techStack, ExperienceLevel experienceLevel)
        {
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == username);

            if (existingUser != null)
            {
                return null;
            }

            var zodiacSign = CalculateZodiacSign(birthDate);

            var user = new User
            {
                Username = username,
                FirstName = firstName,
                LastName = lastName,
                BirthDate = birthDate,
                TechStack = techStack,
                ExperienceLevel = experienceLevel,
                ZodiacSign = zodiacSign,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var userStats = new UserStats
            {
                UserId = user.Id,
                TotalGamesPlayed = 0,
                GamesWon = 0,
                CurrentStreak = 0,
                BestStreak = 0,
                TotalPointsEarned = 0,
                TotalPointsLost = 0,
                PlayedToday = false,
                LastPlayedAt = DateTime.UtcNow
            };

            _context.UserStats.Add(userStats);

            var initialScore = new Score
            {
                UserId = user.Id,
                GameType = "Initial",
                Points = 100,
                CreatedAt = DateTime.UtcNow
            };

            _context.Scores.Add(initialScore);
            await _context.SaveChangesAsync();

            return user;
        }

        public async Task<string?> LoginAsync(string username)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == username);

            if (user == null)
            {
                return null;
            }

            var sessionToken = Guid.NewGuid().ToString();

            await _redisService.CreateUserSessionAsync(sessionToken, user.Id);

            var session = new Session
            {
                UserId = user.Id,
                SessionToken = sessionToken,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                CreatedAt = DateTime.UtcNow
            };

            _context.Sessions.Add(session);
            await _context.SaveChangesAsync();

            return sessionToken;
        }

        public async Task<User?> GetUserByTokenAsync(string sessionToken)
        {
            var userIdFromRedis = await _redisService.GetUserIdFromSessionAsync(sessionToken);

            if (userIdFromRedis.HasValue)
            {
                var cachedUser = await _redisService.GetCachedDataAsync<User>($"user:{userIdFromRedis.Value}");
                if (cachedUser != null)
                {
                    return cachedUser;
                }

                var user = await _context.Users.FindAsync(userIdFromRedis.Value);
                if (user != null)
                {
                    await _redisService.SetCachedDataAsync($"user:{user.Id}", user, TimeSpan.FromMinutes(30));
                }
                return user;
            }

            var session = await _context.Sessions
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.SessionToken == sessionToken && s.ExpiresAt > DateTime.UtcNow);

            if (session?.User != null)
            {
                await _redisService.CreateUserSessionAsync(sessionToken, session.User.Id);

                await _redisService.SetCachedDataAsync($"user:{session.User.Id}", session.User, TimeSpan.FromMinutes(30));
            }

            return session?.User;
        }

        public async Task<bool> LogoutAsync(string sessionToken)
        {
            await _redisService.DeleteSessionAsync(sessionToken);

            var session = await _context.Sessions
                .FirstOrDefaultAsync(s => s.SessionToken == sessionToken);

            if (session != null)
            {
                _context.Sessions.Remove(session);
                await _context.SaveChangesAsync();
                return true;
            }

            return false;
        }

        public async Task<bool> ExtendSessionAsync(string sessionToken)
        {
            var extended = await _redisService.ExtendSessionAsync(sessionToken, TimeSpan.FromDays(7));

            var session = await _context.Sessions
                .FirstOrDefaultAsync(s => s.SessionToken == sessionToken);

            if (session != null)
            {
                session.ExpiresAt = DateTime.UtcNow.AddDays(7);
                await _context.SaveChangesAsync();
            }

            return extended;
        }

        public async Task<List<User>> GetAllUsersAsync()
        {
            return await _context.Users.ToListAsync();
        }

        public async Task<User?> GetUserByIdAsync(int userId)
        {
            var cachedUser = await _redisService.GetCachedDataAsync<User>($"user:{userId}");
            if (cachedUser != null)
            {
                return cachedUser;
            }

            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                await _redisService.SetCachedDataAsync($"user:{userId}", user, TimeSpan.FromMinutes(30));
            }

            return user;
        }

        public async Task<bool> UpdateUserAsync(User user)
        {
            _context.Users.Update(user);
            var result = await _context.SaveChangesAsync() > 0;

            if (result)
            {
                await _redisService.SetCachedDataAsync($"user:{user.Id}", user, TimeSpan.FromMinutes(30));
            }

            return result;
        }

        private static ZodiacSign CalculateZodiacSign(DateTime birthDate)
        {
            var month = birthDate.Month;
            var day = birthDate.Day;

            return (month, day) switch
            {
                (3, >= 21) or (4, <= 19) => ZodiacSign.Aries,
                (4, >= 20) or (5, <= 20) => ZodiacSign.Taurus,
                (5, >= 21) or (6, <= 20) => ZodiacSign.Gemini,
                (6, >= 21) or (7, <= 22) => ZodiacSign.Cancer,
                (7, >= 23) or (8, <= 22) => ZodiacSign.Leo,
                (8, >= 23) or (9, <= 22) => ZodiacSign.Virgo,
                (9, >= 23) or (10, <= 22) => ZodiacSign.Libra,
                (10, >= 23) or (11, <= 21) => ZodiacSign.Scorpio,
                (11, >= 22) or (12, <= 21) => ZodiacSign.Sagittarius,
                (12, >= 22) or (1, <= 19) => ZodiacSign.Capricorn,
                (1, >= 20) or (2, <= 18) => ZodiacSign.Aquarius,
                (2, >= 19) or (3, <= 20) => ZodiacSign.Pisces,
                _ => ZodiacSign.Aries
            };
        }

        public async Task<int> CleanupExpiredSessionsAsync()
        {
            var expiredSessions = await _context.Sessions
                .Where(s => s.ExpiresAt < DateTime.UtcNow)
                .ToListAsync();

            if (expiredSessions.Any())
            {
                _context.Sessions.RemoveRange(expiredSessions);
                return await _context.SaveChangesAsync();
            }

            return 0;
        }

        public async Task<UserStats?> GetUserStatsAsync(int userId)
        {
            return await _context.UserStats
                .FirstOrDefaultAsync(s => s.UserId == userId);
        }

        public async Task<bool> UpdateUserStatsAsync(UserStats stats)
        {
            _context.UserStats.Update(stats);
            return await _context.SaveChangesAsync() > 0;
        }

        public static double GetLuckMultiplier(ZodiacSign zodiacSign)
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
                ZodiacSign.Virgo => 1.0,
                _ => 1.05
            };
        }
    }

    
}