using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver.GeoJsonObjectModel;
using Real_Estate_WebAPI.DTOs.Property;
using Real_Estate_WebAPI.Interfaces;
using Real_Estate_WebAPI.Models;
using System.Security.Claims;

namespace Real_Estate_WebAPI.Controllers
{
    [ApiController]
    [Route("api/properties")]
    public class PropertiesController : ControllerBase
    {
        private readonly IPropertyRepository _repository;

        public PropertiesController(IPropertyRepository repository)
        {
            _repository = repository;
        }

        // ======================================
        // CREATE PROPERTY
        // ======================================


        [HttpPost]
        public async Task<IActionResult> Create(CreatePropertyRequest request)
        {
           

            if (request.FormData == null)
                return BadRequest("Invalid payload");

            var property = new Property
            {
                PropertyCategory = request.FormData.PropertyCategory,
                UserId = null,
                YouAreHereTo = request.FormData.YouAreHereTo,
                Title = request.FormData.Title,
                Description = request.FormData.Description,
                Price = request.FormData.Price,
                PriceUnit = request.FormData.PriceUnit,
                Area = request.FormData.Area,
                AreaUnit = request.FormData.AreaUnit,
                Bedrooms = request.FormData.Bedrooms,
                Bathrooms = request.FormData.Bathrooms,
                Facing = request.FormData.Facing,
                FloorNumber = request.FormData.FloorNumber,
                TotalFloors = request.FormData.TotalFloors,
                FullAddress = request.FormData.FullAddress,
                City = request.FormData.City,
                Locality = request.FormData.Locality,
                ContactPersonName = request.FormData.ContactPersonName,
                Email = request.FormData.Email,
                Whatsapp = request.FormData.Whatsapp,
                UploadedImages = request.FormData.UploadedImages ?? new List<string>(),
                Status = "Approved",
                CreatedAt = DateTime.UtcNow
            };

            if (request.Location != null)
            {
                property.Location =
                    new GeoJsonPoint<GeoJson2DCoordinates>(
                        new GeoJson2DCoordinates(
                            request.Location.Lng,
                            request.Location.Lat
                        )
                    );
            }

            await _repository.CreateAsync(property);

            return Ok(new { message = "Property submitted successfully" });
        }
        // ======================================
        // GET ALL (Public Approved)
        // ======================================

        [HttpGet]
        public async Task<IActionResult> GetAll(
       int page = 1,
       int pageSize = 10)
        {
            var properties = await _repository.GetAllAsync(page, pageSize);
            return Ok(properties);
        }

        // ======================================
        // GET BY ID
        // ======================================

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var property = await _repository.GetByIdAsync(id);

            if (property == null)
                return NotFound();

            return Ok(property);
        }

        // ======================================
        // MY PROPERTIES
        // ======================================

      
        [HttpGet("my")]
        public async Task<IActionResult> GetMyProperties()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var properties = await _repository.GetByUserAsync(userId);

            return Ok(properties);
        }

        // ======================================
        // UPDATE PROPERTY
        // ======================================

      
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(
            string id,
            Property updatedProperty)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var existing = await _repository.GetByIdAsync(id);

            if (existing == null)
                return NotFound();

            if (existing.UserId != userId)
                return Forbid();

            updatedProperty.Id = id;
            updatedProperty.UserId = userId;
            updatedProperty.CreatedAt = existing.CreatedAt;

            await _repository.UpdateAsync(updatedProperty);

            return Ok(new { message = "Property updated successfully" });
        }

        // ======================================
        // DELETE PROPERTY
        // ======================================

      
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var property = await _repository.GetByIdAsync(id);

            if (property == null)
                return NotFound();

            if (property.UserId != userId)
                return Forbid();

            await _repository.DeleteAsync(id);

            return Ok(new { message = "Property deleted successfully" });
        }

        // ======================================
        // FILTER SEARCH
        // ======================================

        [HttpGet("search")]
        [AllowAnonymous]
        public async Task<IActionResult> Search(
       string? city,
       string? locality,
       decimal? minPrice,
       decimal? maxPrice,
       string? bedrooms,
       string? transactionType,
       int page = 1,
       int pageSize = 10)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;

            var result = await _repository.SearchAsync(
                city,
                locality,
                minPrice,
                maxPrice,
                bedrooms,
                transactionType,
                page,
                pageSize);

            return Ok(result);
        }

        // ======================================
        // NEARBY SEARCH
        // ======================================

        [HttpGet("nearby")]
        public async Task<IActionResult> Nearby(
            double lat,
            double lng,
            double radiusKm = 5)
        {
            var properties = await _repository.GetNearbyAsync(
                lat,
                lng,
                radiusKm);

            return Ok(properties);
        }
    }
}
