using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using trackvisualizer.Geodetic;

namespace trackvisualizer.Service
{
    public class GeoLoaderService
    {
        private readonly IGeoLoaderMiddleware[] _middleware;

        public GeoLoaderService(IGeoLoaderMiddleware[] middleware)
        {
            _middleware = middleware;
        }

        public IEnumerable<Tuple<string, string[]>> GetAvailableFormatList() => 
            _middleware.Select(middleware => Tuple.Create(middleware.Description, middleware.Extensions));

        public async Task<Tuple<List<Track>, List<Point>>> LoadTrackAndSlicepointsAsync(string sourceFileName,
            string slicepointSourceFileName)
        {
            var matching = _middleware.FirstOrDefault(m => m.CanLoad(sourceFileName));

            if (null == matching)
                return null;

            return await matching.LoadTrackAndSlicepointsAsync(sourceFileName, slicepointSourceFileName);
        }
    }
}
