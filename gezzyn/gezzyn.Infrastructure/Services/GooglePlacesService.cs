using gezzyn.Domain.DTO;
using gezzyn.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace gezzyn.Infrastructure.Services
{
    public class GooglePlacesService : IGooglePlacesService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<GooglePlacesService> _logger;

        private const string BaseUrl = "https://places.googleapis.com/v1/places:searchText";

        private const string FieldMask ="places.id,places.displayName,places.formattedAddress," +
                                        "places.location,places.rating,places.userRatingCount," +
                                        "places.primaryType,places.types,places.photos," +
                                        "places.currentOpeningHours,places.editorialSummary," +
                                        "places.priceLevel";

        public GooglePlacesService(HttpClient httpClient, IConfiguration configuration, ILogger<GooglePlacesService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<List<GooglePlaceResult>> SearchPlacesAsync(string query, string city, CancellationToken ct = default)
        {
            var fullQuery = $"{query} {city}, Türkiye";
            return await ExecuteSearchAsync(fullQuery, ct);
        }

        public async Task<List<GooglePlaceResult>> SearchByCategoryAsync(string city, string category, CancellationToken ct = default)
        {
            var query = $"{category} in {city}, Türkiye";
            return await ExecuteSearchAsync(query, ct);
        }

        #region private methods

        private async Task<List<GooglePlaceResult>> ExecuteSearchAsync(string textQuery, CancellationToken ct)
        {
            var apiKey = _configuration["GooglePlaces:ApiKey"]
                ?? throw new InvalidOperationException("GooglePlaces:ApiKey ayarlanmamış. User Secrets kontrol et.");

            var requestBody = new
            {
                textQuery,
                languageCode = "tr",
                regionCode = "TR",
                maxResultCount = 20
            };

            var json = JsonSerializer.Serialize(requestBody);
            var request = new HttpRequestMessage(HttpMethod.Post, BaseUrl)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            request.Headers.Add("X-Goog-Api-Key", apiKey);
            request.Headers.Add("X-Goog-FieldMask", FieldMask);

            try
            {
                var response = await _httpClient.SendAsync(request, ct);
                response.EnsureSuccessStatusCode();

                var responseJson = await response.Content.ReadAsStringAsync(ct);
                return ParseResponse(responseJson);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Google Places API isteği başarısız: {Query}", textQuery);
                return new List<GooglePlaceResult>();
            }
        }

        private List<GooglePlaceResult> ParseResponse(string json)
        {
            var results = new List<GooglePlaceResult>();

            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("places", out var places))
                return results;

            foreach (var place in places.EnumerateArray())
            {
                var result = new GooglePlaceResult
                {
                    GooglePlaceId = GetString(place, "id") ?? string.Empty,
                    Name = GetDisplayNameText(place),
                    FormattedAddress = GetString(place, "formattedAddress"),
                    Rating = GetDouble(place, "rating"),
                    UserRatingCount = GetInt(place, "userRatingCount"),
                    PrimaryType = GetString(place, "primaryType"),
                    EditorialSummary = GetEditorialSummary(place),
                    PriceLevel = GetPriceLevel(place)
                };

                if (place.TryGetProperty("location", out var loc))
                {
                    result.Latitude = GetDouble(loc, "latitude") ?? 0;
                    result.Longitude = GetDouble(loc, "longitude") ?? 0;
                }

                if (place.TryGetProperty("types", out var types))
                {
                    result.Types = types.EnumerateArray()
                        .Select(t => t.GetString() ?? string.Empty)
                        .Where(t => !string.IsNullOrEmpty(t))
                        .ToList();
                }

                if (place.TryGetProperty("photos", out var photos) &&
                    photos.GetArrayLength() > 0)
                {
                    result.PhotoReferenceName = GetString(photos[0], "name");
                }

                if (place.TryGetProperty("currentOpeningHours", out var hours) &&
                    hours.TryGetProperty("openNow", out var openNow))
                {
                    result.IsOpenNow = openNow.GetBoolean();
                }

                results.Add(result);
            }

            return results;
        }

        private static string? GetString(JsonElement el, string prop)
            => el.TryGetProperty(prop, out var val) ? val.GetString() : null;

        private static double? GetDouble(JsonElement el, string prop)
            => el.TryGetProperty(prop, out var val) && val.ValueKind == JsonValueKind.Number
                ? val.GetDouble() : null;

        private static int? GetInt(JsonElement el, string prop)
            => el.TryGetProperty(prop, out var val) && val.ValueKind == JsonValueKind.Number
                ? val.GetInt32() : null;

        private static string GetDisplayNameText(JsonElement place)
        {
            if (place.TryGetProperty("displayName", out var dn) &&
                dn.TryGetProperty("text", out var text))
                return text.GetString() ?? string.Empty;
            return string.Empty;
        }

        private static string? GetEditorialSummary(JsonElement place)
        {
            if (place.TryGetProperty("editorialSummary", out var summary) &&
                summary.TryGetProperty("text", out var text))
                return text.GetString();
            return null;
        }

        private static int? GetPriceLevel(JsonElement place)
        {
            if (!place.TryGetProperty("priceLevel", out var pl)) return null;
            var str = pl.GetString();
            return str switch
            {
                "PRICE_LEVEL_FREE" => 0,
                "PRICE_LEVEL_INEXPENSIVE" => 1,
                "PRICE_LEVEL_MODERATE" => 2,
                "PRICE_LEVEL_EXPENSIVE" => 3,
                "PRICE_LEVEL_VERY_EXPENSIVE" => 4,
                _ => null
            };
        }

        #endregion

    }
}
