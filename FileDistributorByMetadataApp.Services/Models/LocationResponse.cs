namespace FileDistributorByMetadataApp.Services.Models
{
    public class LocationResponse
    {
        public Address Address { get; set; } = null!;
    }

    public class Address
    {
        public string Country { get; set; } = null!;
        public string City { get; set; } = null!;
        public string Village { get; set; } = null!;
        public string Amenity { get; set; } = null!;
    }
}
