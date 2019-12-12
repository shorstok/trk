using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using trackvisualizer.Annotations;
using trackvisualizer.Geodetic;
using trackvisualizer.Service;
using trackvisualizer.View;
using Point = trackvisualizer.Geodetic.Point;

namespace trackvisualizer.Vm
{
    public partial class TrackVm : INotifyPropertyChanged, IEquatable<TrackVm>
    {
        private bool _isLoaded;

        public string ShortName => Path.GetFileName(SourceTrackFileName);

        public string SourceTrackFileName { get; }
        public TrackSettings Settings { get; private set; }

        public List<Point> SourceSlicepoints { get; private set; } = new List<Point>();
        public List<Track> SourceTracks { get; private set; }= new List<Track>();

        private readonly HeightmapManagerVm _heightmapManager;
        private readonly IUiLoggingService _loggingService;
        private readonly GeoLoaderService _geoLoader;
        private readonly SrtmRepository _srtmRepository;
        private readonly Func<TrackVm, TrackReportVm> _trackReportGenerator;
        private bool _wptTimesValid;
        private bool _trackTimesValid;
        private TrackReportVm _report;
        private bool _areHeightmapsMissing;

        public TrackReportVm Report
        {
            get => _report;
            set
            {
                if (Equals(value, _report)) return;
                _report = value;
                OnPropertyChanged();
            }
        }


        public bool IsLoaded
        {
            get => _isLoaded;
            set
            {
                if (value == _isLoaded) return;
                _isLoaded = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<string> Errors { get;} = new ObservableCollection<string>();

        public bool WptTimesValid
        {
            get => _wptTimesValid;
            set
            {
                if (value == _wptTimesValid) return;
                _wptTimesValid = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(RealDataPresent));
            }
        }

        public bool TrackTimesValid
        {
            get => _trackTimesValid;
            set
            {
                if (value == _trackTimesValid) return;
                _trackTimesValid = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(RealDataPresent));
            }
        }

        public bool AreHeightmapsMissing
        {
            get => _areHeightmapsMissing;
            set
            {
                if (value == _areHeightmapsMissing) return;
                _areHeightmapsMissing = value;
                OnPropertyChanged();
            }
        }

        public bool RealDataPresent => TrackTimesValid || WptTimesValid;

        public TrackSeg ActiveSeg { get; private set; }

        public TrackVm(string filename,
            HeightmapManagerVm heightmapManager,
            IUiLoggingService loggingService,
            GeoLoaderService geoLoader,
            SrtmRepository srtmRepository,
            Func<TrackVm, TrackReportVm> trackReportGenerator)
        {
            SourceTrackFileName = filename;
            _heightmapManager = heightmapManager;
            _loggingService = loggingService;
            _geoLoader = geoLoader;
            _srtmRepository = srtmRepository;
            _trackReportGenerator = trackReportGenerator;
        }

        internal void RegisterError(string error)
        {
            Errors.Add(error);
            _loggingService.Log(error,true);
        }

        public async Task<bool> LoadAsync()
        {
            if (!Exists())
                return false;

            Settings = TrackSettings.TryLoadAlongsideTrack(SourceTrackFileName);

            Errors.Clear();

            if (!await LoadTracksFromFile(SourceTrackFileName))
                return false;

            Report = _trackReportGenerator(this);

            if (!await Report.CreateReportAsync())
                return false;

            IsLoaded = true;

            return true;
        }

