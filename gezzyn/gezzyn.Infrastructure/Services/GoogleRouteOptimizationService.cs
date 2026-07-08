using gezzyn.Domain.DTO.Route;
using gezzyn.Domain.Enums;
using gezzyn.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace gezzyn.Infrastructure.Services
{
    public class GoogleRouteOptimizationService : IRouteOptimizationService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<GoogleRouteOptimizationService> _logger;
        private const string BaseUrl = "https://routes.googleapis.com/directions/v2:computeRoutes";

        private const string FieldMask =
            "routes.optimizedIntermediateWaypointIndex," +
            "routes.duration,routes.distanceMeters," +
            "routes.polyline.encodedPolyline";

        public GoogleRouteOptimizationService(HttpClient httpClient, IConfiguration configuration, ILogger<GoogleRouteOptimizationService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<RouteOptimizationResult?> OptimizeRouteAsync(List<RoutePoint> points, TravelMode travelMode = TravelMode.Drive, CancellationToken ct = default)
        {
            if (points.Count < 2)
            {
                _logger.LogWarning("Optimizasyon için en az 2 nokta gerekli, {Count} geldi.", points.Count);
                return null;
            }

            var apiKey = _configuration["GooglePlaces:ApiKey"]
                ?? throw new InvalidOperationException("GooglePlaces:ApiKey ayarlanmamış.");

            var origin = points.First();
            var destination = points.Last();
            var intermediates = points.Skip(1).Take(points.Count - 2).ToList();

            var requestBody = new
            {
                origin = new
                {
                    location = new { latLng = new { latitude = origin.Latitude, longitude = origin.Longitude } }
                },
                destination = new
                {
                    location = new { latLng = new { latitude = destination.Latitude, longitude = destination.Longitude } }
                },
                intermediates = intermediates.Select(p => new
                {
                    location = new { latLng = new { latitude = p.Latitude, longitude = p.Longitude } }
                }).ToList(),

                travelMode = MapTravelMode(travelMode),

                routingPreference = travelMode == TravelMode.Drive ? "TRAFFIC_AWARE" : "TRAFFIC_UNAWARE",
                optimizeWaypointOrder = intermediates.Count > 0
            };

            var json = JsonSerializer.Serialize(requestBody,
                new JsonSerializerOptions { DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull });

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
                return ParseResponse(responseJson, origin, intermediates, destination);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Google Routes API isteği başarısız.");
                return null;
            }
        }

        private RouteOptimizationResult? ParseResponse(string json, RoutePoint origin, List<RoutePoint> intermediates, RoutePoint destination)
        {
            using var doc = JsonDocument.Parse(json);

            if (!doc.RootElement.TryGetProperty("routes", out var routes) ||
                routes.GetArrayLength() == 0)
            {
                _logger.LogWarning("Google Routes API'den rota dönmedi.");
                return null;
            }

            var route = routes[0];
            var result = new RouteOptimizationResult();

            if (route.TryGetProperty("distanceMeters", out var dist))
                result.TotalDistanceMeters = dist.GetInt32();

            if (route.TryGetProperty("duration", out var dur))
            {
                var durStr = dur.GetString() ?? "0s";
                if (int.TryParse(durStr.TrimEnd('s'), out var seconds))
                    result.TotalDurationSeconds = seconds;
            }

            if (route.TryGetProperty("polyline", out var poly) &&
                poly.TryGetProperty("encodedPolyline", out var encoded))
            {
                result.EncodedPolyline = encoded.GetString();
            }

            var orderedIds = new List<Guid> { origin.PlaceId };

            if (route.TryGetProperty("optimizedIntermediateWaypointIndex", out var indexArray) &&
                indexArray.ValueKind == JsonValueKind.Array)
            {
                foreach (var indexEl in indexArray.EnumerateArray())
                {
                    var idx = indexEl.GetInt32();
                    if (idx >= 0 && idx < intermediates.Count)
                        orderedIds.Add(intermediates[idx].PlaceId);
                }
            }
            else
            {
                orderedIds.AddRange(intermediates.Select(p => p.PlaceId));
            }

            orderedIds.Add(destination.PlaceId);
            result.OptimizedPlaceIds = orderedIds;

            return result;
        }

        private static string MapTravelMode(TravelMode mode) => mode switch
        {
            TravelMode.Drive => "DRIVE",
            TravelMode.Walk => "WALK",
            TravelMode.Bicycle => "BICYCLE",
            _ => "DRIVE"
        };
    }
}
