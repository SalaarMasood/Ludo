// LudoApp.Server/Services/MongoDbService.cs
using Microsoft.Extensions.Options; // To read configuration from appsettings.json
using MongoDB.Driver;              // The MongoDB C# driver
using LudoApp.Shared;             // To use the shared User model
using LudoApp.Server.Models;      // To use MongoDbSettings

namespace LudoApp.Server.Services
{
    public class MongoDbService
    {
        private readonly IMongoCollection<User> _usersCollection;

        public MongoDbService(IOptions<MongoDbSettings> mongoDbSettings)
        {
            // Create a new MongoDB client using the connection string from settings
            var mongoClient = new MongoClient(mongoDbSettings.Value.ConnectionString);

            // Get the specific database
            var mongoDatabase = mongoClient.GetDatabase(mongoDbSettings.Value.DatabaseName);

            // Get the collection to interact with users
            _usersCollection = mongoDatabase.GetCollection<User>(mongoDbSettings.Value.UsersCollectionName);
        }

        // Method to find a user by username (useful for checking if username exists)
        public async Task<User?> GetUserByUsernameAsync(string username)
        {
            // .Find() returns a filterable collection, .FirstOrDefaultAsync() executes the query
            return await _usersCollection.Find(u => u.Username == username).FirstOrDefaultAsync();
        }

        // Method to create a new user document in the database
        public async Task CreateUserAsync(User newUser)
        {
            await _usersCollection.InsertOneAsync(newUser);
        }

        // Method to increment the wins of a user
        public async Task IncrementUserWinsAsync(string username)
        {
            var filter = Builders<User>.Filter.Eq(u => u.Username, username);
            var update = Builders<User>.Update.Inc(u => u.Wins, 1);
            
            var result = await _usersCollection.UpdateOneAsync(filter, update);
            
            if (result.ModifiedCount > 0)
            {
                Console.WriteLine($"[MongoDbService] Incremented wins for user: {username}");
            }
            else
            {
                Console.WriteLine($"[MongoDbService] Failed to increment wins for user: {username}");
            }
        }
    }
}