using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using trackvisualizer.Geodetic;

namespace trackvisualizer.Service
{
    public class SrtmRepository
    {
        public List<Srtm> LoadedSrtms = new List<Srtm>();


        /// <summary>
        /// </summary>
        /// <param name="ptsToGuessFor"></param>
        /// <returns></returns>
        public static List<string> GuessAllFilenames(List<Point> ptsToGuessFor)
        {
            var rt = new List<string>();

            foreach (var pt in ptsToGuessFor)
            {
                var fn = Srtm.GuessFilename(pt);
                if (!rt.Contains(fn))
                    rt.Add(fn);
            }

            return rt;
        }

        /// <summary>
        /// </summary>
        /// <param name="pts"></param>
        /// <returns></returns>
        public async Task<List<string>> LoadSrtmsForPoints(List<Point> pts)
        {
            var fnames = GuessAllFilenames(pts);
            LoadedSrtms = new List<Srtm>();
            var resultingNames = new List<string>();

            foreach (var fn in fnames)
            {
                var srtm = await Srtm.FromFileAsync(fn + @".hgt");

                if (srtm == null) 
                    continue;

                LoadedSrtms.Add(srtm);
                
                resultingNames.Add(fn);
            }

            return resultingNames;
        }

        /// <summary>
        ///     Propagate call for corresponding SRTM
        /// </summary>
        /// <param name="pt"></param>
        /// <returns></returns>
        public double? GetHeightForPoint(Point pt)
        {
            //todo: Spatial hash. For now it's 1...4 SRTMs max, but...

            foreach (var srtm in LoadedSrtms)
                if (pt > srtm.PtSouthwest && pt < srtm.PtNortheast)
                    return srtm.GetHeightForPoint(pt);

            return null;
        }

    }
}