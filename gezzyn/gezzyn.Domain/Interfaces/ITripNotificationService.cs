namespace gezzyn.Domain.Interfaces
{
    public interface ITripNotificationService
    {
        Task NotifyPlaceAdded(Guid tripId, object placeVisitDto);
        Task NotifyPlaceRemoved(Guid tripId, Guid placeId);
        Task NotifyRouteOptimized(Guid tripId, object optimizeResultDto);
        Task NotifyPlacesReordered(Guid tripId, List<Guid> orderedPlaceIds);
        Task NotifyMemberAdded(Guid tripId, object memberDto);
        Task NotifyMemberRemoved(Guid tripId, Guid userId);
    }
}
