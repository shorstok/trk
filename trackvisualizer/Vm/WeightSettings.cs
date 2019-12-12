using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using trackvisualizer.Annotations;

namespace trackvisualizer.Vm
{
    [DataContract]
    public class WeightSettings : INotifyPropertyChanged
    {
        private double _personalWeightKg = 14.5;
        private double _foodPerDayKg = 0.550;
        private double _fuelPerDayKg = 0.085;
        private double _groupWeightMaleKg = 7;
        private double _groupWeightFemaleKg =2;
        private double _spareDays = 2;

        [JsonProperty("PersonalWeightKg")]
        public double PersonalWeightKg
        {
            get => _personalWeightKg;
            set
            {
                if (value.Equals(_personalWeightKg)) return;
                _personalWeightKg = value;
                OnPropertyChanged();
            }
        }

        [JsonProperty("FoodPerDayKg")]
        public double FoodPerDayKg
        {
            get => _foodPerDayKg;
            set
            {
                if (value.Equals(_foodPerDayKg)) return;
                _foodPerDayKg = value;
                OnPropertyChanged();
            }
        }

        [JsonProperty("FuelPerDayKg")]
        public double FuelPerDayKg
        {
            get => _fuelPerDayKg;
            set
            {
                if (value.Equals(_fuelPerDayKg)) return;
                _fuelPerDayKg = value;
                OnPropertyChanged();
            }
        }

        [JsonProperty("GroupWeightMaleKg")]
        public double GroupWeightMaleKg
        {
            get => _groupWeightMaleKg;
            set
            {
                if (value.Equals(_groupWeightMaleKg)) return;
                _groupWeightMaleKg = value;
                OnPropertyChanged();
            }
        }

        [JsonProperty("GroupWeightFemaleKg")]
        public double GroupWeightFemaleKg
        {
            get => _groupWeightFemaleKg;
            set
            {
                if (value.Equals(_groupWeightFemaleKg)) return;
                _groupWeightFemaleKg = value;
                OnPropertyChanged();
            }
        }

        [JsonProperty("SpareDays")]
        public double SpareDays
        {
            get => _spareDays;
            set
            {
                if (value.Equals(_spareDays)) return;
                _spareDays = value;
                OnPropertyChanged();
            }
        }

        public double GetStartBaseWeight(double days)
        {
            return days * (FoodPerDayKg + FuelPerDayKg) + PersonalWeightKg;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}