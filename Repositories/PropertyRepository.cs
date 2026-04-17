using Microsoft.Extensions.Options;
using MongoDB.Driver.GeoJsonObjectModel;
using MongoDB.Driver;
using Real_Estate_WebAPI.Interfaces;
using Real_Estate_WebAPI.Models;
using Real_Estate_WebAPI.Settings;
using Real_Estate_WebAPI.Services.Auth;

namespace Real_Estate_WebAPI.Repositories
{
    public class PropertyRepository : IPropertyRepository
    {
        private readonly IMongoCollection<Property> _properties;
        private readonly IAuthService _auth;

        public PropertyRepository(
            IMongoClient client,
            IOptions<MongoDbSettings> settings,
            IAuthService auth)
        {
            var database = client.GetDatabase(settings.Value.DatabaseName);

            _properties = database.GetCollection<Property>(
                settings.Value.PropertiesCollection);

            _auth = auth; // ✅ correct assignment
        }

        public async Task CreateAsync(Property property)
        {
            await _properties.InsertOneAsync(property);
        }

        public async Task<Property> GetByIdAsync(string id)
        {
            return await _properties
                .Find(p => p.Id == id)
                .FirstOrDefaultAsync();
        }

        public async Task<PropertyDetailsDto?> GetPropertyDetailsAsync(string id)
        {
            // 🔹 Get property
            var property = await GetByIdAsync(id);

            if (property == null)
                return null;

            // 🔹 Get user (from AuthService)
            var user = await _auth.GetUserByIdAsync(property.UserId);

            // 🔹 Map safely
            return new PropertyDetailsDto
            {
                Id = property.Id,
                UserId = property.UserId,

                PropertyCategory = property.PropertyCategory,
                TransactionType = property.TransactionType,
                YouAreHereTo = property.YouAreHereTo,
                Title = property.Title,
                Description = property.Description,

                Price = property.Price,
                PriceUnit = property.PriceUnit,

                Area = property.Area,
                AreaUnit = property.AreaUnit,

                Bedrooms = property.Bedrooms,
                Bathrooms = property.Bathrooms,
                Facing = property.Facing,
                FloorNumber = property.FloorNumber,
                TotalFloors = property.TotalFloors,

                FullAddress = property.FullAddress,
                City = property.City,
                Locality = property.Locality,

                Location = property.Location,

                ContactPersonName = property.ContactPersonName,
                Email = property.Email,
                Whatsapp = property.Whatsapp,

                UploadedImages = property.UploadedImages,

                Status = property.Status,
                CreatedAt = property.CreatedAt,

                // ✅ SAFE + CORRECT
                CreatedByUser = user?.FullName ?? "Unknown"
            };
        }

