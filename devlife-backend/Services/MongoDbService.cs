using DevLife.API.Models.MongoDB;
using MongoDB.Driver;

namespace DevLife.API.Services
{
    public class MongoDbService
    {
        private readonly IMongoDatabase _database;
        private readonly IMongoCollection<DevProfile> _profiles;
        private readonly IMongoCollection<CodeSnippet> _codeSnippets;

        public MongoDbService(IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("MongoDB");
            var client = new MongoClient(connectionString);
            _database = client.GetDatabase("devlife");

            _profiles = _database.GetCollection<DevProfile>("profiles");
            _codeSnippets = _database.GetCollection<CodeSnippet>("code_snippets");
        }

        public async Task<List<DevProfile>> GetProfilesAsync(int limit = 10)
        {
            return await _profiles.Find(p => p.IsActive).Limit(limit).ToListAsync();
        }

        public async Task<DevProfile> GetProfileByIdAsync(string id)
        {
            return await _profiles.Find(p => p.Id == id).FirstOrDefaultAsync();
        }

        public async Task<List<DevProfile>> GetProfilesByTechStackAsync(string techStack, int limit = 5)
        {
            var filter = Builders<DevProfile>.Filter.And(
                Builders<DevProfile>.Filter.Eq(p => p.IsActive, true),
                Builders<DevProfile>.Filter.Eq(p => p.TechStack, techStack)
            );
            return await _profiles.Find(filter).Limit(limit).ToListAsync();
        }

        public async Task<DevProfile> CreateProfileAsync(DevProfile profile)
        {
            await _profiles.InsertOneAsync(profile);
            return profile;
        }

        public async Task<List<CodeSnippet>> GetCodeSnippetsAsync(string techStack, string difficulty)
        {
            var filter = Builders<CodeSnippet>.Filter.And(
                Builders<CodeSnippet>.Filter.Eq(c => c.IsActive, true),
                Builders<CodeSnippet>.Filter.Eq(c => c.TechStack, techStack),
                Builders<CodeSnippet>.Filter.Eq(c => c.Difficulty, difficulty)
            );
            return await _codeSnippets.Find(filter).ToListAsync();
        }

        public async Task<CodeSnippet> GetRandomCodeSnippetAsync(string techStack, string difficulty)
        {
            var filter = Builders<CodeSnippet>.Filter.And(
                Builders<CodeSnippet>.Filter.Eq(c => c.IsActive, true),
                Builders<CodeSnippet>.Filter.Eq(c => c.TechStack, techStack),
                Builders<CodeSnippet>.Filter.Eq(c => c.Difficulty, difficulty)
            );

            var count = await _codeSnippets.CountDocumentsAsync(filter);
            if (count == 0) return null;

            var random = new Random();
            var skip = random.Next(0, (int)count);

            return await _codeSnippets.Find(filter).Skip(skip).FirstOrDefaultAsync();
        }

        public async Task<CodeSnippet> CreateCodeSnippetAsync(CodeSnippet snippet)
        {
            await _codeSnippets.InsertOneAsync(snippet);
            return snippet;
        }

        public async Task SeedDataAsync()
        {
            var profilesCount = await _profiles.CountDocumentsAsync(FilterDefinition<DevProfile>.Empty);
            if (profilesCount == 0)
            {
                var sampleProfiles = new List<DevProfile>
                {
                    new() {
                        Name = "ანა დეველოპერი",
                        Age = 26,
                        TechStack = "React",
                        ExperienceLevel = "Middle",
                        Bio = "მიყვარს React-ით სამუშაო და სუფთა კოდის წერა",
                        Interests = new() { "Coding", "Reading", "Coffee" },
                        Location = "თბილისი",
                        ZodiacSign = "Virgo"
                    },
                    new() {
                        Name = "გიორგი კოდერი",
                        Age = 29,
                        TechStack = ".NET",
                        ExperienceLevel = "Senior",
                        Bio = "Backend დეველოპერი მეტრო 10 წლის გამოცდილებით",
                        Interests = new() { "Architecture", "Music", "Gaming" },
                        Location = "ბათუმი",
                        ZodiacSign = "Leo"
                    }
                };
                await _profiles.InsertManyAsync(sampleProfiles);
            }

            var snippetsCount = await _codeSnippets.CountDocumentsAsync(FilterDefinition<CodeSnippet>.Empty);
            if (snippetsCount == 0)
            {
                var sampleSnippets = new List<CodeSnippet>
                {
                    new() {
                        Title = "React Hook რა იქნება?",
                        Description = "რომელი კოდი მუშაობს სწორად?",
                        TechStack = "React",
                        Difficulty = "Junior",
                        Code1 = "const [count, setCount] = useState(0);\nconst increment = () => {\n  setCount(count + 1);\n};",
                        Code2 = "const [count, setCount] = useState(0);\nconst increment = () => {\n  setCount(prev => prev + 1);\n};",
                        CorrectAnswer = 2,
                        Explanation = "useState-ში ყოველთვის function form უნდა ვიყენოთ state update-ისთვის",
                        Tags = new() { "React", "Hooks", "State" },
                        Category = "Best Practice"
                    }
                };
                await _codeSnippets.InsertManyAsync(sampleSnippets);
            }
        }
    }
}
