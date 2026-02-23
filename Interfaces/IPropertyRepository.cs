using Real_Estate_WebAPI.Models;

namespace Real_Estate_WebAPI.Interfaces
{
    public interface IPropertyRepository
    {
        Task CreateAsync(Property property);

        Task<Property> GetByIdAsync(string id);

        Task<List<Property>> GetAllAsync(
            int page,
            int pageSize);

        Task<List<Property>> GetByUserAsync(
            string userId);

        Task UpdateAsync(Property property);

        Task DeleteAsync(string id);

        Task<List<Property>> SearchAsync(
            string city,
            string locality,
            decimal? minPrice,
            decimal? maxPrice,
            string bedrooms,
            string transactionType,
            int page,
            int pageSize);

        Task<List<Property>> GetNearbyAsync(
            double latitude,
            double longitude,
            double radiusInKm);
    }
}
