using gezzyn.API.Hubs;
using gezzyn.Domain.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace gezzyn.API.Services
{
    public class TripNotificationService : ITripNotificationService
    {
        private readonly IHubContext<TripHub> _hubContext;

        public TripNotificationService(IHubContext<TripHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task NotifyPlaceAdded(Guid tripId, object placeVisitDto)
        {
            await _hubContext.Clients.Group(GroupName(tripId))
                  .SendAsync("PlaceAdded", placeVisitDto);
        }

        public async Task NotifyPlaceRemoved(Guid tripId, Guid placeId)
        {
            await _hubContext.Clients.Group(GroupName(tripId))
                  .SendAsync("PlaceRemoved", new { placeId });
        }

        public async Task NotifyRouteOptimized(Guid tripId, object optimizeResultDto)
        {
            await _hubContext.Clients.Group(GroupName(tripId))
                  .SendAsync("RouteOptimized", optimizeResultDto);
        }

        public async Task NotifyPlacesReordered(Guid tripId, List<Guid> orderedPlaceIds)
        {
            await _hubContext.Clients.Group(GroupName(tripId))
                  .SendAsync("PlacesReordered", new { orderedPlaceIds });
        }

        public async Task NotifyMemberAdded(Guid tripId, object memberDto)
        {
            await _hubContext.Clients.Group(GroupName(tripId))
                  .SendAsync("MemberAdded", memberDto);
        }

        public async Task NotifyMemberRemoved(Guid tripId, Guid userId)
        {
            await _hubContext.Clients.Group(GroupName(tripId))
                  .SendAsync("MemberRemoved", new { userId });
        }
        private static string GroupName(Guid tripId) => $"trip-{tripId}";
    }
}
