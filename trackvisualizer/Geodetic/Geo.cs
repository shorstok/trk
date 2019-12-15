using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using trackvisualizer.Service;

namespace trackvisualizer.Geodetic
{
    public static class Geo
    {
        private static readonly string DecimalSeparator = @".";
        private static readonly string NotDecimalSeparator = @",";

        // from http://en.wikipedia.org/wiki/Earth_radius [16/12/2009 LysakA]
        // it seems like Ozi is using the same values

        private static readonly double EarthRadiusMajor = 6384400;
        private static readonly double EarthRadiusMinor = 6352800;


        private const double ExSearchStepM = 5;

        static Geo()
        {
            DecimalSeparator = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
            NotDecimalSeparator = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator == "," ? @"." : @",";
        }

        /// <summary>
        ///     cleans spaces and prepares for parsing, regardless current culture
        /// </summary>
        /// <param name="latlon"></param>
        /// <returns></returns>
        public static string CleanupLatLonHgtForConversion(string latlon)
        {
            return latlon.Trim().Replace(NotDecimalSeparator, DecimalSeparator);
        }


        /// <summary>
        ///     converts any string lat,lon into decimal degrees. doesnt check bounds.
        /// </summary>
        /// <param name="str">degrees in any form</param>
        /// <returns>decimal degrees (-90..90lat or -180..180lon)</returns>
        public static bool TryDecodeGeoDegrees(string str, out double value)
        {
            str = CleanupLatLonHgtForConversion(str);

            if (str.IndexOfAny(new[] {'N', 'S', 'E', 'W', ' ', '\''}) == -1)
            {
                value = double.Parse(str);
                return true;
            }

            //throw new NotImplementedException("dont know how to parse complex DMS/DM.MMM/...");

            value = 0;
            return false;
        }

        public static double DecodeGeoDegrees(string str) => TryDecodeGeoDegrees(str, out var value)
            ? value
            : throw new ArgumentException(nameof(str));

        private static double CalcSegmentIteratorsRetSeLen(Point s, Point e, out double dlat, out double dlon,
            out int stepCount)
        {
            var disLen = DistanceExactMeters(s, e);

            stepCount = (int) Math.Round(disLen / ExSearchStepM);

            if (stepCount == 0)
                stepCount = 1;

            dlat = (e.Lat - s.Lat) / stepCount;
            dlon = (e.Lon - s.Lon) / stepCount;

            return disLen;
        }

        private static void GetSegmentDeltas(Point s, Point e,
            out double ascent,
            out double descent,
            out double hMax,
            out double hMin,
            SrtmRepository heightCache)
        {
            ascent = descent = 0;

            CalcSegmentIteratorsRetSeLen(s, e, out var stepLat, out var stepLon, out var nSteps);

            double latI = s.Lat;
            double lonI = s.Lon;

            var hprev = heightCache.GetHeightForPoint(new Point(latI, lonI)) ?? 0;

            hMin = hMax = hprev;

            for (var c = 1; c < nSteps; ++c)
            {
                latI += stepLat;
                lonI += stepLon;

                var h = heightCache.GetHeightForPoint(new Point(latI, lonI)) ?? 0;

                if (h > hprev)
                    ascent += h - hprev;
                if (h < hprev)
                    descent += -(h - hprev);

                if (h < hMin)
                    hMin = h;
                if (h > hMax)
                    hMax = h;

                hprev = h;
            }
        }


        public static void GetHeightDetailsAccurate(List<Point> lst,
            out double ascentTotal,
            out double descentTotal,
            out double hstart,
            out double hend,
            out double hmax,
            out double hmin,
            out int ihmax,
            out int ihmin,
            SrtmRepository heightCache)
        {
            ascentTotal = 0;
            descentTotal = 0;

            hmax = hmin = hstart = heightCache.GetHeightForPoint(lst.First())??0;
            hend = heightCache.GetHeightForPoint(lst.Last())??0;
            ihmax = ihmin = 0;

            for (var c = 0; c < lst.Count - 1; ++c)
            {
                GetSegmentDeltas(lst[c], lst[c + 1], out var tacc, out var tdesc,
                    out var thmax,
                    out var thmin,
                    heightCache);

                if (hmax < thmax)
                {
                    hmax = thmax;
                    ihmax = c;
                }

                if (hmin > thmin)
                {
                    hmin = thmin;
                    ihmin = c;
                }

                ascentTotal += tacc;
                descentTotal += tdesc;
            }
        }


        /// <summary>
        ///     distance in 'degrees'
        /// </summary>
        /// <param name="start">start pt</param>
        /// <param name="end">end pt</param>
        /// <returns>distance in degrees</returns>
        public static double DistanceDegrees(Point start, Point end)
        {
            var latD = end.Lat - start.Lat;
            var lonDNorm = (end.Lon - start.Lon) * 2;

            return Math.Sqrt(latD * latD + lonDNorm * lonDNorm);
        }

        public static double DistanceExactMeters(Point start, Point end)
        {
            // Convert all values to radians
            var lon1 = start.Lon * Math.PI / 180;
            var lat1 = start.Lat * Math.PI / 180;
            var lon2 = end.Lon * Math.PI / 180;
            var lat2 = end.Lat * Math.PI / 180;

            // Calculate the distance
            var s1 = Math.Sin((lat2 - lat1) / 2);
            var s2 = Math.Sin((lon2 - lon1) / 2);
            var a = s1 * s1 + Math.Cos(lat1) * Math.Cos(lat2) * s2 * s2;
            var c = 2 * Math.Asin(Math.Min(1, Math.Sqrt(a)));

            // Calculate the radius of the earth at this latitude
            var r = EarthRadiusMajor - (EarthRadiusMajor - EarthRadiusMinor) * s1;

            // XXX Make the earth radius vary with latitude
            return r * c;
        }

        public static TrackSeg.Slice SliceTrackSegmentWithPoint(Point pt, List<Point> pts,
            double? sliceptKickOutRadiusMeters)
        {
            var rt = new TrackSeg.Slice {NPoint = -1};

            var dislist = new List<double>(); // distances for each track point [16/12/2009 LysakA]
            double? mindis = null;
            var ptCount = 0;

            foreach (var segpt in pts)
            {
                var dis = DistanceExactMeters(pt, segpt);

                if (sliceptKickOutRadiusMeters.HasValue && dis > sliceptKickOutRadiusMeters
                ) //Exclude too distant points
                    continue;

                if (null == mindis || dis < mindis)
                {
                    mindis = dis;
                    rt.NPoint = ptCount;
                }

                ptCount++;
                dislist.Add(dis);
            }


            rt.SlicePoint = pt;

            return rt.NPoint == -1 ? null : rt;
        }
    }
}