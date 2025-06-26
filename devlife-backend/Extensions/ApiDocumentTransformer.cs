using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;

namespace DevLife.API.Extensions
{
    public class ApiDocumentTransformer : IOpenApiDocumentTransformer
    {
        public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
        {
            document.Info = new OpenApiInfo
            {
                Title = "🎮 DevLife Portal API",
                Version = "v1",
                Description = """
                    **DevLife Portal** - Developer Life Simulator
                    
                    ინტერაქტიული ვებ პლატფორმა 6 მინი-პროექტით:
                    
                    🎰 **Code Casino** - Code snippet guessing game  
                    🔥 **Code Roasting** - AI code evaluation  
                    🏃 **Bug Chase** - Endless runner game  
                    🔍 **GitHub Analyzer** - Code personality analysis  
                    💑 **Dev Dating** - Developer matching  
                    🏃 **Meeting Escape** - Excuse generator  
                    
                    ## 🔐 Authentication
                    Session-based (username only, no password required)
                    
                    ## ⭐ Features
                    - Zodiac horoscope & luck multipliers
                    - Real-time leaderboards  
                    - Multi-database architecture
                    - AI-powered challenges
                    """,
                Contact = new OpenApiContact
                {
                    Name = "DevLife Portal",
                    Url = new Uri("https://github.com/your-org/devlife-portal")
                }
            };

            document.Servers = new List<OpenApiServer>
            {
                new() { Url = "http://localhost:5000", Description = "Development Server" },
                new() { Url = "https://api.devlife.ge", Description = "Production Server" }
            };

            document.Tags = new List<OpenApiTag>
            {
                new() { Name = "Authentication", Description = "🔐 User registration and login" },
                new() { Name = "Dashboard", Description = "🏠 User dashboard with zodiac horoscope" },
                new() { Name = "Casino", Description = "🎰 Code Casino mini-game" },
                new() { Name = "Dating", Description = "💑 Dev Dating Room profiles" },
                new() { Name = "System", Description = "⚙️ Health checks and statistics" }
            };

            return Task.CompletedTask;
        }
    }
}
