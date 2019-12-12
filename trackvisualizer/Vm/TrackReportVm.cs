using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using trackvisualizer.Annotations;
using trackvisualizer.Config;
using trackvisualizer.Geodetic;
using trackvisualizer.Service;
using Point = trackvisualizer.Geodetic.Point;

namespace trackvisualizer.Vm
{
    public class TagGraphData
    {
        public List<KeyValuePair<double, double>> Profile;
        public List<double> DaySections;
        public List<KeyValuePair<double, double>> Extremities;
    }

    public class TrackReportVm : INotifyPropertyChanged
    {
        private readonly IUiLoggingService _loggingService;
        private readonly SrtmRepository _srtmRepository;
        private readonly Func<TrackVm,TrackReportItemVm> _reportItemGenerator;
        private readonly TrekplannerConfiguration _configuration;
        public TrackVm Source { get; }

        public double
            StartEndCapDistance =
                500; //m, move start point to track start in case of placement error[18/12/2009 LysakA]

        private List<TrackSeg.Slice> _slicesCalc;

        public ObservableCollection<TrackReportItemVm> Results { get; } = new ObservableCollection<TrackReportItemVm>();

        public TagGraphData GraphData { get; set; }
        
        public TrackReportVm(TrackVm source, 
            IUiLoggingService loggingService,
            SrtmRepository srtmRepository,
            Func<TrackVm,TrackReportItemVm> reportItemGenerator,
            TrekplannerConfiguration configuration)
        {
            _loggingService = loggingService;
            _srtmRepository = srtmRepository;
            _reportItemGenerator = reportItemGenerator;
            _configuration = configuration;
            Source = source;
        }

        
        private List<Point> FilterSlicepts()
        {
            var filteredSlicepoints = (from Point pt in Source.SourceSlicepoints
                where !Source.Settings.ExcludedPoints.Contains(pt.Name)
                select pt).ToList();

            if (!_configuration.ReportGeneratorOptions.FilterPassesByName)
                return Source.SourceSlicepoints;
            
            var passsuff = new List<string>();

            for (var diff = 1; diff < 7; ++diff)
            {
                passsuff.Add(" " + diff + "a");
                passsuff.Add(" " + diff + "b");
                passsuff.Add(" " + diff + "а");
                passsuff.Add(" " + diff + "б");
                passsuff.Add(" " + diff + "A");
                passsuff.Add(" " + diff + "B");
                passsuff.Add(" " + diff + "А");
                passsuff.Add(" " + diff + "Б");
            }

            filteredSlicepoints = filteredSlicepoints.Where(
                pt => passsuff.All(ps => pt.Name.IndexOf(ps, StringComparison.Ordinal) == -1)).ToList();

            return filteredSlicepoints;
        }

