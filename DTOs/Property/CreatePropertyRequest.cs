using Real_Estate_WebAPI.Models;

namespace Real_Estate_WebAPI.DTOs.Property
{
    public class CreatePropertyRequest
    {
        public PropertyFormData FormData { get; set; }
        public GeoLocation Location { get; set; }
        public DateTime SubmittedAt { get; set; }
    }

    public class PropertyFormData
    {
        public string PropertyCategory { get; set; }
        public string? UserId { get; set; }
        public string YouAreHereTo { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }

        public decimal Price { get; set; }
        public string PriceUnit { get; set; }

        public double Area { get; set; }
        public string AreaUnit { get; set; }

        public string Bedrooms { get; set; }
        public string Bathrooms { get; set; }
        public string Facing { get; set; }
        public string FloorNumber { get; set; }
        public string TotalFloors { get; set; }

        public string FullAddress { get; set; }
        public string City { get; set; }
        public string Locality { get; set; }

        public string ContactPersonName { get; set; }
        public string Email { get; set; }
        public string Whatsapp { get; set; }

        public List<string> UploadedImages { get; set; }
    }
}
