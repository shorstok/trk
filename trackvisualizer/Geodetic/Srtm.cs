using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace trackvisualizer.Geodetic
{
    /// <summary>
    ///     Each data file covers a one-degree-of-latitude by one-degree-of-longitude block of Earth's surface.
    ///     The first seven characters indicate the southwest corner of the block, with N, S, E, and W referring to north,
    ///     south, east, and west.
    ///     Thus, the "N34W119.hgt" file covers latitudes 34 to 35 North and longitudes 118-119 West
    ///     (this file includes downtown Los Angeles, California).
    ///     The filename extension ".hgt" simply stands for the word "height", meaning elevation.
    ///     It is NOT a format type. These files are in "raw" format (no headers and not compressed),
    ///     16-bit signed integers, elevation measured in meters above sea level, in a "geographic"
    ///     (latitude and longitude array) projection, with data voids indicated by -32768.
    ///     International 3-arc-second files have 1201 columns and 1201 rows of data, with a total filesize of 2,884,802 bytes
    ///     ( = 1201 x 1201 x 2).
    ///     United States 1-arc-second files have 3601 columns and 3601 rows of data, with a total filesize of 25,934,402 bytes
    ///     ( = 3601 x 3601 x 2).
    ///     http://www2.jpl.nasa.gov/srtm/faq.html
    /// </summary>
    public class Srtm
    {
        private const int SrtmPointsSideMax = 3601;
        // 6370997.0 is a radius of Earth (see proj -le)

        // Специальные значения для высоты
        private const short SrtmMin = -32000;
        private const short OutOfRange = -32767;
        private const short SrtmUndef = -32768;

        /// <summary>
        ///     true if voids detected during load process
        /// </summary>
        public bool HasVoids;

        public string OriginalFilename;
        public Point PtNortheast;

        /// <summary>
        ///     borders of loaded data
        /// </summary>
        public Point PtSouthwest;

        /// <summary>
        ///     point heights in meters
        /// </summary>
        public short[,] SrtmPoints;

        // Размер квадратной srtm-картинки
        public int SrtmPointsSide = 1201;

        public bool SubsampleHeights = true;

        // Размер одной точки в метрах вдоль меридиана
        private double srtm_point_size_m()
        {
            return 1.0 / SrtmPointsSide / 180.0 * Math.PI * 6370997.0;
        }


        /// <summary>
        ///     guess SRTM3 file name that holds 'pt_to_guess_for' point.
        ///     srtm filename is (N|S)nn(E|W)nnn,
        ///     for example N48E024 is for lat 48...49, lon 24...25.
        /// </summary>
        /// <param name="pt_to_guess_for"></param>
        /// <returns></returns>
        public static string GuessFilename(Point ptToGuessFor)
        {
            var latSw = Math.Floor(ptToGuessFor.Lat);
            var lonSw = Math.Floor(ptToGuessFor.Lon);

            var ns = latSw > 0 ? @"N" : @"S";
            var ew = lonSw > 0 ? @"E" : @"W";

            return ns + Math.Abs(latSw).ToString(@"00.") + ew + Math.Abs(lonSw).ToString(@"000.");
        }

        private struct SrtmPoint
        {
            public int X;
            public int Y;
            public int Value;

            public SrtmPoint(int x, int y, int value)
            {
                X = x;
                Y = y;
                Value = value;
            }

            public override string ToString()
            {
                return $@"{X}, {Y} : {Value}";
            }
        }

        private class SrtmVoid
        {
            public HashSet<SrtmPoint> Voids;
            public List<SrtmPoint> BorderPoints;

            public SrtmVoid(HashSet<SrtmPoint> voids, List<SrtmPoint> borderPoints)
            {
                Voids = voids;
                BorderPoints = borderPoints;
            }

            public short Interpolate(int x, int y)
            {
                double sumInvDistance = 0;
                double result = 0;

                foreach (var borderPoint in BorderPoints)
                {
                    var distanceSq = ((double) borderPoint.X - x) * ((double) borderPoint.X - x);
                    distanceSq+= ((double) borderPoint.Y - y) * ((double) borderPoint.Y - y);

                    var invDistSq = 1/(1+distanceSq);

                    sumInvDistance += invDistSq;

                    result += invDistSq * borderPoint.Value;
                }

                result /= sumInvDistance;

                return (short) result;
            }
        }

        private void GetVoidWithNeigborsRecursive(int x, int y, HashSet<SrtmPoint> result,
            HashSet<SrtmPoint> borders)
        {
            var queue = new Queue<SrtmPoint>();

            queue.Enqueue(new SrtmPoint(x,y,SrtmPoints[x, y]));

            do
            {
                var pt = queue.Dequeue();

                if (pt.Value < SrtmMin)
                {
                    if (!result.Add(pt))
                        continue;
                }
                else
                {
                    borders.Add(pt);
                    continue;
                }

                x = pt.X;
                y = pt.Y;
                
                if(x > 0)
                    queue.Enqueue(new SrtmPoint(x-1,y,SrtmPoints[x-1, y]));
                if(y > 0)
                    queue.Enqueue(new SrtmPoint(x,y-1,SrtmPoints[x, y-1]));
                if(x < SrtmPoints.GetLength(0)-1)
                    queue.Enqueue(new SrtmPoint(x+1,y,SrtmPoints[x+1, y]));
                if(y < SrtmPoints.GetLength(1)-1)
                    queue.Enqueue(new SrtmPoint(x,y+1,SrtmPoints[x, y+1]));

            } while (queue.Any());                       
        }

        public async Task<bool> CleanupSrtmVoidsAsync()
        {
            var result = (short[,])SrtmPoints.Clone();

            bool somethingChanged = false;
            
            void fixSrtmChunk(int xs, int xe)
            {
                var span = SrtmPoints.GetLength(0);

                var voidRegister = new List<SrtmVoid>();
                var visitedPoints = new HashSet<int>();

                for (int y = 0; y < SrtmPoints.GetLength(1); y++)
                {
                   
                    for (int x = xs; x < xe; x++)
                    {
                        if (SrtmPoints[x, y] < SrtmMin)
                        {
                            if(visitedPoints.Contains(x + y * span))
                                continue;                            

                            var voids = new HashSet<SrtmPoint>();
                            var borders = new HashSet<SrtmPoint>();

                            GetVoidWithNeigborsRecursive(x, y, voids, borders);

                            var srtmvoid = new SrtmVoid(voids,borders.ToList());

                            voidRegister.Add(srtmvoid);

                            foreach (var srtmPoint in voids)
                                visitedPoints.Add(srtmPoint.X + srtmPoint.Y * span);
                        }
                    }
                }

                if(!voidRegister.Any())
                    return;

                somethingChanged = true;

                for (var i = 0; i < voidRegister.Count; i++)
                {
                    var srtmVoid = voidRegister[i];
                    foreach (var point in srtmVoid.Voids)
                        result[point.X, point.Y] = srtmVoid.Interpolate(point.X, point.Y);
                }
            }
            
            var tasks = new List<Task>();

            var step = SrtmPoints.GetLength(0) / Environment.ProcessorCount;

            for (int nthread = 0; nthread < Environment.ProcessorCount; nthread++)
            {
                var xs = nthread * step;
                var xe = (nthread+1) * step;

                tasks.Add(Task.Run(() => fixSrtmChunk(xs,xe)));
            }

            await Task.WhenAll(tasks);

            SrtmPoints = result;
            HasVoids = false;

            return somethingChanged;
        }


        public async Task SaveSrtmAsync()
        {
            if(string.IsNullOrWhiteSpace(OriginalFilename))
                return;

            var buffer = new byte[2];

            using (var fileStream = File.Create(OriginalFilename))            
            {
                for (int yc = 0; yc < SrtmPoints.GetLength(1); yc++)
                {
                    for (int xc = 0; xc < SrtmPoints.GetLength(0); xc++)
                    {
                        ushort height = (ushort)SrtmPoints[xc, yc];

                        buffer[0] = (byte) (height >> 8);
                        buffer[1] = (byte) (height & 0xff);

                        if(yc% 100 != 0)
                            fileStream.Write(buffer,0,2);
                        else
                            await fileStream.WriteAsync(buffer,0,2);
                    }
                }
            }
        }


        /// <summary>
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public static async Task<Srtm> FromFileAsync(string filename, bool autoCleanVoids = true)
        {
            try
            {
                var pointsRead = 0;
                var buffer = new byte[524288];

                var result = new Srtm();

                if (!File.Exists(filename))
                    return null;

                using (var fileStream = File.OpenRead(filename))
                {
                    var pointCountGuess = (int) Math.Sqrt(fileStream.Length / 2);

                    if (pointCountGuess * pointCountGuess != fileStream.Length / 2)
                        return null;

                    result.SrtmPointsSide = pointCountGuess;
                    result.SrtmPoints = new short[result.SrtmPointsSide, result.SrtmPointsSide];
                    
                    int bytesRead = 0;

                    do
                    {
                        bytesRead = await fileStream.ReadAsync(buffer, 0, buffer.Length);

                        for (int i = 0; i < bytesRead; i+=2)
                        {
                            // change endianness [16/12/2009 LysakA]
                            short height = buffer[i];
                            height <<= 8;
                            height |= buffer[i + 1];

                            result.SrtmPoints[pointsRead % result.SrtmPointsSide, pointsRead / result.SrtmPointsSide] = height;

                            if (height < SrtmMin)
                                result.HasVoids = true;

                            pointsRead++;
                            
                            if(pointsRead > SrtmPointsSideMax * SrtmPointsSideMax)
                                return null; // invalid srtm [16/12/2009 LysakA]
                        }

                    } while (bytesRead == buffer.Length);

                    
                    if (pointsRead != result.SrtmPointsSide * result.SrtmPointsSide)
                        return null;

                    ParseSrtmFilename(filename, out var lonDeg, out var latDeg);

                    result.PtSouthwest = (Point) Point.Parse(latDeg, lonDeg);
                    result.PtNortheast = new Point(result.PtSouthwest.Lat + 1, result.PtSouthwest.Lon + 1);

                    result.OriginalFilename = filename;
                }

                if (result.HasVoids && autoCleanVoids)
                {
                    if (await result.CleanupSrtmVoidsAsync())
                        await result.SaveSrtmAsync();
                }

                return result;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static void ParseSrtmFilename(string fn, out string lonDeg, out string latDeg)
        {
            var ns = fn.Substring(0, 1);
            var ew = fn.Substring(3, 1);

            latDeg = fn.Substring(1, 2);
            lonDeg = fn.Substring(4, 3);

            if (ns == @"s" || ns == @"S")
                latDeg = @"-" + latDeg;
            if (ew == @"w" || ew == @"W")
                lonDeg = @"-" + lonDeg;
        }

        public double? GetHeightForPoint(Point pt)
        {
            if (pt.Lon < PtSouthwest.Lon)
                return null;
            if (pt.Lat < PtSouthwest.Lat)
                return null;

            if (pt.Lon > PtNortheast.Lon)
                return null;
            if (pt.Lat > PtNortheast.Lat)
                return null;

            var kx = pt.Lon - PtSouthwest.Lon;
            var ky = 1 - (pt.Lat - PtSouthwest.Lat);

            var fx = SrtmPointsSide * kx;
            var fy = SrtmPointsSide * ky;

            if (SubsampleHeights && fx > 1 && fx < SrtmPointsSide - 1 && fy > 1 && fy < SrtmPointsSide - 1)
            {
                var ktX = fx - Math.Floor(fx);
                var ktY = fy - Math.Floor(fy);

                var ix = (int) Math.Floor(fx);
                var iy = (int) Math.Floor(fy);

                var rt = SrtmPoints[ix, iy] * (1 - ktX) * (1 - ktY);
                rt += SrtmPoints[ix + 1, iy] * ktX * (1 - ktY);
                rt += SrtmPoints[ix, iy + 1] * (1 - ktX) * ktY;
                rt += SrtmPoints[ix + 1, iy + 1] * ktX * ktY;

                return rt;
            }

            return SrtmPoints[(int) (SrtmPointsSide * kx), (int) (SrtmPointsSide * ky)];
        }
    }
}