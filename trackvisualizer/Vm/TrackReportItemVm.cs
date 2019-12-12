using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using trackvisualizer.Annotations;
using trackvisualizer.Config;
using trackvisualizer.Geodetic;
using trackvisualizer.Service;
using Point = trackvisualizer.Geodetic.Point;

namespace trackvisualizer.Vm
{
    public class TrackReportItemVm : INotifyPropertyChanged
    {
        public TrackVm Source { get; }
        private readonly TrekplannerConfiguration _configuration;
        private readonly SrtmRepository _srtmRepository;
        private string _name;
        private string _comment;
        private double _distanceMeters;
        private double _fietsIndex;
        private double _lebedevHours;
        private double _maxHeight;
        private double _sleepHeight;
        private ObservableCollection<double> _extremeHeights;
        private double _deltaHeightAbsPerDay;
        private double _descentPerDay;
        private double _ascentPerDay;
        private double _maleWeight;
        private double _femaleWeight;

        public string Name
        {
            get => _name;
            set
            {
                if (value == _name) return;
                _name = value;
                OnPropertyChanged();
            }
        }

        public string Comment
        {
            get => _comment;
            set
            {
                if (value == _comment) return;
                _comment = value;
                OnPropertyChanged();
            }
        }

        public double DistanceMeters
        {
            get => _distanceMeters;
            set
            {
                if (value.Equals(_distanceMeters)) return;
                _distanceMeters = value;
                OnPropertyChanged();
            }
        }


        public double FietsIndex
        {
            get => _fietsIndex;
            set
            {
                if (value.Equals(_fietsIndex)) return;
                _fietsIndex = value;
                OnPropertyChanged();
            }
        }

        public double LebedevHours
        {
            get => _lebedevHours;
            set
            {
                if (value.Equals(_lebedevHours)) return;
                _lebedevHours = value;
                OnPropertyChanged();
            }
        }

        public double MaxHeight
        {
            get => _maxHeight;
            set
            {
                if (value.Equals(_maxHeight)) return;
                _maxHeight = value;
                OnPropertyChanged();
            }
        }

