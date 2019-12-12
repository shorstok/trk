using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using trackvisualizer.Geodetic;

namespace trackvisualizer.Service
{
    public interface IGeoLoaderMiddleware
    {
        string Description { get; }
        string[] Extensions { get; }

        bool CanLoad(string sourceFileName);

        Task<Tuple<List<Track>, List<Point>>> LoadTrackAndSlicepointsAsync(string sourceFileName,
            string slicepointSourceFileName);
    }
}