        public async Task<bool> CreateReportAsync()
        {
            var activeSeg = Source.ActiveSeg;
            var activeSegPts = activeSeg?.Pts;

            Results.Clear();

            if (Source.AreHeightmapsMissing)
            {
                Source.RegisterError("Расчет прекращен из-за отсутствия карты высот");
                return false;
            }

            if (activeSegPts == null)
            {
                Source.RegisterError("Не загружен трек");
                return false;
            }
            
            if (activeSegPts.Count < 2)
            {
                Source.RegisterError("Недостаточно точек в треке");
                return false;
            }

            var points = FilterSlicepts();

            if (points.Count < 2)
            {
                Source.RegisterError("Недостаточно точек разбиения на дни");
                return false;
            }

            // everything is ok, can calculate [17/12/2009 LysakA]
            _slicesCalc = new List<TrackSeg.Slice>();

            _loggingService.Log("Разбиение трека на дни");

            GraphData = new TagGraphData();
            
            foreach (var pt in points)
            {
                var slice = Geo.SliceTrackSegmentWithPoint(pt, activeSegPts, _configuration.ReportGeneratorOptions.SliceptKickOutRadiusMeters);

                if (null != slice)
                    _slicesCalc.Add(slice);
            }

            // order by slice connection point [17/12/2009 LysakA]
            _slicesCalc.Sort((slc1, slc2) => slc1.NPoint - slc2.NPoint);

            // delete blind slices [17/12/2009 LysakA]
            for (var c = 0; c < _slicesCalc.Count - 1; ++c)
                if (_slicesCalc[c].NPoint == _slicesCalc[c + 1].NPoint)
                {
                    _slicesCalc.Remove(_slicesCalc[c]);
                    c = 0;
                }

            if (!_slicesCalc.Any())
            {
                _loggingService.LogError("Разбивка пуста! Невозможно распилить трек на дни.");
                return false;
            }

            _loggingService.Log("Уточнение разбивки");
            // shift start & end to track start & end [18/12/2009 LysakA]
            if (Geo.DistanceExactMeters(_slicesCalc.First().SlicePoint, activeSegPts.First()) <
                StartEndCapDistance)
                _slicesCalc.First().NPoint = 0;
            if (Geo.DistanceExactMeters(_slicesCalc.Last().SlicePoint, activeSegPts.Last()) <
                StartEndCapDistance)
                _slicesCalc.Last().NPoint = activeSegPts.Count - 1;

            if (_slicesCalc.First().NPoint != 0) //Cap start
                _slicesCalc.Insert(0, new TrackSeg.Slice {NPoint = 0}); //Start -- 0

            if (_slicesCalc.Last().NPoint != activeSegPts.Count - 1) //Cap end
                _slicesCalc.Add(new TrackSeg.Slice
                {
                    NPoint = activeSegPts.Count - 1
                }); //Last -- Finish


            _loggingService.Log("Расчет перепадов");

            for (var npoint = 0; npoint != _slicesCalc.Count - 1; ++npoint)
            {
                var reportItem = _reportItemGenerator(Source);

                reportItem.UseSlice(npoint, activeSegPts,_slicesCalc);

                Results.Add(reportItem);
            }

            GenBackpackWeightColumn();

            GenGraphData(activeSegPts);

            // check for bad heights; [22/12/2009 LysakA]
            foreach (var s in _srtmRepository.LoadedSrtms)
                if (s.BadValuesDetected)
                    _loggingService.LogError("В файле высот <" + s.Loadname +
                                     "> обнаружены неверные значения.\nЗапустите из командной строки\n\nsrtm_interp " +
                                     s.Loadname +
                                     "\n\nиначе все расчеты, зависящие от высоты, будут совершенно бредовыми!");

            return true;
        }

        
        private void GenGraphData(List<Point> activeSegPts)
        {
            double disM = 0, prevHgt = 0;

            GraphData = new TagGraphData
            {
                Profile = new List<KeyValuePair<double, double>>(),
                DaySections = new List<double>(),
                Extremities = new List<KeyValuePair<double, double>>()
            };

            GraphData.Profile.Clear();

            const double peakTolerance = 100;

            for (var c = 0; c != _slicesCalc.Count - 1; ++c)
            {
                var first = true;

                var peakH = double.MinValue;
                double peakKm = 0;
                var peakNpoint = 0;

                for (var i = _slicesCalc[c].NPoint; i < _slicesCalc[c + 1].NPoint; ++i)
                {
                    if (first)
                        GraphData.DaySections.Add(disM);
                    
                    disM += Geo.DistanceExactMeters(activeSegPts[i], activeSegPts[i + 1]);
                    var hgt = _srtmRepository.GetHeightForPoint(activeSegPts[i + 1]);

                    GraphData.Profile.Add(new KeyValuePair<double, double>(disM, hgt ?? prevHgt));

                    if (hgt.HasValue)
                    {
                        if (hgt.Value > peakH)
                        {
                            peakNpoint = i;
                            peakH = hgt ?? 0;
                            peakKm = disM;
                        }

                        prevHgt = hgt ?? 0;
                    }

                    first = false;
                }

                if (peakNpoint != _slicesCalc[c].NPoint &&
                    peakNpoint != _slicesCalc[c + 1].NPoint - 1 &&
                    Math.Abs(
                        (_srtmRepository.GetHeightForPoint(activeSegPts[peakNpoint + 1]) ?? 0) -
                        (_srtmRepository.GetHeightForPoint(activeSegPts[_slicesCalc[c].NPoint + 1]) ?? 0)
                    ) > peakTolerance &&
                    Math.Abs(
                        (_srtmRepository.GetHeightForPoint(activeSegPts[peakNpoint + 1]) ?? 0) -
                        (_srtmRepository.GetHeightForPoint(activeSegPts[_slicesCalc[c + 1].NPoint]) ?? 0)
                    ) > peakTolerance
                )
                    GraphData.Extremities.Add(new KeyValuePair<double, double>(peakKm, peakH));
            }
        }

                
        private void GenBackpackWeightColumn()
        {
            if (Source.Settings.BackpackWeightSettings == null) 
                return;

            var presets = Source.Settings.BackpackWeightSettings;

            var totalDays = _slicesCalc.Count - 1;

            var maleWeight = presets.GetStartBaseWeight(totalDays + presets.SpareDays) + presets.GroupWeightMaleKg;
            var femaleWeight = presets.GetStartBaseWeight(totalDays + presets.SpareDays) +
                               presets.GroupWeightFemaleKg;

            var dayDelta = presets.FoodPerDayKg + presets.FuelPerDayKg;
            double zabrDeltaWeight = 0;

            //Zabroska set

            if (Source.Settings.ZabroskaStartPointNum.HasValue && Source.Settings.ZabroskaEndPointNum.HasValue)
            {
                var afterZabroskaDays = totalDays - (Source.Settings.ZabroskaEndPointNum.Value + 1) +
                                        presets.SpareDays;

                zabrDeltaWeight = afterZabroskaDays * dayDelta;
            }

           //Zabroska left before route start
            if (-1 == Source.Settings.ZabroskaStartPointNum)
            {
                maleWeight -= zabrDeltaWeight;
                femaleWeight -= zabrDeltaWeight;
            }

            for (int c = 0; c < Results.Count; c++)
            {                
                Results[c].MaleWeight = maleWeight;
                Results[c].FemaleWeight = femaleWeight;

                if (c == Source.Settings.ZabroskaStartPointNum)
                {
                    maleWeight -= zabrDeltaWeight;
                    femaleWeight -= zabrDeltaWeight;
                }

                if (c == Source.Settings.ZabroskaEndPointNum)
                {
                    maleWeight += zabrDeltaWeight;
                    femaleWeight += zabrDeltaWeight;
                }

                maleWeight -= dayDelta;
                femaleWeight -= dayDelta;
            }

            
        }

        

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}