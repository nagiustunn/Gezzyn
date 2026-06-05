using gezzyn.Application.DTO.Place;
using gezzyn.Application.DTO.PlaceVisit;
using gezzyn.Application.DTO.Trip;
using gezzyn.Application.DTO.TripMembership;
using gezzyn.Application.DTO.User;
using gezzyn.Domain.Entities;

namespace gezzyn.Application.Extensions
{
    public static class MappingExtensions
    {
        public static UserDto ToDto(this User user) => new UserDto
        {
            Id = user.Id,
            Name = user.Name,
            Surname = user.Surname,
            UserName = user.UserName,
            Email = user.Email,
            AvatarUrl = user.AvatarUrl
        };

        public static TripDto ToDto(this Trip trip) => new TripDto
        {
            Id = trip.Id,
            Title = trip.Title,
            City = trip.City,
            Description = trip.Description,
            Status = trip.Status.ToString(),
            InviteCode = trip.InviteCode,
            StartDate = trip.StartDate,
            EndDate = trip.EndDate,
            MemberCount = trip.Members.Count(m => !m.IsDeleted),
            PlaceCount = trip.PlaceVisits.Count(pv => !pv.IsDeleted),
            Members = trip.Members
                .Where(m => !m.IsDeleted)
                .Select(m => m.ToDto())
                .ToList(),
            PlaceVisits = trip.PlaceVisits
                .Where(pv => !pv.IsDeleted)
                .OrderBy(pv => pv.Order)
                .Select(pv => pv.ToDto())
                .ToList(),
            CreatedAt = trip.CreatedAt
        };

        public static TripMemberDto ToDto(this TripMember m) => new TripMemberDto
        {
            UserId = m.UserId,
            FullName = m.User?.FullName ?? string.Empty,
            UserName = m.User?.UserName ?? string.Empty,
            AvatarUrl = m.User?.AvatarUrl,
            Role = m.Role.ToString(),
            JoinedAt = m.JoinedAt
        };

        public static PlaceVisitDto ToDto(this PlaceVisit pv) => new PlaceVisitDto
        {
            Id = pv.Id,
            PlaceId = pv.PlaceId,
            PlaceName = pv.Place?.Name ?? string.Empty,
            PlaceAddress = pv.Place?.FormattedAddress,
            Latitude = pv.Place?.Latitude,
            Longitude = pv.Place?.Longitude,
            Order = pv.Order,
            Status = pv.Status.ToString(),
            Note = pv.Note,
            PlannedArrivalTime = pv.PlannedArrivalTime,
            EstimatedDurationMinutes = pv.EstimatedDurationMinutes,
            HasEntranceFee = pv.Place?.HasEntranceFee ?? false,
            EntranceFeeAmount = pv.Place?.EntranceFeeAmount,
            EntranceFeeNote = pv.Place?.EntranceFeeNote,
            AddedByUserId = pv.AddedByUserId,
            AddedByUserName = pv.AddedBy?.UserName ?? string.Empty
        };

        public static PlaceDto ToDto(this Place p) => new PlaceDto
        {
            Id = p.Id,
            Name = p.Name,
            City = p.City,
            FormattedAddress = p.FormattedAddress,
            Description = p.Description,
            Latitude = p.Latitude,
            Longitude = p.Longitude,
            GooglePlaceId = p.GooglePlaceId,
            GoogleRating = p.GoogleRating,
            PhotoUrl = p.PrimaryPhotoUrl,
            Category = p.Category.ToString(),
            HasEntranceFee = p.HasEntranceFee,
            EntranceFeeAmount = p.EntranceFeeAmount,
            EntranceFeeNote = p.EntranceFeeNote,
            OpeningHoursJson = p.OpeningHoursJson,
            Source = p.Source.ToString()
        };

        public static PlaceSearchDocument ToSearchDocument(this Place p) => new PlaceSearchDocument
        {
            Id = p.Id.ToString(),
            Name = p.Name,
            City = p.City,
            District = p.District,
            FormattedAddress = p.FormattedAddress,
            Description = p.Description,
            Category = p.Category.ToString(),
            Latitude = p.Latitude,
            Longitude = p.Longitude,
            Rating = p.GoogleRating,
            HasEntranceFee = p.HasEntranceFee,
            EntranceFeeAmount = p.EntranceFeeAmount,
            Source = p.Source.ToString()
        };
    }
}
