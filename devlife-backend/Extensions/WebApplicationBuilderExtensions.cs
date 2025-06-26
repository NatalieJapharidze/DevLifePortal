namespace DevLife.API.Extensions
{
    public static class WebApplicationBuilderExtensions
    {
        public static void LoadEnvironmentVariables(this WebApplicationBuilder builder)
        {
            var isDocker = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER"));

            if (!isDocker)
            {
                var envPath = Path.Combine(Directory.GetCurrentDirectory(), ".env");
                if (File.Exists(envPath))
                {
                    DotNetEnv.Env.Load(envPath);
                    Console.WriteLine(".env file loaded");
                }
            }

            var geminiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
            Console.WriteLine($"GEMINI_API_KEY: {(string.IsNullOrEmpty(geminiKey) ? "Not set" : "Available")}");

            AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", false);
        }
    }
}