using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace trackvisualizer.View
{
    /// <summary>
    ///     Логика взаимодействия для ScatterGraph.xaml
    /// </summary>
    public partial class ScatterGraph : UserControl
    {
        /// <summary>
        /// </summary>
        private List<GraphEntity> _points = new List<GraphEntity>();

        private Conv _currConv;

        private readonly int _heightScaleDivisionNumber = 20;

        private List<VertGraphSeparator> _separators = new List<VertGraphSeparator>();

        public ScatterGraph()
        {
            InitializeComponent();
        }

        public List<GraphEntity> Points
        {
            set
            {
                _points = value;
                Redraw();
            }
            get => _points;
        }

        public List<VertGraphSeparator> Separators
        {
            get => _separators;
            set
            {
                _separators = value;
                Redraw();
            }
        }

        private void Redraw()
        {
            if (null == _points || !_points.Any())
                return;

            double minX = double.MaxValue, maxX = double.MinValue;

            _currConv.MinH = double.MaxValue;
            _currConv.MaxH = double.MinValue;

            _points.ForEach(it =>
            {
                if (minX > it.X)
                    minX = it.X;
                if (maxX < it.X)
                    maxX = it.X;

                if (_currConv.MinH > it.Y)
                    _currConv.MinH = it.Y;
                if (_currConv.MaxH < it.Y)
                    _currConv.MaxH = it.Y;
            });

            _points.Sort((co, co2) =>
            {
                if (co.X == co2.X)
                    return 0;

                return (int) (10 * (co.X - co2.X) / Math.Abs(co.X - co2.X));
            });

            DataContainer.Points.Clear();
            SeparatorsContainer.Children.Clear();

            _currConv.WindowW = Data.ActualWidth;
            _currConv.WindowH = Data.ActualHeight;

            _currConv.SubX = minX;
            _currConv.ScaleX = _currConv.WindowW / (maxX - minX);

            _currConv.SubY = _currConv.MaxH;
            _currConv.ScaleY = -_currConv.WindowH / (_currConv.MaxH - _currConv.MinH);

            for (var i = 0; i < _points.Count; ++i)
            {
                var x = _points[i].X;
                var y = _points[i].Y;

                _currConv.fwd_conv(ref x, ref y);

                DataContainer.Points.Add(new Point(x, y));
            }

            SeparatorsContainer.Height = Data.ActualHeight;

            foreach (var separator in _separators)
            {
                var x = separator.X;
                double d = 0;

                _currConv.fwd_conv(ref x, ref d);

                var v = new Rectangle
                {
                    Width = 1,
                    MinHeight = _currConv.WindowH,
                    Fill = separator.Color,
                    VerticalAlignment = VerticalAlignment.Stretch
                };


                if (separator.Title != null)
                {
                    var tb = new TextBlock
                    {
                        LayoutTransform = new RotateTransform(-90),
                        Text = separator.Title,
                        Foreground = separator.Color,
                        Padding = new Thickness(3, 0, 3, 3),
                        Background = new SolidColorBrush(Color.FromArgb(10, 0, 0, 0))
                    };

                    Canvas.SetLeft(tb, x);
                    Canvas.SetTop(tb, _currConv.WindowH - 20);

                    SeparatorsContainer.Children.Add(tb);
                }

                Canvas.SetLeft(v, x);
                Canvas.SetTop(v, 0);


                SeparatorsContainer.Children.Add(v);
            }

            BuildLegend();
        }

        private void BuildLegend()
        {
            BuildHeightLegend();
            BuildLengthLegend();
        }

        private void BuildLengthLegend()
        {
            double kmCounter = 0;
            double x = 0, dummyY = 0;
            const double matchWidth = 100;

            LengthScale.Children.Clear();

            while (x < _currConv.WindowW)
            {
                kmCounter += 10000;
                x = kmCounter;

                _currConv.fwd_conv(ref x, ref dummyY);

                if (x > _currConv.WindowW)
                    continue;

                var tb = new TextBlock
                {
                    Margin = new Thickness(x - matchWidth / 2, 3, 3, 0),
                    Text = (kmCounter / 1e3).ToString("0."),
                    Width = matchWidth,
                    TextAlignment = TextAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Foreground = Brushes.Black
                };

                LengthScale.Children.Add(tb);
            }
        }

        private void BuildHeightLegend()
        {
            const double kernH = 11;
            double heightStep = 100;
            const double majorHeightMark = 500;

            var nStepsPerKm = 0;

            var aperture = Math.Abs(_currConv.MaxH - _currConv.MinH) / _heightScaleDivisionNumber;


            heightStep = 100 * Math.Round(aperture / 100);

            nStepsPerKm = (int) (1000 / heightStep);

            HeightScale.Children.Clear();

            var startH = _currConv.MaxH;

            startH = Math.Truncate(startH / heightStep) * heightStep;

            for (var i = 0; i < _heightScaleDivisionNumber; i++)
            {
                double x = 0;

                var y = startH;

                _currConv.fwd_conv(ref x, ref y);

                if (y > _currConv.WindowH)
                    continue;

                bool isMajorHeight = Math.Abs(majorHeightMark * Math.Round(startH / majorHeightMark) - startH) < 1;

                var tb = new TextBlock
                {
                    Margin = new Thickness(0, y - kernH / 2, 3, 0),
                    Text = startH.ToString("0."),
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Foreground = isMajorHeight ? Brushes.Black : Brushes.DarkGray,
                    FontSize = kernH
                };

                var v = new Rectangle
                {
                    Width = _currConv.WindowW,
                    Height = 0.5,
                    Fill = Brushes.LightGray
                };

                Canvas.SetLeft(v, 0);
                Canvas.SetTop(v, y);

                HeightScale.Children.Add(tb);
                SeparatorsContainer.Children.Add(v);

                startH -= heightStep;
            }
        }

        private void Data_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Redraw();
        }

        public struct GraphEntity
        {
            public double X;
            public double Y;
        }

        public struct VertGraphSeparator
        {
            public Brush Color;
            public double X;
            public string Title;
        }

        /// <summary>
        /// </summary>
        private struct Conv
        {
            internal double WindowH;
            internal double WindowW;

            internal double SubX;
            internal double SubY;

            internal double ScaleX;
            internal double ScaleY;

            internal double MaxH;
            internal double MinH;

            public void fwd_conv(ref double x, ref double y)
            {
                x -= SubX;
                x *= ScaleX;

                y -= SubY;
                y *= ScaleY;
            }

            public void rev_conv(ref double x, ref double y)
            {
                if (Math.Abs(ScaleX) < double.Epsilon || Math.Abs(ScaleY) < double.Epsilon)
                    return;

                x /= ScaleX;
                x += SubX;

                y /= ScaleY;
                y += SubY;
            }
        }
    }
}