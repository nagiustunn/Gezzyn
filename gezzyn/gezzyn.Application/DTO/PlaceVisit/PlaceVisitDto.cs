namespace gezzyn.Application.DTO.PlaceVisit
{
    public class PlaceVisitDto
    {
        public Guid Id { get; set; }
        public Guid PlaceId { get; set; }
        public string? PlaceName { get; set; }
        public string? PlaceAddress { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public int Order { get; set; }
        public string? Status { get; set; }
        public string? Note { get; set; }
        public string? PlannedArrivalTime { get; set; }
        public int? EstimatedDurationMinutes { get; set; }
        public bool HasEntranceFee { get; set; }
        public decimal? EntranceFeeAmount { get; set; }
        public string? EntranceFeeNote { get; set; }
        public Guid AddedByUserId { get; set; }
        public string? AddedByUserName { get; set; }
    }
}
