namespace gezzyn.Application.DTO.Trip
{
    public class CreateTripDto
    {
        public string? Title { get; set; }
        public string? City { get; set; }
        public string? Description { get; set; }
        public DateOnly? StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
    }
}