        public async Task<bool> LoadTracksFromFile(string trackFilename)
        {
            var loadedTrack = await _geoLoader.LoadTrackAndSlicepointsAsync(
                trackFilename,
                Settings.SourceSeparateSlicepointSource ?? SourceTrackFileName);

            if (loadedTrack == null)
            {
                RegisterError("Ошибка при загрузке файла!");
                return false;
            }

            SourceTracks = loadedTrack.Item1;

            if (SourceTracks.Count == 0)
            {
                RegisterError("Нет трека в файле!");
                return false;
            }

            if (SourceTracks.Count > 1)
            {
                SourceTracks = new List<Track>(new[]
                {
                    new Track
                    {
                        Name = string.Join("*", SourceTracks.Select(t => t.Name).ToArray()),
                        Segments = SourceTracks.SelectMany(t => t.Segments).ToList()
                    }
                });

                _loggingService.Log($"Склеиваю {SourceTracks.Count} треков");
            }
            else
                _loggingService.Log($"Загружен 1 трек");

            ActiveSeg = SourceTracks?.FirstOrDefault()?.GetWithFusedSegments()?.Segments?.FirstOrDefault();

            //////////////////////////////////////////////////////////////////////////
            if (!ValidateSlicepoints(loadedTrack.Item2))
                return false;

            var ptsKey = SourceTracks.First().GetWithFusedSegments().Segments.First().Pts;
            var requiredSrtms = SrtmRepository.GuessAllFilenames(ptsKey);

            var loadedSrtmNames =
                _srtmRepository.LoadedSrtms.Select(srt => srt.Loadname.Replace(".hgt", "")).ToList();
            
            //////////////////////////////////////////////////////////////////////////
            if (requiredSrtms.Except(loadedSrtmNames).Any())
            {
                _loggingService.Log("Загрузка данных высот (SRTM)");
                _loggingService.Log("Для трека 1 необходимы файлы ");

                foreach (var sFile in requiredSrtms)
                    _loggingService.Log(sFile + ".hgt");

                _loggingService.Log("Загрузка файлов...");

                var srtmsOk = await _srtmRepository.LoadSrtmsForPoints(ptsKey);
                var strmsFailed = requiredSrtms.Except(srtmsOk).ToList();

                if (strmsFailed.Count > 0)
                {
                    RegisterError("На этапе загрузки SRTM не найдены следующие карты высот:");
                    foreach (var sFile in strmsFailed)
                    {
                        _heightmapManager.RegisterMissingSrtm(sFile);
                        _loggingService.Log(sFile + ".hgt", true);
                    }
                    
                    RegisterError("Их необходимо загрузить с сервера NASA или иного источника");
                    AreHeightmapsMissing = true;
                }
                else
                {
                    _loggingService.Log("Все SRTM файлы загружены успешно!");
                    AreHeightmapsMissing = false;
                }
            }

            _loggingService.Log("Все загружено без ошибок, всё есть!");
            DeterminePointsReality();
            
            return true;
        }

        
        public bool ValidateSlicepoints(List<Point> slicepoints)
        {
            SourceSlicepoints = slicepoints;

            if (SourceSlicepoints.Count < 2)
            {
                _loggingService.Log(
                    "В выбранном файле недостаточно точек для разбиения трека по дням! Теперь точки нужно загрузить отдельно, либо пересохранить выбранный .gpx файл уже с точками.",true);
                return false;
            }

            _loggingService.Log("Загружено " + SourceSlicepoints.Count + " точек, ок");

            return true;
        }

        private void DeterminePointsReality()
        {
            var AS = ActiveSeg;
            double velocity;

            TrackTimesValid = false;
            WptTimesValid = false;

            if (AS.Pts.First().DateTimeGpx == null || AS.Pts.Last().DateTimeGpx == null)
                TrackTimesValid = false;
            else
                try
                {
                    var tsDelta = (TimeSpan) (AS.Pts.Last().DateTimeGpx - AS.Pts.First().DateTimeGpx);

                    velocity = AS.Length / 1000 / tsDelta.TotalHours;

                    if (velocity < 50) // velo [21/12/2009 LysakA]
                        TrackTimesValid = true;
                }
                catch (Exception)
                {
                    TrackTimesValid = false;
                }

            var validtimePoints = SourceSlicepoints.Where(pt => pt.DateTimeGpx != null).OrderBy(pt => pt.DateTimeGpx).ToArray();

            if (validtimePoints.Count() > 1)
            {
                var p1 = validtimePoints.First();
                var p2 = validtimePoints.Last();

                var tsDelta = (TimeSpan) (p2.DateTimeGpx - p1.DateTimeGpx);
                var distKm = Geo.DistanceExactMeters(p1, p2) / 1000;

                velocity = distKm / tsDelta.TotalHours;

                if (velocity < 50) // velo [21/12/2009 LysakA]
                    WptTimesValid = true;
            }
        }


        public bool Exists() => 
            File.Exists(SourceTrackFileName);
        
        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }


    }
}