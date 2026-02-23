using Microsoft.Extensions.Options;
using MongoDB.Driver.GeoJsonObjectModel;
using MongoDB.Driver;
using Real_Estate_WebAPI.Interfaces;
using Real_Estate_WebAPI.Models;
using Real_Estate_WebAPI.Settings;

namespace Real_Estate_WebAPI.Repositories
{
    public class PropertyRepository : IPropertyRepository
    {
        private readonly IMongoCollection<Property> _properties;

        public PropertyRepository(
            IMongoClient client,
            IOptions<MongoDbSettings> settings)
        {
            var database = client.GetDatabase(settings.Value.DatabaseName);

            _properties = database.GetCollection<Property>(
                settings.Value.PropertiesCollection);
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
            var builder = Builders<Property>.Filter;
            var filter = builder.Eq(p => p.Status, "Approved");

            if (!string.IsNullOrEmpty(city))
                filter &= builder.Eq(p => p.City, city);

            if (!string.IsNullOrEmpty(locality))
                filter &= builder.Eq(p => p.Locality, locality);

            if (minPrice.HasValue)
                filter &= builder.Gte(p => p.Price, minPrice.Value);

            if (maxPrice.HasValue)
                filter &= builder.Lte(p => p.Price, maxPrice.Value);

            if (!string.IsNullOrEmpty(bedrooms))
                filter &= builder.Eq(p => p.Bedrooms, bedrooms);

            if (!string.IsNullOrEmpty(transactionType))
                filter &= builder.Eq(p => p.YouAreHereTo, transactionType);

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
