using gezzyn.Domain.Enums;

namespace gezzyn.Domain.Entities
{
    public class Place : BaseEntity
    {
        public string? Name { get; set; } 
        public string? Description { get; set; }
        public string? FormattedAddress { get; set; }
        public string? City { get; set; }
        public string? District { get; set; }
        public string Country { get; set; } = "TR";
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public string? GooglePlaceId { get; set; }
        public double? GoogleRating { get; set; }
        public string? PrimaryPhotoUrl { get; set; }
        public string? GoogleMapsUrl { get; set; }
        public bool HasEntranceFee { get; set; } = false;
        public decimal? EntranceFeeAmount { get; set; }
        public string? EntranceFeeNote { get; set; }
        public string? OpeningHoursJson { get; set; }
        public PlaceCategory Category { get; set; } = PlaceCategory.Other;
        public PlaceSource Source { get; set; } = PlaceSource.Manual;

        public ICollection<PlaceVisit>? TripVisits { get; set; }
    }
}
