// LudoApp.Shared/User.cs
using System.ComponentModel.DataAnnotations; // For validation attributes in Blazor

// MongoDB related attributes
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace LudoApp.Shared
{
    public class User
    {
        // This maps to MongoDB's unique document ID (_id)
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)] // Tells MongoDB to treat this as an ObjectId
        public string? Id { get; set; } // Nullable, as MongoDB generates it on insert

        [Required(ErrorMessage = "Username is required.")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Username must be between 3 and 50 characters.")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        [StringLength(50, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters long.")]
        public string Password { get; set; } = string.Empty;

        public int Wins { get; set; } = 0; 

    }
}