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
using LiveCharts.Wpf.Charts.Base;
using trackvisualizer.Annotations;
using trackvisualizer.Config;
using trackvisualizer.Geodetic;
using trackvisualizer.Properties;
using trackvisualizer.Service;
using trackvisualizer.Service.ReportExporters;
using Point = trackvisualizer.Geodetic.Point;

namespace trackvisualizer.Vm
{
    public class TagGraphData : INotifyPropertyChanged
    {
        public List<KeyValuePair<double, double>> Profile { get; set; } = new List<KeyValuePair<double, double>>();
        public List<KeyValuePair<double, double>> SectionLabels{ get; set; } = new List<KeyValuePair<double, double>>();

        /// <summary>
        /// Should be called explicitly to minimize overdraw
        /// </summary>
        public void NotifyOnDataUpdateFinished()
        {
            OnPropertyChanged(nameof(Profile));
        }
        
        public void Clear()
        {
            Profile.Clear();
            SectionLabels.Clear();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        
    }

    public class TrackReportVm : INotifyPropertyChanged
    {
        private readonly IUiLoggingService _loggingService;
        private readonly IUiService _uiService;
        private readonly SrtmRepository _srtmRepository;
        private readonly ITrackReportExporter[] _reportExporters;
        private readonly Func<TrackVm,TrackReportItemVm> _reportItemGenerator;
        private readonly TrekplannerConfiguration _configuration;
        public TrackVm Source { get; }

        public double
            StartEndCapDistance =
                500; //m, move start point to track start in case of placement error[18/12/2009 LysakA]

        private List<TrackSeg.Slice> _slicesCalc;

        public ObservableCollection<TrackReportItemVm> Results { get; } = new ObservableCollection<TrackReportItemVm>();

        public TrackReportTotalsVm Totals { get; }

        public TagGraphData GraphData { get; } = new TagGraphData();

        public TrackChartVm Chart { get; }

        public ICommand ExportCommand { get; }

        public TrackReportVm(TrackVm source, 
            IUiLoggingService loggingService,
            IUiService uiService,
            SrtmRepository srtmRepository,
            ITrackReportExporter[] reportExporters,
            Func<TrackVm,TrackReportItemVm> reportItemGenerator,
            TrekplannerConfiguration configuration)
        {
            _loggingService = loggingService;
            _uiService = uiService;
            _srtmRepository = srtmRepository;
            _reportExporters = reportExporters;
            _reportItemGenerator = reportItemGenerator;
            _configuration = configuration;
            
            Source = source;
            Totals = new TrackReportTotalsVm(this);
            Chart = new TrackChartVm(this);

            ExportCommand = new DelegateCommand(o=>Results.Any(), ExportReportAsync);
        }

        private async void ExportReportAsync(object obj)
        {
            var exporterId =
                await _uiService.ChooseAsync(_reportExporters.Select(e => Tuple.Create(e.Id, e.Description)), Resources.TrackReportVm_ExportReportAsync_Select_report_format);

            if(null == exporterId)
                return;

            var exporter = _reportExporters.FirstOrDefault(re => re.Id == exporterId);

            if (null == exporter)
            {
                await _uiService.NofityError(string.Format(Resources.TrackReportVm_ExportReportAsync_Err_Report_exporter_with_ID__0__unknown, exporterId));
                return;
            }

            await exporter.Export(this);
        }


        [Localizable(false)]
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
            Totals.Recalculate();

            if (Source.AreHeightmapsMissing)
            {
                Source.RegisterError(Resources.TrackReportVm_CreateReportAsync_ErrNoHeightmap);
                return false;
            }

            if (activeSegPts == null)
            {
                Source.RegisterError(Resources.TrackReportVm_CreateReportAsync_ErrNoTrack);
                return false;
            }
            
            if (activeSegPts.Count < 2)
            {
                Source.RegisterError(Resources.TrackReportVm_CreateReportAsync_ErrNoPointsInTrack);
                return false;
            }

            var points = FilterSlicepts();

            if (points.Count < 2)
            {
                Source.RegisterError(Resources.TrackReportVm_CreateReportAsync_ErrNotEnoughPoints);
                return false;
            }

            // everything is ok, can calculate [17/12/2009 LysakA]
            _slicesCalc = new List<TrackSeg.Slice>();

            _loggingService.Log(Resources.TrackReportVm_CreateReportAsync_SplittingTrack);
           
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
            {
                if (_slicesCalc[c].NPoint == _slicesCalc[c + 1].NPoint)
                {
                    _slicesCalc.Remove(_slicesCalc[c]);
                    c = 0;
                }
            }

            if (!_slicesCalc.Any())
            {
                _loggingService.LogError(Resources.TrackReportVm_CreateReportAsync_ErrSplitEmptyCantContinue);
                return false;
            }

            _loggingService.Log(Resources.TrackReportVm_CreateReportAsync_RefiningSplit);
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


            _loggingService.Log(Resources.TrackReportVm_CreateReportAsync_DeltaCalculation);

            for (var npoint = 0; npoint != _slicesCalc.Count - 1; ++npoint)
            {
                var reportItem = _reportItemGenerator(Source);

                reportItem.CalculateGeoProperties(npoint, activeSegPts,_slicesCalc);

                Results.Add(reportItem);
            }

            GenBackpackWeightColumn();

            GenGraphData(activeSegPts);

            // check for bad heights; [22/12/2009 LysakA]
            foreach (var s in _srtmRepository.LoadedSrtms)
                if (s.HasVoids)
                    _loggingService.LogError(
                        string.Format(Resources.TrackReportVm_CreateReportAsync_ErrHeightmapInvalid, s.OriginalFilename, s.OriginalFilename));

            Totals.Recalculate();

            return true;
        }

        
        private void GenGraphData(List<Point> activeSegPts)
        {
            double disM = 0, prevHgt = 0;
            double prevDis = 0;

            GraphData.Clear();

            const double peakTolerance = 100;
            const double distanceTolerance = 400;

            for (var c = 0; c != _slicesCalc.Count - 1; ++c)
            {
                for (var i = _slicesCalc[c].NPoint; i < _slicesCalc[c + 1].NPoint; ++i)
                {
                    disM += Geo.DistanceExactMeters(activeSegPts[i], activeSegPts[i + 1]);
                    var hgt = _srtmRepository.GetHeightForPoint(activeSegPts[i + 1]);

                    if(!hgt.HasValue)
                        continue;

                    if(i== _slicesCalc[c + 1].NPoint-1 || (c ==0 && i ==  _slicesCalc[c].NPoint))
                        GraphData.SectionLabels.Add(new KeyValuePair<double, double>(disM, hgt.Value));

                    if (i ==  _slicesCalc[c].NPoint || Math.Abs(prevHgt - hgt.Value) > peakTolerance || Math.Abs(prevDis - disM) > distanceTolerance)
                    {
                        GraphData.Profile.Add(new KeyValuePair<double, double>(disM, hgt.Value));
                        prevDis = disM;
                        prevHgt = hgt.Value;
                    }
                }
            }

            GraphData.NotifyOnDataUpdateFinished();
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