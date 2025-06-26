using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace DevLife.API.Models.MongoDB
{
    public class CodeSnippet
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;

        [BsonElement("title")]
        public string Title { get; set; } = string.Empty;

        [BsonElement("description")]
        public string Description { get; set; } = string.Empty;

        [BsonElement("techStack")]
        public string TechStack { get; set; } = string.Empty;

        [BsonElement("difficulty")]
        public string Difficulty { get; set; } = string.Empty;

        [BsonElement("code1")]
        public string Code1 { get; set; } = string.Empty;

        [BsonElement("code2")]
        public string Code2 { get; set; } = string.Empty;

        [BsonElement("correctAnswer")]
        public int CorrectAnswer { get; set; }

        [BsonElement("explanation")]
        public string Explanation { get; set; } = string.Empty;

        [BsonElement("tags")]
        public List<string> Tags { get; set; } = new();

        [BsonElement("category")]
        public string Category { get; set; } = string.Empty;

        [BsonElement("isActive")]
        public bool IsActive { get; set; } = true;

        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
