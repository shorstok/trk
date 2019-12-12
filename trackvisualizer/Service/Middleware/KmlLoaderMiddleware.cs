using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SharpKml.Dom;
using SharpKml.Engine;
using trackvisualizer.Geodetic;
using Point = trackvisualizer.Geodetic.Point;

namespace trackvisualizer.Service.Middleware
{
    public class KmlLoaderMiddleware : IGeoLoaderMiddleware
    {
        public string Description => "KML Keyhole Markup Language";
        public string[] Extensions => new[] {"kml", "kmz"};
        
        public bool CanLoad(string sourceFileName) => sourceFileName.EndsWith(".kml",StringComparison.InvariantCultureIgnoreCase)||
                                                      sourceFileName.EndsWith(".kmz",StringComparison.InvariantCultureIgnoreCase);

        public Task<Tuple<List<Track>, List<Point>>> LoadTrackAndSlicepointsAsync(string sourceFileName, string slicepointSourceFileName)
        {
            List<Track> tracks;
            List<Point> points;
            
            using (var stream = File.Open(sourceFileName,FileMode.Open))
            {
                var root = ExtractElementFromFile(stream, sourceFileName.EndsWith("kmz"));

                tracks = new List<Track>();

                foreach (var lineString in root.Flatten().OfType<LineString>())
                {
                    var track = new Track
                    {
                        Name = lineString.Id,
                    };

                    var segment = new TrackSeg
                    {
                        Pts = lineString.Coordinates.Select(vector => new Point(vector.Latitude, vector.Longitude)
                        {
                            ElevationGpx = vector.Altitude
                        }).ToList()
                    };

                    segment.RecalcBounds();

                    track.Segments = new List<TrackSeg>{segment};

                    tracks.Add(track);
                }  
            }
            
            using (var stream = File.Open(slicepointSourceFileName,FileMode.Open))
            {
                var root = ExtractElementFromFile(stream, slicepointSourceFileName.EndsWith("kmz"));

                points = new List<Point>();

                foreach (var placemark in root.Flatten().OfType<Placemark>())
                {
                    if(!(placemark.Geometry is SharpKml.Dom.Point point))
                        continue;

                    if(point.Coordinate == null)
                        continue;
                    
                    points.Add(new Point(point.Coordinate.Latitude,point.Coordinate.Longitude)
                    {
                        ElevationGpx = point.Coordinate.Altitude,
                        Name = placemark.Name
                    });
                }  
            }

            return Task.FromResult(Tuple.Create(tracks, points));
        }

        private Element ExtractElementFromFile(Stream stream, bool isKmz)
        {
            return isKmz ? 
                KmzFile.Open(stream).GetDefaultKmlFile()?.Root :
                KmlFile.Load(stream)?.Root;
        }
    }
}