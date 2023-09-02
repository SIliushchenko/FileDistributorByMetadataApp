namespace FileDistributorByMetadataApp.Services.Models
{
    public class FileData
    {
        public string FilePath { get; set; } = null!;
        public int Year { get; set; }
        public string Country { get; set; } = null!;
        public string City { get; set; } = null!;
        public string Village { get; set; }
    }
}
