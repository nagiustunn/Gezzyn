using gezzyn.Domain.DTO.Route;
using gezzyn.Domain.Enums;

namespace gezzyn.Domain.Interfaces
{
    public interface IRouteOptimizationService
    {
        Task<RouteOptimizationResult?> OptimizeRouteAsync(List<RoutePoint> points, TravelMode travelMode = TravelMode.Drive, CancellationToken ct = default);
    }
}
