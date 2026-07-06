using gezzyn.Domain.DTO;
namespace gezzyn.Domain.Interfaces
{
    public interface IGooglePlacesService
    {
        Task<List<GooglePlaceResult>> SearchPlacesAsync(string query, string city, CancellationToken ct = default);
        Task<List<GooglePlaceResult>> SearchByCategoryAsync(string city, string category, CancellationToken ct = default);
    }
}
