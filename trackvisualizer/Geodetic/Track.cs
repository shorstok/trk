using System.Collections.Generic;

namespace trackvisualizer.Geodetic
{
    /// <summary>
    /// </summary>
    public class Track
    {
        public string Name;
        public List<TrackSeg> Segments;

        public Track GetWithFusedSegments()
        {
            if (Segments.Count < 2)
                return this;

            var rt = new Track
            {
                Name = Name
            };

            rt.Segments = new List<TrackSeg>();
            rt.Segments.Add(new TrackSeg {Pts = new List<Point>()});

            foreach (var sg in Segments)
                rt.Segments[0].Pts.AddRange(sg.Pts);

            return rt;
        }
    }
}