        public async Task<List<Property>> GetAllAsync(
            int page, 
            int pageSize)
        {
            return await _properties
                .Find(p => p.Status == "Approved")
                .SortByDescending(p => p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Limit(pageSize)
                .ToListAsync();
        }
        public async Task<bool> UpdateStatusAsync(string id, string status)
        {
            var update = Builders<Property>.Update.Set(p => p.Status, status);

            var result = await _properties.UpdateOneAsync(
                p => p.Id == id,
                update
            );

            return result.ModifiedCount > 0;
        }

        public async Task<List<PropertyDetailsDto>> GetAllPropertyDetailsAsync(
    int page,
    int pageSize)
        {
            var properties = await _properties
      .Find(_ => true) // explicit "get all"
      .SortByDescending(p => p.CreatedAt)
      .Skip((page - 1) * pageSize)
      .Limit(pageSize)
      .ToListAsync();

            var result = new List<PropertyDetailsDto>();

            foreach (var property in properties)
            {
                var user = await _auth.GetUserByIdAsync(property.UserId);

                result.Add(new PropertyDetailsDto
                {
                    Id = property.Id,
                    UserId = property.UserId,

                    PropertyCategory = property.PropertyCategory,
                    TransactionType = property.TransactionType,
                    YouAreHereTo = property.YouAreHereTo,
                    Title = property.Title,
                    Description = property.Description,

                    Price = property.Price,
                    PriceUnit = property.PriceUnit,

                    Area = property.Area,
                    AreaUnit = property.AreaUnit,

                    Bedrooms = property.Bedrooms,
                    Bathrooms = property.Bathrooms,
                    Facing = property.Facing,
                    FloorNumber = property.FloorNumber,
                    TotalFloors = property.TotalFloors,

                    FullAddress = property.FullAddress,
                    City = property.City,
                    Locality = property.Locality,

                    Location = property.Location,

                    ContactPersonName = property.ContactPersonName,
                    Email = property.Email,
                    Whatsapp = property.Whatsapp,

                    UploadedImages = property.UploadedImages,

                    Status = property.Status,
                    CreatedAt = property.CreatedAt,

                    // ✅ SAFE + CORRECT
                    CreatedByUser = user?.FullName ?? "Unknown"
                });
            }

            return result;
        }
        public async Task<List<Property>> GetByUserAsync(
            string userId)
        {
            return await _properties
                .Find(p => p.UserId == userId)
                .SortByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task UpdateAsync(Property property)
        {
            await _properties.ReplaceOneAsync(
                p => p.Id == property.Id,
                property);
        }

        public async Task DeleteAsync(string id)
        {
            await _properties.DeleteOneAsync(
                p => p.Id == id);
        }

        // =========================
        // FILTER SEARCH
        // =========================

        public async Task<List<Property>> SearchAsync(
      string city,
      string locality,
      decimal? minPrice,
      decimal? maxPrice,
      string bedrooms,
      string transactionType,
      int page,
      int pageSize)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;

            var builder = Builders<Property>.Filter;

            // Always filter only Approved
            var filter = builder.Eq(p => p.Status, "Approved");

            // ✅ CITY
            if (!string.IsNullOrWhiteSpace(city) &&
                !city.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                filter &= builder.Regex(p => p.City,
                    new MongoDB.Bson.BsonRegularExpression(city, "i"));
            }

            // ✅ LOCALITY
            if (!string.IsNullOrWhiteSpace(locality) &&
                !locality.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                filter &= builder.Regex(p => p.Locality,
                    new MongoDB.Bson.BsonRegularExpression(locality, "i"));
            }

            // ✅ MIN PRICE
            if (minPrice.HasValue)
                filter &= builder.Gte(p => p.Price, minPrice.Value);

            // ✅ MAX PRICE
            if (maxPrice.HasValue)
                filter &= builder.Lte(p => p.Price, maxPrice.Value);

            // ✅ BEDROOMS
            if (!string.IsNullOrWhiteSpace(bedrooms) &&
                !bedrooms.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                filter &= builder.Eq(p => p.Bedrooms, bedrooms);
            }

            // ✅ TRANSACTION TYPE
            if (!string.IsNullOrWhiteSpace(transactionType) &&
                !transactionType.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                filter &= builder.Eq(p => p.YouAreHereTo, transactionType);
            }

            return await _properties
                .Find(filter)
                .SortByDescending(p => p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Limit(pageSize)
                .ToListAsync();
        }
        // =========================
        // GEO SEARCH
        // =========================

        public async Task<List<Property>> GetNearbyAsync(
            double latitude,
            double longitude,
            double radiusInKm)
        {
            var point = new GeoJsonPoint<GeoJson2DCoordinates>(
                new GeoJson2DCoordinates(longitude, latitude));

            var radiusInMeters = radiusInKm * 1000;

            var filter = Builders<Property>.Filter.NearSphere(
                p => p.Location,
                point,
                maxDistance: radiusInMeters);

            return await _properties
                .Find(filter & Builders<Property>.Filter.Eq(p => p.Status, "Approved"))
                .Limit(50)
                .ToListAsync();
        }
    }
}
