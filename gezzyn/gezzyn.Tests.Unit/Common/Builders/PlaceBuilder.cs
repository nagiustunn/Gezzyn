using gezzyn.Domain.Entities;
using gezzyn.Domain.Enums;

namespace gezzyn.Tests.Unit.Common.Builders
{
    public class PlaceBuilder
    {
        private Guid _id = Guid.NewGuid();
        private string _name = "Deyrulzafaran Manastırı";
        private string _city = "Mardin";
        private double _lat = 37.2940;
        private double _lon = 40.7830;

        public PlaceBuilder WithId(Guid id) { _id = id; return this; }
        public PlaceBuilder WithName(string name) { _name = name; return this; }
        public PlaceBuilder WithCoordinates(double lat, double lon) { _lat = lat; _lon = lon; return this; }

        public Place Build() => new()
        {
            Id = _id,
            Name = _name,
            City = _city,
            Latitude = _lat,
            Longitude = _lon,
            Source = PlaceSource.Google
        };
    }
}
