using System;

namespace trackvisualizer.Geodetic
{
    /// <summary>
    /// </summary>
    public struct Point
    {
        public double Lat;
        public double Lon;
        public double? ElevationGpx;
        public string Name;
        public string Comment;
        public DateTime? DateTimeGpx;

        public Point(double latDg, double lonDg)
        {
            ElevationGpx = null;
            Name = null;
            Comment = null;
            DateTimeGpx = null;

            Lat = latDg;
            Lon = lonDg;
        }

        public static Point? Parse(string lat, string lon)
        {   
            if (!Geo.TryDecodeGeoDegrees(lat, out var latDegrees))
                return null;

            if (!Geo.TryDecodeGeoDegrees(lon, out var lonDegrees))
                return null;

            return new Point(latDegrees,lonDegrees);
        }

        public static bool operator >(Point a, Point b)
        {
            return a.Lat > b.Lat && a.Lon > b.Lon;
        }

        public static bool operator <(Point a, Point b)
        {
            return a.Lat < b.Lat && a.Lon < b.Lon;
        }
    }
}