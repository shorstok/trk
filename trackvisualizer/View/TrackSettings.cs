using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Xml.Linq;
using Newtonsoft.Json;
using trackvisualizer.Annotations;
using trackvisualizer.Config;
using trackvisualizer.Vm;

namespace trackvisualizer.View
{  
    [DataContract]
    public class TrackSettings : INotifyPropertyChanged
    {
        [JsonProperty("renamed_sections")]
        public Dictionary<int,string> CustomSectionName { get; set; } = new Dictionary<int,string>();

        [JsonProperty("backpack_weight_settings")]
        public WeightSettings BackpackWeightSettings { get; set; } = new WeightSettings();

        [JsonProperty("zabroska_start_pt")]
        public int? ZabroskaStartPointNum
        {
            get => _zabroskaStartPointNum;
            set
            {
                if (value == _zabroskaStartPointNum) return;
                _zabroskaStartPointNum = value;
                Save();
                OnPropertyChanged();
            }
        }

        [JsonProperty("zabroska_end_pt")]
        public int? ZabroskaEndPointNum
        {
            get => _zabroskaEndPointNum;
            set
            {
                if (value == _zabroskaEndPointNum) return;
                _zabroskaEndPointNum = value;
                Save();
                OnPropertyChanged();
            }
        }

        [JsonProperty("separate_slicepoint_filename")]
        public string SourceSeparateSlicepointSource
        {
            get => _sourceSeparateSlicepointSource;
            set
            {
                if (value == _sourceSeparateSlicepointSource) return;
                _sourceSeparateSlicepointSource = value;
                OnPropertyChanged();
            }
        }
        
        [JsonProperty("excluded_points")]
        public ObservableCollection<string> ExcludedPoints { get; set; } = new ObservableCollection<string>();

        private string _originalFilename = null;
        private int? _zabroskaStartPointNum;
        private int? _zabroskaEndPointNum;
        private string _sourceSeparateSlicepointSource;

        public void OverrideSectionName(int sectionNumber, string value)
        {
            if (CustomSectionName.TryGetValue(sectionNumber, out var existing))
            {
                if(string.Equals(existing,value))
                    return;
            }

            if (string.IsNullOrWhiteSpace(value))
                CustomSectionName.Remove(sectionNumber);
            else
                CustomSectionName[sectionNumber] = value;

            Save();

            OnPropertyChanged(nameof(CustomSectionName));
        }
        

        public void Save()
        {
            if(!string.IsNullOrWhiteSpace(_originalFilename))
                File.WriteAllText(_originalFilename, JsonConvert.SerializeObject(this, JsonFormatters.IndentedAutotype));
        }

        public static TrackSettings TryLoadAlongsideTrack(string fileToLoad)
        {
            fileToLoad += ".jsettings";

            if (!File.Exists(fileToLoad))
            {
                var result = new TrackSettings
                {
                    _originalFilename = fileToLoad
                };

                result.Save();

                return result;
            }

            var existing = JsonConvert.DeserializeObject<TrackSettings>(
                File.ReadAllText(fileToLoad),
                JsonFormatters.IndentedAutotype);

            existing._originalFilename = fileToLoad;            

            return existing;

        }


        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}