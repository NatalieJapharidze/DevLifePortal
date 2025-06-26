using System.Text.Json;
using DevLife.API.Models;
using GenerativeAI;

namespace DevLife.API.Services;

public class GeminiService
{
    private readonly GoogleAi _client;
    private readonly GenerativeModel _model;

    public GeminiService()
    {
        var apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");

        if (string.IsNullOrEmpty(apiKey))
        {
            throw new InvalidOperationException("GEMINI_API_KEY environment variable not set. Please add it to your .env file.");
        }

        _client = new GoogleAi(apiKey);
        _model = _client.CreateGenerativeModel("gemini-1.5-flash");
    }

    public async Task<CasinoChallenge?> GenerateCodeChallengeAsync(string techStack, ExperienceLevel difficulty)
    {
        try
        {
            var prompt = CreatePrompt(techStack, difficulty);
            var response = await _model.GenerateContentAsync(prompt);

            if (response?.Text == null) return null;

            return ParseChallengeResponse(response.Text, techStack, difficulty);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Gemini API Error: {ex.Message}");
            return null;
        }
    }

    private string CreatePrompt(string techStack, ExperienceLevel difficulty)
    {
        var difficultyLevel = difficulty switch
        {
            ExperienceLevel.Junior => "მარტივი (Junior დონე)",
            ExperienceLevel.Middle => "საშუალო (Middle დონე)",
            ExperienceLevel.Senior => "რთული (Senior დონე)",
            _ => "საშუალო"
        };

        return $@"
შექმენი კოდის გამოცნობის თამაში {techStack} ტექნოლოგიისთვის, {difficultyLevel} დონის დეველოპერებისთვის.

Format შემდეგნაირად უნდა იყოს JSON-ში:
{{
    ""title"": ""მოკლე სათაური"",
    ""description"": ""აღწერა ქართულად"",
    ""codeSnippet1"": ""პირველი კოდის ნაწყვეტი"",
    ""codeSnippet2"": ""მეორე კოდის ნაწყვეტი"",
    ""correctAnswer"": 1 ან 2,
    ""explanation"": ""ახსნა ქართულად რატომ არის ესა თუ ის პასუხი სწორი""
}}

მოთხოვნები:
1. ერთი კოდი უნდა იყოს სწორი, მეორე - მცდარი ან inefficient
2. მცდარობა უნდა იყოს subtle, არა აშკარა syntax error
3. explanation უნდა იყოს ქართულად და educational
4. კოდები უნდა იყოს realistic და practical
5. ფოკუსი იყოს common mistakes-ზე რომლებსაც {difficulty} დონის დეველოპერები შეიძლება დაუშვან

Technology: {techStack}
Difficulty: {difficultyLevel}

შექმენი ახალი და საინტერესო challenge.";
    }

    private CasinoChallenge? ParseChallengeResponse(string response, string techStack, ExperienceLevel difficulty)
    {
        try
        {
            var cleanResponse = response.Trim();
            if (cleanResponse.StartsWith("```json"))
            {
                cleanResponse = cleanResponse.Substring(7);
            }
            if (cleanResponse.EndsWith("```"))
            {
                cleanResponse = cleanResponse.Substring(0, cleanResponse.Length - 3);
            }
            cleanResponse = cleanResponse.Trim();

            var challengeData = JsonSerializer.Deserialize<JsonElement>(cleanResponse);

            return new CasinoChallenge
            {
                TechStack = techStack,
                Title = challengeData.GetProperty("title").GetString() ?? "AI Generated Challenge",
                Description = challengeData.GetProperty("description").GetString() ?? "",
                CodeSnippet1 = challengeData.GetProperty("codeSnippet1").GetString() ?? "",
                CodeSnippet2 = challengeData.GetProperty("codeSnippet2").GetString() ?? "",
                CorrectAnswer = challengeData.GetProperty("correctAnswer").GetInt32(),
                Explanation = challengeData.GetProperty("explanation").GetString() ?? "",
                Difficulty = difficulty
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to parse Gemini response: {ex.Message}");
            Console.WriteLine($"Response was: {response}");
            return null;
        }
    }

    public async Task<string> GenerateRoastAsync(string code, string language, bool isGood)
    {
        try
        {
            var prompt = isGood ? CreatePraisePrompt(code, language) : CreateRoastPrompt(code, language);
            var response = await _model.GenerateContentAsync(prompt);
            return response?.Text ?? (isGood ? "კარგი კოდია! 👍" : "ეს კოდი შეიძლება გაუმჯობესდეს 🤔");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Gemini Roast Error: {ex.Message}");
            return isGood ? "კარგი კოდია! 👍" : "ეს კოდი შეიძლება გაუმჯობესდეს 🤔";
        }
    }

    private string CreateRoastPrompt(string code, string language)
    {
        return $@"
ქართველი Senior დეველოპერის როლში, გააკეთე ამ კოდის სახალისო, მაგრამ educational roast.

კოდი ({language}):
```
{code}
```

მოთხოვნები:
1. იყო სახალისო და დისამ ქართულად  
2. არ იყო ძალიან აგრესიული
3. მიუთითე კონკრეტულ problems-ზე
4. ჩართე coding humor
5. მოკლე იყო (1-2 წინადადება)

მაგალითი style: ""ეს კოდი ისე ცუდია, კომპილატორმა დეპრესია დაიწყო 😅""
";
    }

    private string CreatePraisePrompt(string code, string language)
    {
        return $@"
ქართველი Senior დეველოპერის როლში, შექება ეს კოდი სახალისო სტილით.

კოდი ({language}):
```
{code}
```

მოთხოვნები:
1. იყო positive და encouraging
2. ქართულად დაწერე
3. შექება technical aspects-ები
4. ჩართე humor
5. მოკლე იყო (1-2 წინადადება)

მაგალითი style: ""ბრავო! ამ კოდს ჩემი ბებიაც დაწერდა, მაგრამ მაინც კარგია! 🔥""
";
    }
}