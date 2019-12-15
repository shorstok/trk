using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using LiveCharts;
using LiveCharts.Defaults;
using LiveCharts.Wpf;
using trackvisualizer.Annotations;

namespace trackvisualizer.Vm
{
    public class TrackChartVm : INotifyPropertyChanged
    {
        private readonly TrackReportVm _source;

        public SeriesCollection Series { get; }

        public ChartValues<ObservablePoint> HeightPoints { get; } = new ChartValues<ObservablePoint>();
        public ChartValues<ObservablePoint> SectionPoints { get; } = new ChartValues<ObservablePoint>();

        private SolidColorBrush GraphBrush = new SolidColorBrush(Color.FromRgb(87, 161, 223));
        private GradientBrush GraphBrushFill = new LinearGradientBrush
        {
            StartPoint = new Point(0,0),
            EndPoint = new Point(0,1),
            GradientStops =
            {
                new GradientStop(Color.FromArgb(148, 87, 161, 223),0),
                new GradientStop(Color.FromArgb(61, 87, 161, 223),1)
            }
        };

        private SolidColorBrush GraphBrushAlternate = new SolidColorBrush(Color.FromArgb(255, 255, 235, 0));

        public Func<double, string> YFormatter { get; } = value => $"{value:0.} м";
        public Func<double, string> XFormatter { get; } = value => $"{value:0.} Км";

        public TrackChartVm(TrackReportVm source)
        {
            _source = source;

            if (source?.GraphData != null)
                PropertyChangedEventManager.AddHandler(source.GraphData, OnSourceUpdate, String.Empty);

            BuildCharts();


            Series = new SeriesCollection
            {
                new LineSeries
                {
                    Title = "Высота",
                    Values = HeightPoints,
                    PointGeometry = null,                                    
                    Stroke = GraphBrush,
                    Fill = GraphBrushFill
                },
                new ScatterSeries
                {
                    Title = "Участок",
                    Values = SectionPoints,
                    LabelPoint = GetSectionLabel,
                    Stroke = GraphBrush,                                        
                    StrokeThickness =  2,
                    MinPointShapeDiameter = 15,
                    MaxPointShapeDiameter = 17,
                    Fill = GraphBrushAlternate
                },
            };
        }

        private readonly Dictionary<int, string> _sectionNamesLookup = new Dictionary<int, string>();

        private string GetSectionLabel(ChartPoint chartPoint)
        {
            //Found no easier way to lookup section on mouse hover :|

            return _sectionNamesLookup.TryGetValue(chartPoint.Key,
                out var label)
                ? label
                : $"Finish {chartPoint.X:0.} Км";
        }

        private void OnSourceUpdate(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
        {
            BuildCharts();
        }

        private void BuildCharts()
        {
            if (!(_source.GraphData?.Profile?.Count > 0))
            {
                HeightPoints.Clear();
                SectionPoints.Clear();
                return;
            }

            CreateOrUpdateChartSeries(HeightPoints, _source.GraphData.Profile);

            _sectionNamesLookup.Clear();

            for (int i = 0; i < _source.Results.Count; i++)
                _sectionNamesLookup[i] = _source.Results[i].SectionStartName;

            CreateOrUpdateChartSeries(SectionPoints, _source.GraphData.SectionLabels);



        }

        private void CreateOrUpdateChartSeries(ChartValues<ObservablePoint> target,
            List<KeyValuePair<double, double>> source)
        {
            
            var max = Math.Max(target.Count, source.Count);

            for (int i = 0; i < max; i++)
            {
                if (source.Count <= i)
                    target.RemoveAt(i);
                else if (target.Count <= i)
                    target.Add(new ObservablePoint(source[i].Key / 1e3,source[i].Value));
                else
                {
                    target[i].X = source[i].Key / 1e3;
                    target[i].Y = source[i].Value;
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) => 
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}