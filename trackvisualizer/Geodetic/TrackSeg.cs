using System.Collections.Generic;
using System.Linq;

namespace trackvisualizer.Geodetic
{
    /// <summary>
    /// </summary>
    public class TrackSeg
    {
        public Point Ne;

        public List<Point> Pts;

        public Point Sw;

        public double Length
        {
            get
            {
                double rt = 0;

                if (Pts.Count < 2)
                    return 0;

                var pLast = Pts.First();

                foreach (var p in Pts)
                {
                    rt += Geo.DistanceExactMeters(pLast, p);
                    pLast = p;
                }

                return rt;
            }
        }

        /// <summary>
        /// </summary>
        public void RecalcBounds()
        {
            if (Pts == null || Pts.Count < 2)
                return;

            Sw = Ne = Pts.First();

            foreach (var p in Pts)
            {
                if (p.Lon < Sw.Lon)
                    Sw.Lon = p.Lon;
                if (p.Lat < Sw.Lat)
                    Sw.Lat = p.Lat;

                if (p.Lon > Ne.Lon)
                    Ne.Lon = p.Lon;
                if (p.Lat > Ne.Lat)
                    Ne.Lat = p.Lat;
            }
        }

        public class Slice
        {
            public int NPoint;
            public double NPointInterpolated;
            public Point SlicePoint;
        }
    }
}