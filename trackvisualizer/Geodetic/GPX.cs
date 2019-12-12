using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;

namespace trackvisualizer.Geodetic
{
    public class Gpx
    {
        public static bool ForceDateTimeFromComment = true;
    
        /// <summary>
        /// </summary>
        /// <param name="wpt"></param>
        /// <returns></returns>
        private static DateTime? GetDateTimeFromWpt(XElement wpt)
        {
            var ns = wpt.Name.Namespace;
            string sDate = null;

            // first try to get xsD [16/12/2009 LysakA]
            try
            {
                if (wpt.Element(ns + "time") != null &&
                    (!ForceDateTimeFromComment || wpt.Name.LocalName == "trkpt"))
                    return DateTime.Parse(wpt.Element(ns + "time").Value,
                        CultureInfo.InvariantCulture);
            }
            catch (Exception)
            {
            }
            
            sDate = wpt.Element(ns + "cmt")?.Value ?? wpt.Element(ns + "desc")?.Value;

            if (sDate == null) 
                return null;

            return !DateTime.TryParse(sDate, out var date) ? (DateTime?) null : date;
        }

        /// <summary>
        ///     When passed a file, open it and parse all waypoints from it.
        /// </summary>
        /// <param name="sFile">Fully qualified file name (local)</param>
        public static List<Point> LoadGpxWaypoints(string sFile)
        {
            try
            {
                var autoNameCounter = 0;

                var gpxDoc = XDocument.Load(sFile);
                var gpx = gpxDoc.Root.Name.Namespace;

                var waypoints = from waypoint in gpxDoc.Descendants(gpx + "wpt")
                    select new Point
                    {
                        Lat = Geo.DecodeGeoDegrees(waypoint.Attribute("lat")?.Value),
                        Lon = Geo.DecodeGeoDegrees(waypoint.Attribute("lon")?.Value),
                        ElevationGpx = waypoint.Element(gpx + "ele") != null
                            ? double.Parse(Geo.CleanupLatLonHgtForConversion(waypoint.Element(gpx + "ele")?.Value))
                            : (double?) null,
                        Name = waypoint.Element(gpx + "name")?.Value ?? "AUTO" + (++autoNameCounter).ToString("000"),
                        Comment = waypoint.Element(gpx + "cmt")?.Value,
                        DateTimeGpx = GetDateTimeFromWpt(waypoint)
                    };

                return waypoints.ToList();
            }
            catch (Exception)
            {
                return new List<Point>(); //empty.
            }
        }

        /// <summary>
        ///     When passed a file, open it and parse all tracks
        ///     and track segments from it.
        /// </summary>
        /// <param name="sFile">Fully qualified file name (local)</param>
        /// <returns>
        ///     string containing line delimited waypoints from the
        ///     file (for test)
        /// </returns>
        public static List<Track> LoadGpxTracks(string sFile)
        {
            try
            {
                var gpxDoc = XDocument.Load(sFile);
                var gpx = gpxDoc.Root.Name.Namespace;

                var trkList = (from track in gpxDoc.Descendants(gpx + "trk")
                    select new Track
                    {
                        Name = track.Element(gpx + "name")?.Value ?? track.Element(gpx + "desc")?.Value,

                        Segments = (
                            from tracksegment in track.Descendants(gpx + "trkseg")
                            select new TrackSeg
                            {
                                Pts =
                                (
                                    from trackpoint in tracksegment.Descendants(gpx + "trkpt")
                                    select new Point
                                    {
                                        Lat = Geo.DecodeGeoDegrees(trackpoint.Attribute("lat")?.Value),
                                        Lon = Geo.DecodeGeoDegrees(trackpoint.Attribute("lon")?.Value),
                                        ElevationGpx =
                                            trackpoint.Element(gpx + "ele") != null
                                                ? double.Parse(
                                                    Geo.CleanupLatLonHgtForConversion(trackpoint.Element(gpx + "ele")?
                                                        .Value))
                                                : (double?) null,
                                        DateTimeGpx = GetDateTimeFromWpt(trackpoint)
                                    }
                                ).ToList()
                            }
                        ).ToList()
                    }).ToList();

                foreach (var trk in trkList)
                {
                    if (trk.Segments == null)
                        continue;

                    foreach (var seg in trk.Segments)
                        seg.RecalcBounds();
                }

                return trkList;
            }
            catch (Exception)
            {
                return new List<Track>(); //empty.
            }
        }
    }
}