namespace gezzyn.Domain.DTO
{
    public class GooglePlaceResult
    {
        public string GooglePlaceId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? FormattedAddress { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double? Rating { get; set; }
        public int? UserRatingCount { get; set; }
        public string? PrimaryType { get; set; }
        public List<string> Types { get; set; } = new();
        public string? PhotoReferenceName { get; set; }  
        public bool? IsOpenNow { get; set; }
        public string? EditorialSummary { get; set; }     
        public int? PriceLevel { get; set; }            
    }
}
