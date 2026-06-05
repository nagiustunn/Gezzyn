namespace gezzyn.Application.DTO.Place
{
    public class PlaceDto
    {
        public Guid Id { get; set; }
        public string? Name { get; set; }
        public string? City { get; set; }
        public string? FormattedAddress { get; set; }
        public string? Description { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public string? GooglePlaceId { get; set; }
        public double? GoogleRating { get; set; }
        public string? PhotoUrl { get; set; }
        public string? Category { get; set; }
        public bool HasEntranceFee { get; set; }
        public decimal? EntranceFeeAmount { get; set; }
        public string? EntranceFeeNote { get; set; }
        public string? OpeningHoursJson { get; set; }
        public string? Source { get; set; }
    }
}
