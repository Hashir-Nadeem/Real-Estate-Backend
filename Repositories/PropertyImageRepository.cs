using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Real_Estate_WebAPI.Interfaces;
using Real_Estate_WebAPI.Models;
using Real_Estate_WebAPI.Services.Auth;
using Real_Estate_WebAPI.Settings;

public class PropertyImageRepository : IPropertyImageRepository
{
    private readonly IMongoCollection<PropertyImage> _propertyImages;

    public PropertyImageRepository(
           IMongoClient client,
            IOptions<MongoDbSettings> settings)
    {
        var database = client.GetDatabase(settings.Value.DatabaseName);

        _propertyImages = database.GetCollection<PropertyImage>(
            settings.Value.PropertiesImageCollection);
        
    }

    public async Task<PropertyImage> CreateAsync(
        PropertyImage image)
    {
        await _propertyImages.InsertOneAsync(image);

        return image;
    }

    public async Task CreateManyAsync(
        List<PropertyImage> images)
    {
        if (!images.Any())
            return;

        await _propertyImages.InsertManyAsync(images);
    }

    public async Task<PropertyImage?> GetByIdAsync(
        string id)
    {
        return await _propertyImages
            .Find(x => x.Id == id)
            .FirstOrDefaultAsync();
    }

    public async Task<List<PropertyImage>> GetByPropertyIdAsync(
        string propertyId)
    {
        return await _propertyImages
            .Find(x => x.PropertyId == propertyId)
            .SortBy(x => x.SortOrder)
            .ToListAsync();
    }

    public async Task DeleteAsync(string id)
    {
        await _propertyImages.DeleteOneAsync(
            x => x.Id == id);
    }

    public async Task DeleteByPropertyIdAsync(
        string propertyId)
    {
        await _propertyImages.DeleteManyAsync(
            x => x.PropertyId == propertyId);
    }
}