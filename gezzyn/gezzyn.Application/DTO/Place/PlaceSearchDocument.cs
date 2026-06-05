namespace gezzyn.Application.DTO.Place
{
    public class PlaceSearchDocument
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? City { get; set; }
        public string? District { get; set; }
        public string? FormattedAddress { get; set; }
        public string? Description { get; set; }
        public string? Category { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public double? Rating { get; set; }
        public bool HasEntranceFee { get; set; }
        public decimal? EntranceFeeAmount { get; set; }
        public string? Source { get; set; }
    }
}
