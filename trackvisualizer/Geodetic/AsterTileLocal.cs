using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Windows.Media.Imaging;
using trackvisualizer.Properties;

namespace trackvisualizer.Geodetic
{
    public class AsterTileLocal
    {
        public const int AsterTileSidePointCount = 3601;

        public static Action<string> ProgressReporter = null;

        private static readonly string AsterServerAddr = @"113.35.103.196";

        /// <summary>
        ///     point heights in meters
        /// </summary>
        public short[,] GeotiffPoints;

        public Point PtSouthwest { get; set; }
        public Point PtNortheast { get; set; }

        public short[,] RawHeights => GeotiffPoints;

        private static byte[] DownloadAsArray(int lat, int lon)
        {
            var stream = GetAsterTileStream(lat, lon, out var contentLen);
            if (stream == null)
                return null;

            var ret = new byte[contentLen];

            stream.Read(ret, 0, (int) contentLen);

            return ret;
        }

        private static Stream GetAsterTileStream(int lat, int lon, out long contentLength)
        {
            var tileName = GetTileNameForLatLon(lat, lon);

            var rq =
                (HttpWebRequest)
                WebRequest.Create($"http://{AsterServerAddr}/gdServlet/Download?_gd_download_file_name={tileName}");

            rq.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";
            rq.Headers.Add("Accept-Encoding", "gzip, deflate");
            rq.Headers.Add("Accept-Language", "ru-ru,ru;q=0.8,en-us;q=0.5,en;q=0.3");
            rq.Referer = "http://www.gdem.aster.ersdac.or.jp/download.jsp";
            rq.Method = WebRequestMethods.Http.Post;

            var resp = rq.GetResponse();

            contentLength = resp.ContentLength;

            if (resp.ContentLength < 1e6)
                return null;

            return resp.GetResponseStream();
        }

        public static string GetTileNameForLatLon(int lat, int lon, bool zipname = true)
        {
            var hemisphereNs = lat > 0 ? @"N" : @"S";
            var hemisphereWe = lon < 0 ? @"W" : @"E";

            lat = Math.Abs(lat);
            lon = Math.Abs(lon);

            var basename = $@"ASTGTM2_{hemisphereNs}{lat:00}{hemisphereWe}{lon:000}";

            return zipname ? $@"{basename}.zip" : $@"{basename}_dem.tif";
        }

        private static void _rep_progress(string x)
        {
            ProgressReporter?.Invoke(x);
        }

        public static AsterTileLocal DownloadAndUnpack(int latDeg, int lonDeg, string basedir = null)
        {
            string tiffFilename = null;

            try
            {
                if (basedir == null)
                    basedir = Directory.GetCurrentDirectory();

                //test maybe geotiff exists

                tiffFilename = Path.Combine(basedir, GetTileNameForLatLon(latDeg, lonDeg, false));

                if (File.Exists(tiffFilename))
                {
                    _rep_progress(string.Format(Resources.AsterTileLocal_DownloadAndUnpack_UsingExistingGeotiffFormatted, tiffFilename));

                    return FromFile(tiffFilename, new Point(latDeg, lonDeg));
                }

                _rep_progress(string.Format(Resources.AsterTileLocal_DownloadAndUnpack_ConnectingToAsterServer, AsterServerAddr));

                var stream = GetAsterTileStream(latDeg, lonDeg, out var contentLen);

                if (stream == null)
                    return null;

                var temporaryZipFileName = Path.GetTempFileName().Replace(@".", @"_") + @".zip";

                var outputStream = File.OpenWrite(temporaryZipFileName);

                for (var i = 0; i < contentLen;)
                {
                    var blockLen = (int) Math.Min(1 << 12, contentLen - i);

                    var buffer = new byte[blockLen];
                    var nread = stream.Read(buffer, 0, blockLen);

                    if (nread == 0)
                        return null;

                    outputStream.Write(buffer, 0, nread);

                    i += nread;
                    _rep_progress(string.Format(Resources.AsterTileLocal_DownloadAndUnpack_LoadingTileFormatted, i / 1024, contentLen / 1024));
                }

                outputStream.Close();

                _rep_progress(Resources.AsterTileLocal_DownloadAndUnpack_UnpackingFormatted);

                using (var archive = ZipFile.OpenRead(temporaryZipFileName))
                {
                    foreach (var entry in archive.Entries)
                    {
                        if (!entry.FullName.EndsWith(@".tif", StringComparison.OrdinalIgnoreCase) ||
                            entry.FullName.IndexOf(@"_dem", StringComparison.Ordinal) == -1)
                            continue;

                        tiffFilename = Path.Combine(basedir, Path.GetFileName(entry.FullName));

                        entry.ExtractToFile(tiffFilename);
                    }
                }

                _rep_progress(Resources.AsterTileLocal_DownloadAndUnpack_Ready);
            }
            catch (Exception)
            {
                throw;
            }


            return FromFile(tiffFilename,
                new Point(latDeg, lonDeg));
        }

