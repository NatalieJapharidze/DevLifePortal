using Microsoft.EntityFrameworkCore;
using DevLife.API.Data;
using DevLife.API.Models;

namespace DevLife.API.Services;

public class CasinoService
{
    private readonly AppDbContext _context;
    private readonly GeminiService _geminiService;

    public CasinoService(AppDbContext context, GeminiService geminiService)
    {
        _context = context;
        _geminiService = geminiService;
    }

    public async Task<CasinoChallenge?> GetRandomChallengeAsync(string techStack, ExperienceLevel experienceLevel)
    {
        try
        {
            var aiChallenge = await _geminiService.GenerateCodeChallengeAsync(techStack, experienceLevel);
            if (aiChallenge != null)
            {
                Console.WriteLine($"✅ Generated AI challenge for {techStack} - {experienceLevel}");
                return aiChallenge;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ AI challenge generation failed: {ex.Message}");
        }

        Console.WriteLine($"🔄 Falling back to static challenges for {techStack} - {experienceLevel}");

        var challenges = await _context.CasinoChallenges
            .Where(c => c.TechStack == techStack && c.Difficulty == experienceLevel)
            .ToListAsync();

        if (!challenges.Any())
        {
            challenges = await _context.CasinoChallenges
                .Where(c => c.TechStack == "General" && c.Difficulty == experienceLevel)
                .ToListAsync();
        }

        if (!challenges.Any())
        {
            challenges = await _context.CasinoChallenges
                .Where(c => c.Difficulty == experienceLevel)
                .ToListAsync();
        }

        if (!challenges.Any()) return null;

        var random = new Random();
        return challenges[random.Next(challenges.Count)];
    }

    public async Task<CasinoGame> PlayGameAsync(int userId, int challengeId, int userAnswer, int betPoints)
    {
        var user = await _context.Users.FindAsync(userId);

        CasinoChallenge? challenge = null;
        if (challengeId > 0)
        {
            challenge = await _context.CasinoChallenges.FindAsync(challengeId);
        }

        if (user == null)
            throw new ArgumentException("User not found");

        var isCorrect = userAnswer == (challenge?.CorrectAnswer ?? userAnswer);
        var pointsWon = isCorrect ? betPoints * 2 : -betPoints;

        var game = new CasinoGame
        {
            UserId = userId,
            ChallengeId = challengeId,
            UserAnswer = userAnswer,
            BetPoints = betPoints,
            IsCorrect = isCorrect,
            PointsWon = pointsWon,
            PlayedAt = DateTime.UtcNow
        };

        _context.CasinoGames.Add(game);

        var existingScore = await _context.Scores
            .FirstOrDefaultAsync(s => s.UserId == userId && s.GameType == "Casino");

        if (existingScore != null)
        {
            existingScore.Points += pointsWon;
        }
        else
        {
            _context.Scores.Add(new Score
            {
                UserId = userId,
                GameType = "Casino",
                Points = Math.Max(0, pointsWon)
            });
        }

        await _context.SaveChangesAsync();
        return game;
    }

    public async Task<CasinoGame> PlayAIChallengeAsync(int userId, CasinoChallenge challenge, int userAnswer, int betPoints)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            throw new ArgumentException("User not found");

        var isCorrect = userAnswer == challenge.CorrectAnswer;
        var pointsWon = isCorrect ? betPoints * 2 : -betPoints;

        var game = new CasinoGame
        {
            UserId = userId,
            ChallengeId = 0,
            UserAnswer = userAnswer,
            BetPoints = betPoints,
            IsCorrect = isCorrect,
            PointsWon = pointsWon,
            PlayedAt = DateTime.UtcNow
        };

        _context.CasinoGames.Add(game);

        var existingScore = await _context.Scores
            .FirstOrDefaultAsync(s => s.UserId == userId && s.GameType == "Casino");

        if (existingScore != null)
        {
            existingScore.Points += pointsWon;
        }
        else
        {
            _context.Scores.Add(new Score
            {
                UserId = userId,
                GameType = "Casino",
                Points = Math.Max(100, 100 + pointsWon)
            });
        }

        await _context.SaveChangesAsync();
        return game;
    }

    public async Task<List<object>> GetLeaderboardAsync()
    {
        return await _context.Scores
            .Where(s => s.GameType == "Casino")
            .Include(s => s.User)
            .OrderByDescending(s => s.Points)
            .Take(10)
            .Select(s => new {
                username = s.User!.Username,
                points = s.Points,
                techStack = s.User.TechStack
            })
            .Cast<object>()
            .ToListAsync();
    }

    public async Task<int> GetUserPointsAsync(int userId)
    {
        var score = await _context.Scores
            .FirstOrDefaultAsync(s => s.UserId == userId && s.GameType == "Casino");

        return score?.Points ?? 100;
    }

    public async Task SeedChallengesAsync()
    {
        if (await _context.CasinoChallenges.AnyAsync()) return;

        var challenges = new List<CasinoChallenge>
        {
            new CasinoChallenge
            {
                TechStack = "React",
                Title = "useState Hook",
                Description = "რომელი კოდი სწორად იყენებს useState-ს?",
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
                Explanation = "useState-ის state updates ასინქრონულია. Function form უფრო სანდოა.",
                Difficulty = ExperienceLevel.Junior
            },
            
            new CasinoChallenge
            {
                TechStack = "JavaScript",
                Title = "Array Methods",
                Description = "რომელი კოდი სწორად აბრუნებს array-ის ყველაზე დიდ ელემენტს?",
                CodeSnippet1 = @"const arr = [1, 5, 3, 9, 2];
const max = Math.max(arr);
console.log(max);",
                CodeSnippet2 = @"const arr = [1, 5, 3, 9, 2];
const max = Math.max(...arr);
console.log(max);",
                CorrectAnswer = 2,
                Explanation = "Math.max spread operator-ს სჭირდება array-ს elements-ებისთვის.",
                Difficulty = ExperienceLevel.Junior
            },

            new CasinoChallenge
            {
                TechStack = "C#",
                Title = "Null Reference",
                Description = "რომელი კოდი დაიცავს null reference exception-ისგან?",
                CodeSnippet1 = @"string text = GetText();
if (text.Length > 0)
{
    Console.WriteLine(text.ToUpper());
}",
                CodeSnippet2 = @"string text = GetText();
if (!string.IsNullOrEmpty(text))
{
    Console.WriteLine(text.ToUpper());
}",
                CorrectAnswer = 2,
                Explanation = "IsNullOrEmpty ამოწმებს null და empty string-ს.",
                Difficulty = ExperienceLevel.Junior
            },

            new CasinoChallenge
            {
                TechStack = "General",
                Title = "Algorithm Complexity",
                Description = "რომელი ალგორითმი უფრო ეფექტურია დიდი array-სთვის?",
                CodeSnippet1 = @"// Linear Search
for (int i = 0; i < arr.length; i++) {
    if (arr[i] == target) return i;
}",
                CodeSnippet2 = @"// Binary Search (sorted array)
int left = 0, right = arr.length - 1;
while (left <= right) {
    int mid = (left + right) / 2;
    if (arr[mid] == target) return mid;
    if (arr[mid] < target) left = mid + 1;
    else right = mid - 1;
}",
                CorrectAnswer = 2,
                Explanation = "Binary Search O(log n), Linear Search O(n). Binary უფრო სწრაფია დიდი datasets-ისთვის.",
                Difficulty = ExperienceLevel.Middle
            }
        };

        await _context.CasinoChallenges.AddRangeAsync(challenges);
        await _context.SaveChangesAsync();
    }
}