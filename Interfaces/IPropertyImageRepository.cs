using Real_Estate_WebAPI.Models;

namespace Real_Estate_WebAPI.Interfaces
{
    public interface IPropertyImageRepository
    {
      public  Task<List<PropertyImage>> GetByPropertyIdAsync(string propertyId);

        Task<PropertyImage?> GetByIdAsync(string id);

        Task<PropertyImage> CreateAsync(PropertyImage image);

        Task CreateManyAsync(List<PropertyImage> images);

        Task DeleteAsync(string id);

        Task DeleteByPropertyIdAsync(string propertyId);
    }
}
