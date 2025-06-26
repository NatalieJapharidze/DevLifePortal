using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace DevLife.API.Models.MongoDB
{
    public class DevProfile
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;

        [BsonElement("name")]
        public string Name { get; set; } = string.Empty;

        [BsonElement("age")]
        public int Age { get; set; }

        [BsonElement("techStack")]
        public string TechStack { get; set; } = string.Empty;

        [BsonElement("experienceLevel")]
        public string ExperienceLevel { get; set; } = string.Empty;

        [BsonElement("bio")]
        public string Bio { get; set; } = string.Empty;

        [BsonElement("interests")]
        public List<string> Interests { get; set; } = new();

        [BsonElement("location")]
        public string Location { get; set; } = string.Empty;

        [BsonElement("profileImage")]
        public string ProfileImage { get; set; } = string.Empty;

        [BsonElement("isActive")]
        public bool IsActive { get; set; } = true;

        [BsonElement("zodiacSign")]
        public string ZodiacSign { get; set; } = string.Empty;

        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
