namespace Real_Estate_WebAPI.DTOs.Property
{
    public class PropertyImageRequest
    {
        public string FileName { get; set; }

        public string ContentType { get; set; }

        // Base64 image string
        public string Data { get; set; }
    }
}