        public static AsterTileLocal FromFile(string geotiffFilename, Point ptSw)
        {
            var rt = new AsterTileLocal
            {
                PtSouthwest = ptSw,
                PtNortheast = new Point(ptSw.Lat + 1, ptSw.Lon + 1)
            };

            // Open a Stream and decode a GeoTIFF image
            Stream imageStreamSource = new FileStream(geotiffFilename, FileMode.Open, FileAccess.Read, FileShare.Read);
            var decoder = new TiffBitmapDecoder(imageStreamSource, BitmapCreateOptions.PreservePixelFormat,
                BitmapCacheOption.Default);
            BitmapSource bitmapSource = decoder.Frames[0];

            var pixels = new short[AsterTileSidePointCount * AsterTileSidePointCount];


            bitmapSource.CopyPixels(pixels, sizeof(short) * AsterTileSidePointCount, 0);

            //organize pixels in 2d array
            rt.GeotiffPoints = new short[AsterTileSidePointCount, AsterTileSidePointCount];
            //Buffer.BlockCopy(pixels, 0, rt.geotiff_points, 0, sizeof(short) * ASTERTileSidePointCount * ASTERTileSidePointCount);

            for (var yc = 0; yc < AsterTileSidePointCount; yc++)
            for (var xc = 0; xc < AsterTileSidePointCount; xc++)
                rt.GeotiffPoints[xc, yc] = pixels[xc + yc * AsterTileSidePointCount];

            return rt;
        }

        public void SaveAsHgt(string dir)
        {
            var srtmFilenameConv =
                $@"{(PtSouthwest.Lat > 0 ? @"N" : @"S")}{(int) PtSouthwest.Lat:00}{(PtSouthwest.Lon > 0 ? @"E" : @"W")}{(int) PtSouthwest.Lon:000}.hgt";

            using (var fs = File.OpenWrite(Path.Combine(dir, srtmFilenameConv)))
            {
                for (var yc = 0; yc != AsterTileSidePointCount; ++yc)
                for (var xc = 0; xc != AsterTileSidePointCount; ++xc)
                    fs.Write(BitConverter.GetBytes(RawHeights[xc, yc]).Reverse().ToArray(), 0, 2);
                fs.Close();
            }
        }

        public double? GetHeightForPoint(Point pt, bool disableInterpolation = false)
        {
            double px, py;
            if (!GetIndexForGeopoint(pt, out px, out py))
                return null;

            if (disableInterpolation)
                return GeotiffPoints[(int) px, (int) py];

            //todo: interpolate

            throw new NotImplementedException();
        }

        public bool GetIndexForGeopoint(Point pt, out double px, out double py)
        {
            px = py = 0;

            if (pt.Lon < PtSouthwest.Lon)
                return false;
            if (pt.Lat < PtSouthwest.Lat)
                return false;

            if (pt.Lon > PtNortheast.Lon)
                return false;
            if (pt.Lat > PtNortheast.Lat)
                return false;

            var kx = pt.Lon - PtSouthwest.Lon;
            var ky = 1 - (pt.Lat - PtSouthwest.Lat);

            px = AsterTileSidePointCount * kx;
            py = AsterTileSidePointCount * ky;

            return true;
        }
    }
}