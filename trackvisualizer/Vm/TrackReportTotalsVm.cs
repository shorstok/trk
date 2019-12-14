using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using trackvisualizer.Annotations;

namespace trackvisualizer.Vm
{
    public class TrackReportTotalsVm : INotifyPropertyChanged
    {
        public double? DistanceTotalKilometers
        {
            get => _distanceTotalKilometers;
            set
            {
                if (value.Equals(_distanceTotalKilometers)) return;
                _distanceTotalKilometers = value;
                OnPropertyChanged();
            }
        }

        public double? AscentTotalMeters
        {
            get => _ascentTotalMeters;
            set
            {
                if (value.Equals(_ascentTotalMeters)) return;
                _ascentTotalMeters = value;
                OnPropertyChanged();
            }
        }

        public double? DescentTotal
        {
            get => _descentTotal;
            set
            {
                if (value.Equals(_descentTotal)) return;
                _descentTotal = value;
                OnPropertyChanged();
            }
        }

        public double? HoursTotal
        {
            get => _hoursTotal;
            set
            {
                if (value.Equals(_hoursTotal)) return;
                _hoursTotal = value;
                OnPropertyChanged();
            }
        }
        
        private double? _distanceTotalKilometers;
        private double? _ascentTotalMeters;
        private double? _descentTotal;
        private double? _hoursTotal;

        private readonly TrackReportVm _source;

        public TrackReportTotalsVm(TrackReportVm source)
        {
            _source = source;
        }

        public void Recalculate()
        {
            DistanceTotalKilometers = _source.Results.DefaultIfEmpty().Sum(r => r?.DistanceMeters) / 1e3;
            AscentTotalMeters = _source.Results.DefaultIfEmpty().Sum(r => r?.AscentPerDay);
            DescentTotal = _source.Results.DefaultIfEmpty().Sum(r => r?.DescentPerDay);
            HoursTotal = _source.Results.DefaultIfEmpty().Sum(r => r?.LebedevHours);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) => 
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));        
    }
}