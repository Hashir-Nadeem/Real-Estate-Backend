using MongoDB.Driver;
using Microsoft.Extensions.Options;
using Real_Estate_WebAPI.Models;
using Real_Estate_WebAPI.Settings;
using MongoDB.Bson;

namespace Real_Estate_WebAPI.Services
{
    public class DatabaseInitializer
    {
        private readonly IMongoCollection<User> _users;
        private readonly IMongoCollection<Property> _properties;
        private readonly IMongoCollection<PropertyImage> _propertiesImages;

        public DatabaseInitializer(
            IMongoClient client,
            IOptions<MongoDbSettings> settings)
        {
            var database = client.GetDatabase(settings.Value.DatabaseName);

            _users = database.GetCollection<User>(
                settings.Value.UsersCollection);

            _properties = database.GetCollection<Property>(
                settings.Value.PropertiesCollection);

            _propertiesImages = database.GetCollection<PropertyImage>(
             settings.Value.PropertiesImageCollection);
        }

        public async Task InitializeAsync()
        {

            var retries = 3;

            for (int i = 0; i < retries; i++)
            {
                try
                {
                   
                   await CreateUserIndexes();
                    await CreatePropertyIndexes();
                    await CreatePropertyImageIndexes();

                    Console.WriteLine("Database initialized successfully");
                    return;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Retry {i + 1} failed: {ex.Message}");
                    await Task.Delay(2000);
                }
            }

            Console.WriteLine("Database initialization failed after retries.");
        }


        // =========================
        // USER INDEXES
        // =========================

        private async Task CreateUserIndexes()
        {
            var emailIndex = new CreateIndexModel<User>(
                Builders<User>.IndexKeys.Ascending(u => u.Email),
                new CreateIndexOptions
                {
                    Unique = true,
                    Collation = new Collation("en",
                        strength: CollationStrength.Secondary)
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
        private async Task CreatePropertyImageIndexes()
        {
            var propertyIdIndex = new CreateIndexModel<PropertyImage>(
                Builders<PropertyImage>.IndexKeys.Ascending(i => i.PropertyId));

            var createdAtIndex = new CreateIndexModel<PropertyImage>(
                Builders<PropertyImage>.IndexKeys.Descending(i => i.CreatedAt));

            await _propertiesImages.Indexes.CreateManyAsync(new[]
            {
        propertyIdIndex,
        createdAtIndex
    });
        }
        // =========================
        // PROPERTY INDEXES
        // =========================

        private async Task CreatePropertyIndexes()
        {
            var indexModels = new List<CreateIndexModel<Property>>();

            // 🔥 GEO INDEX (for nearby search)
            indexModels.Add(
                new CreateIndexModel<Property>(
                    Builders<Property>.IndexKeys.Geo2DSphere(p => p.Location)));

            // City + Locality
            indexModels.Add(
                new CreateIndexModel<Property>(
                    Builders<Property>.IndexKeys.Ascending(p => p.City)));

            indexModels.Add(
                new CreateIndexModel<Property>(
                    Builders<Property>.IndexKeys.Ascending(p => p.Locality)));

            // User (My Properties)
            indexModels.Add(
                new CreateIndexModel<Property>(
                    Builders<Property>.IndexKeys.Ascending(p => p.UserId)));

            // Status (Admin moderation)
            indexModels.Add(
                new CreateIndexModel<Property>(
                    Builders<Property>.IndexKeys.Ascending(p => p.Status)));

            // CreatedAt (newest first)
            indexModels.Add(
                new CreateIndexModel<Property>(
                    Builders<Property>.IndexKeys.Descending(p => p.CreatedAt)));

            // Price (range filtering)
            indexModels.Add(
                new CreateIndexModel<Property>(
                    Builders<Property>.IndexKeys.Ascending(p => p.Price)));

            // Bedrooms filter
            indexModels.Add(
                new CreateIndexModel<Property>(
                    Builders<Property>.IndexKeys.Ascending(p => p.Bedrooms)));

            // Transaction type (Rent/Sell)
            indexModels.Add(
                new CreateIndexModel<Property>(
                    Builders<Property>.IndexKeys.Ascending(p => p.YouAreHereTo)));

            // 🔥 Compound Index (City + Price + Status)
            indexModels.Add(
                new CreateIndexModel<Property>(
                    Builders<Property>.IndexKeys
                        .Ascending(p => p.City)
                        .Ascending(p => p.Price)
                        .Ascending(p => p.Status)));

            await _properties.Indexes.CreateManyAsync(indexModels);
        }
    }
}