        public double SleepHeight
        {
            get => _sleepHeight;
            set
            {
                if (value.Equals(_sleepHeight)) return;
                _sleepHeight = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<double> ExtremeHeights
        {
            get => _extremeHeights;
            set
            {
                if (Equals(value, _extremeHeights)) return;
                _extremeHeights = value;
                OnPropertyChanged();
            }
        }

        public double DeltaHeightAbsPerDay
        {
            get => _deltaHeightAbsPerDay;
            set
            {
                if (value.Equals(_deltaHeightAbsPerDay)) return;
                _deltaHeightAbsPerDay = value;
                OnPropertyChanged();
            }
        }

        public double DescentPerDay
        {
            get => _descentPerDay;
            set
            {
                if (value.Equals(_descentPerDay)) return;
                _descentPerDay = value;
                OnPropertyChanged();
            }
        }

        public double AscentPerDay
        {
            get => _ascentPerDay;
            set
            {
                if (value.Equals(_ascentPerDay)) return;
                _ascentPerDay = value;
                OnPropertyChanged();
            }
        }

        public double MaleWeight
        {
            get => _maleWeight;
            set
            {
                if (value.Equals(_maleWeight)) return;
                _maleWeight = value;
                OnPropertyChanged();
            }
        }

        public double FemaleWeight
        {
            get => _femaleWeight;
            set
            {
                if (value.Equals(_femaleWeight)) return;
                _femaleWeight = value;
                OnPropertyChanged();
            }
        }


        public TrackReportItemVm(TrekplannerConfiguration configuration, TrackVm source, SrtmRepository srtmRepository)
        {
            Source = source;
            _configuration = configuration;
            _srtmRepository = srtmRepository;
        }

        public void UseSlice(int npoint, List<Point> activeSegPts, List<TrackSeg.Slice> slicesCalc)
        {
            CalculateName(npoint, slicesCalc);
            CalculateLength(npoint, slicesCalc, activeSegPts);
            GenSegmentAscents(npoint, slicesCalc, activeSegPts);
        }

        private void CalculateLength(int npoint, List<TrackSeg.Slice> slicesCalc, List<Point> activeSegPts)
        {
            double distance = 0;

            for (var i = slicesCalc[npoint].NPoint; i < slicesCalc[npoint + 1].NPoint; ++i)
                distance += Geo.DistanceExactMeters(activeSegPts[i], activeSegPts[i + 1]);

            DistanceMeters = distance;
        }

        private void CalculateName(int npoint, List<TrackSeg.Slice> slicesCalc)
        {
            var slicept = slicesCalc[npoint].SlicePoint;
            var nextSlicept = slicesCalc[npoint + 1].SlicePoint;

            var nameConv = nextSlicept.Name;

            if (_configuration.ReportGeneratorOptions.Comments)
                nameConv += nextSlicept.Comment != null ? " (" + nextSlicept.Comment + ")" : "";

            if (slicept.Name == null)
                nameConv = "S.." + nameConv;
            else if (nextSlicept.Name == null)
                nameConv = slicept.Name + "..F";
            else
                nameConv = slicept.Name + " .. " + nameConv;

            if (Source.Settings.CustomSectionNames.TryGetValue(npoint, out var customName))
                nameConv = customName;

            if (npoint == Source.Settings.ZabroskaStartPointNum)
                Comment = "оставляем заброску";

            if (npoint == 0 && Source.Settings.ZabroskaStartPointNum == -1)
                Comment = "завозим заброску до начала маршрута";

            if (npoint == Source.Settings.ZabroskaEndPointNum)
                Comment = "забираем заброску";

            Name = nameConv;
        }


        private void GenSegmentAscents(int npoint, List<TrackSeg.Slice> slicesCalc, List<Point> activeSegPts)
        {
            const double deltaNonsignificant = 70; //m

            //FIETS-index = [dH^2 / D*10] + (T - 1000):1000   
            
            int npStart = slicesCalc[npoint].NPoint, npEnd = slicesCalc[npoint + 1].NPoint;

            if (npStart > npEnd)
            {
                var t = npEnd;
                npEnd = npStart;
                npStart = t;
            }

            Geo.GetHeightDetailsAccurate(
                activeSegPts.Skip(npStart).Take(npEnd + 1 - npStart).ToList(),
                out var asc,
                out var desc,
                out var hstart,
                out var hend,
                out var hmax,
                out var hmin,
                out var ihmax,
                out var ihmin,
                _srtmRepository);

            //////////////////////////////////////////////////////////////////////////
            var extremeHgts = new List<double> { hstart };

            if (ihmax < ihmin)
            {
                extremeHgts.Add(hmax);
                extremeHgts.Add(hmin);
            }
            else
            {
                extremeHgts.Add(hmin);
                extremeHgts.Add(hmax);
            }

            extremeHgts.Add(hend);

            for (var i = 1; i < extremeHgts.Count; ++i)
            {
                var iprev = i - 1;
                var inext = i + 1;

                if (inext < extremeHgts.Count)
                {
                    var dprev = extremeHgts[iprev] - extremeHgts[i];
                    var dnext = extremeHgts[i] - extremeHgts[inext];

                    if (dprev * dnext >= 0 || Math.Abs(dnext) < deltaNonsignificant ||
                        Math.Abs(dprev) < deltaNonsignificant) //same sign
                    {
                        extremeHgts.RemoveAt(i);
                        i = 1;
                    }
                }
            }

            //////////////////////////////////////////////////////////////////////////

            if (!_configuration.ReportGeneratorOptions.AscDescCalcAccurate)
            {
                asc = desc = 0;

                for (var i = 1; i < extremeHgts.Count; ++i)
                {
                    var d = extremeHgts[i] - extremeHgts[i - 1];

                    if (d > 0)
                        asc += d;
                    else
                        desc += -d;
                }

                if (asc < deltaNonsignificant)
                    asc = 0;
                if (desc < deltaNonsignificant)
                    desc = 0;
            }

            AscentPerDay = asc;
            DescentPerDay = desc;

            DeltaHeightAbsPerDay = asc + desc;

            ExtremeHeights = new ObservableCollection<double>(extremeHgts);

            SleepHeight = hend;
            MaxHeight = hmax;

            double disM = 0;

            for (var i = slicesCalc[npoint].NPoint; i < slicesCalc[npoint + 1].NPoint; ++i)
                disM += Geo.DistanceExactMeters(activeSegPts[i], activeSegPts[i + 1]);

            var lebval = (disM + asc * 10) / (_configuration.ReportGeneratorOptions.LebedevSpeedKmH * 1000.0);

            LebedevHours = lebval;

            //FIETS-index = [dH^2 / D*10] + (T - 1000):1000   

            var fietsIdx = asc * asc / (disM * 10);
            if (hmax > 1e3)
                fietsIdx += hmax / 1e3;

            FietsIndex = fietsIdx;
        }


        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}