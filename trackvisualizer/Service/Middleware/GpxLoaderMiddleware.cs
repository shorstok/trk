using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using trackvisualizer.Geodetic;

namespace trackvisualizer.Service.Middleware
{
    public class GpxLoaderMiddleware : IGeoLoaderMiddleware
    {
        public string Description => "GPX Gps Exchange format";
        public string[] Extensions => new[] {"gpx"};
        
        public bool CanLoad(string sourceFileName) => sourceFileName.EndsWith(".gpx",StringComparison.InvariantCultureIgnoreCase);

        public Task<Tuple<List<Track>, List<Point>>> LoadTrackAndSlicepointsAsync(string sourceFileName, string slicepointSourceFileName)
        {
            return Task.FromResult(Tuple.Create(
                Gpx.LoadGpxTracks(sourceFileName),
                Gpx.LoadGpxWaypoints(slicepointSourceFileName)));
        }
    }
}