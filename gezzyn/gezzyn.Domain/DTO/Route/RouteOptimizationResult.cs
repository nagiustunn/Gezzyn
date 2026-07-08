namespace gezzyn.Domain.DTO.Route
{
    public class RouteOptimizationResult
    {
        public List<Guid> OptimizedPlaceIds { get; set; } = new();
        public int TotalDistanceMeters { get; set; }
        public int TotalDurationSeconds { get; set; }
        public string? EncodedPolyline { get; set; }
    }
}
