using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using MongoDB.Driver.GeoJsonObjectModel;

namespace Real_Estate_WebAPI.Models
{
    public class Property
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonRepresentation(BsonType.ObjectId)]
        public string UserId { get; set; }

        public string PropertyCategory { get; set; }
        public string TransactionType { get; set; }
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

        // 🔥 Proper GeoJSON format
        public GeoJsonPoint<GeoJson2DCoordinates> Location { get; set; }

        public string ContactPersonName { get; set; }
        public string Email { get; set; }
        public string Whatsapp { get; set; }

        public List<string> UploadedImages { get; set; } = new();

        public string Status { get; set; } = "Pending";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class GeoLocation
    {
        public double Lat { get; set; }
        public double Lng { get; set; }
    }
}
