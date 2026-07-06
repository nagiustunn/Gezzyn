using gezzyn.Domain.DTO;
using gezzyn.Domain.Entities;
using gezzyn.Domain.Enums;

namespace gezzyn.Application.Extensions
{
    public static class GooglePlaceMappingExtensions
    {
        public static Place ToPlaceEntity(this GooglePlaceResult g, string city)
        {
            return new Place
            {
                Name = g.Name,
                City = city,
                FormattedAddress = g.FormattedAddress,
                Description = g.EditorialSummary,
                Latitude = g.Latitude,
                Longitude = g.Longitude,
                GooglePlaceId = g.GooglePlaceId,
                GoogleRating = g.Rating,
                Category = MapCategory(g.PrimaryType, g.Types),
                Source = PlaceSource.Google,
                HasEntranceFee = false
            };
        }

        private static PlaceCategory MapCategory(string? primaryType, List<string> types)
        {
            var all = types.Append(primaryType ?? string.Empty).ToList();

            if (all.Any(t => t.Contains("museum"))) return PlaceCategory.Museum;
            if (all.Any(t => t.Contains("historical") || t.Contains("monument")))
                return PlaceCategory.HistoricalSite;
            if (all.Any(t => t.Contains("restaurant") || t.Contains("cafe") || t.Contains("food")))
                return PlaceCategory.Restaurant;
            if (all.Any(t => t.Contains("park") || t.Contains("natural")))
                return PlaceCategory.Nature;
            if (all.Any(t => t.Contains("shopping") || t.Contains("store") || t.Contains("market")))
                return PlaceCategory.Shopping;
            if (all.Any(t => t.Contains("church") || t.Contains("mosque") ||
                              t.Contains("synagogue") || t.Contains("place_of_worship")))
                return PlaceCategory.Religious;
            if (all.Any(t => t.Contains("lodging") || t.Contains("hotel")))
                return PlaceCategory.Accommodation;
            if (all.Any(t => t.Contains("amusement") || t.Contains("entertainment")))
                return PlaceCategory.Entertainment;

            return PlaceCategory.Other;
        }
    }
}
