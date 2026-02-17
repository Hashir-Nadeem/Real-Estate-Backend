using MongoDB.Driver;
using Microsoft.Extensions.Options;
using Real_Estate_WebAPI.Models;
using Real_Estate_WebAPI.Settings;


namespace Real_Estate_WebAPI.Services
{
    public class DatabaseInitializer
    {
        private readonly IMongoCollection<User> _users;

        public DatabaseInitializer(
            IMongoClient client,
            IOptions<MongoDbSettings> settings)
        {
            var database = client.GetDatabase(settings.Value.DatabaseName);
            _users = database.GetCollection<User>(settings.Value.UsersCollection);
        }

        public async Task InitializeAsync()
        {
            await CreateUserIndexes();
        }

        private async Task CreateUserIndexes()
        {
            var emailIndex = new CreateIndexModel<User>(
        Builders<User>.IndexKeys.Ascending(u => u.Email),
        new CreateIndexOptions
        {
            Unique = true,
            Collation = new Collation("en", strength: CollationStrength.Secondary)
        });

            var phoneIndex = new CreateIndexModel<User>(
                Builders<User>.IndexKeys.Ascending(u => u.PhoneNumber),
                new CreateIndexOptions
                {
                    Unique = true
                });

            await _users.Indexes.CreateManyAsync(new[]
            {
            emailIndex,
            phoneIndex
        });
        }
    }

}
