using Real_Estate_WebAPI.Models;
using Real_Estate_WebAPI.Repositories;

namespace Real_Estate_WebAPI.Interfaces
{
    public interface IPropertyRepository
    {
        Task CreateAsync(Property property);

        Task<Property> GetByIdAsync(string id);

        Task<List<Property>> GetAllAsync(
            int page,
            int pageSize);
        Task<List<PropertyDetailsDto>> GetAllPropertyDetailsAsync(int page, int pageSize);

        Task<List<Property>> GetByUserAsync(
            string userId);

        Task UpdateAsync(Property property);

        Task DeleteAsync(string id);

        Task<PropertyDetailsDto?> GetPropertyDetailsAsync(string id);
        // ✅ NEW: Update status (BEST approach)
        Task<bool> UpdateStatusAsync(string id, string status);
        Task<List<Property>> SearchAsync(
        string? city,
        string? locality,
        decimal? minPrice,
        decimal? maxPrice,
        string? bedrooms,
        string? transactionType,
        int page,
        int pageSize);
        Task<List<Property>> GetNearbyAsync(
            double latitude,
            double longitude,
            double radiusInKm);
    }
}
