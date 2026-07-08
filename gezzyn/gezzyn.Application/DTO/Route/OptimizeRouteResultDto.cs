using gezzyn.Application.DTO.PlaceVisit;

namespace gezzyn.Application.DTO.Route
{
    public class OptimizeRouteResultDto
    {
        public List<PlaceVisitDto> OrderedPlaces { get; set; } = new();
        public int TotalDistanceMeters { get; set; }
        public int TotalDurationSeconds { get; set; }
        public string? EncodedPolyline { get; set; }
    }
}
