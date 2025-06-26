using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using DevLife.API.Data;
using DevLife.API.Services;
using DevLife.API.Models;
using DevLife.API.Models.DTOs;

// Check if running in Docker
var isDocker = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER"));

if (!isDocker)
{
    // Running locally - load .env file
    var envPath = Path.Combine(Directory.GetCurrentDirectory(), ".env");
    if (File.Exists(envPath))
    {
        DotNetEnv.Env.Load(envPath);
        Console.WriteLine("✅ .env file loaded successfully from: " + envPath);
    }
    else
    {
        Console.WriteLine($"💻 Running locally without .env file");
    }
}
else
{
    Console.WriteLine("🐳 Running in Docker - using environment variables from docker-compose");
}

// Check environment variables
var geminiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
Console.WriteLine($"GEMINI_API_KEY: {(string.IsNullOrEmpty(geminiKey) ? "❌ Not set" : "✅ Available")}");

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", false);

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "DevLife Portal API",
        Version = "v1",
        Description = "API for the DevLife Portal - Developer Life Simulator"
    });

    // Add JWT Bearer authentication to Swagger
    c.AddSecurityDefinition("Bearer", new()
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Enter your session token (without 'Bearer ' prefix)"
    });

    c.AddSecurityRequirement(new()
    {
        {
            new()
            {
                Reference = new() { Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            new string[] {}
        }
    });
});

// Add Entity Framework
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("PostgreSQL")));

// Add custom services
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<CasinoService>();
builder.Services.AddScoped<GeminiService>();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Ensure database is created and seed data
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await context.Database.EnsureCreatedAsync();

    // Seed casino challenges
    var casinoService = scope.ServiceProvider.GetRequiredService<CasinoService>();
    await casinoService.SeedChallengesAsync();
}

// Configure pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "DevLife Portal API v1");
        c.RoutePrefix = "swagger"; // Access at /swagger
        c.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.List);
        c.DefaultModelsExpandDepth(-1); // Hide schemas section by default
    });
}

app.UseCors("AllowAll");

// Health check
app.MapGet("/api/health", () =>
    Results.Ok(new
    {
        status = "healthy",
        timestamp = DateTime.UtcNow,
        environment = app.Environment.EnvironmentName
    }));

// Authentication endpoints
app.MapPost("/api/auth/register", async (AuthService authService,
    string username, string firstName, string lastName, DateTime birthDate,
    string techStack, ExperienceLevel experienceLevel) =>
{
    var user = await authService.RegisterAsync(username, firstName, lastName,
        birthDate, techStack, experienceLevel);

    if (user == null)
    {
        return Results.BadRequest(new { message = "Username already exists" });
    }

    var token = await authService.LoginAsync(user.Username);
    return Results.Ok(new { user, token });
});

app.MapPost("/api/auth/login", async (AuthService authService, string username) =>
{
    var token = await authService.LoginAsync(username);
    if (token == null)
    {
        return Results.BadRequest(new { message = "User not found" });
    }

    var user = await authService.GetUserByTokenAsync(token);
    return Results.Ok(new { user, token });
});

// Dashboard endpoint
app.MapGet("/api/dashboard", async (HttpContext context, AuthService authService) =>
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

    var horoscope = ZodiacService.GetDailyHoroscope(user.ZodiacSign);
    var welcomeMessage = $"გამარჯობა {user.FirstName}! {user.ZodiacSign}, დღეს {horoscope}";

    return Results.Ok(new
    {
        welcomeMessage,
        user,
        dailyHoroscope = horoscope,
        luckyTech = ZodiacService.GetLuckyTechnology()
    });
});

// Casino endpoints
app.MapGet("/api/casino/challenge", async (HttpContext context, AuthService authService, CasinoService casinoService) =>
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
            isAI = challenge.Id == 0 // AI generated challenges have Id = 0
        },
        userPoints
    });
});

// Updated play endpoint that handles both static and AI challenges
app.MapPost("/api/casino/play", async (HttpContext context, AuthService authService, CasinoService casinoService,
    PlayCasinoRequest request) =>
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

    // Check if user has enough points
    var currentPoints = await casinoService.GetUserPointsAsync(user.Id);
    if (currentPoints < request.BetPoints)
    {
        return Results.BadRequest(new { message = "არ გაქვს საკმარისი points! 💰" });
    }

    try
    {
        CasinoGame game;

        // If it's an AI challenge (challengeData provided)
        if (request.ChallengeData != null)
        {
            var aiChallenge = new CasinoChallenge
            {
                Id = 0, // AI challenge
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
            // Static challenge
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
});

// Separate endpoint for AI-only challenges (for testing)
app.MapGet("/api/casino/ai-challenge", async (HttpContext context, AuthService authService, GeminiService geminiService) =>
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

    try
    {
        var aiChallenge = await geminiService.GenerateCodeChallengeAsync(user.TechStack, user.ExperienceLevel);
        if (aiChallenge == null)
        {
            return Results.Problem("Failed to generate AI challenge");
        }

        return Results.Ok(new
        {
            challenge = new
            {
                aiChallenge.Id,
                aiChallenge.Title,
                aiChallenge.Description,
                aiChallenge.CodeSnippet1,
                aiChallenge.CodeSnippet2,
                aiChallenge.TechStack,
                aiChallenge.Difficulty,
                isAI = true
            }
        });
    }
    catch (Exception ex)
    {
        return Results.Problem($"AI Error: {ex.Message}");
    }
});

app.MapGet("/api/casino/leaderboard", async (CasinoService casinoService) =>
{
    var leaderboard = await casinoService.GetLeaderboardAsync();
    return Results.Ok(new { leaderboard });
});

app.MapGet("/api/casino/points", async (HttpContext context, AuthService authService, CasinoService casinoService) =>
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
});

app.Run();