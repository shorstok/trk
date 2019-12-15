using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Win32;
using trackvisualizer.Annotations;
using trackvisualizer.Config;
using trackvisualizer.Properties;
using trackvisualizer.Service;

namespace trackvisualizer.Vm
{
    public class TrackManagerVm : INotifyPropertyChanged
    {
        public UiLoggingVm Logging { get; }
        public HeightmapManagerVm HeightmapManager { get; }
        private readonly TrekplannerConfiguration _configuration;
        private readonly GeoLoaderService _geoLoader;
        private readonly Func<string, TrackVm> _trackGeneratorFunc;
        private TrackVm _activeTrack;

        private ObservableCollection<TrackVm> _availableTracks;
        private bool _isLoading;

        public ObservableCollection<TrackVm> AvailableTracks
        {
            get => _availableTracks;
            set
            {
                if (Equals(value, _availableTracks)) return;
                _availableTracks = value;
                OnPropertyChanged();
            }
        }

        public TrackVm ActiveTrack
        {
            get => _activeTrack;
            set
            {
                if (Equals(value, _activeTrack)) return;
                _activeTrack = value;
                OnPropertyChanged();

                //Load track on activation
                if (_activeTrack?.IsLoaded == false && 
                    _activeTrack?.IsOkToLoad != false)
                {
                    IsLoading = true;
                    Logging.ResetLog();
                    _activeTrack.LoadAsync().ContinueWith(t => { IsLoading = false; });
                }
            }
        }

        public ICommand LoadFromFileCommand { get; }

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (value == _isLoading) return;
                _isLoading = value;
                OnPropertyChanged();
            }
        }


        //xaml
        public TrackManagerVm()
        {

        }

        public TrackManagerVm(TrekplannerConfiguration configuration, 
            UiLoggingVm logging,
            GeoLoaderService geoLoader,
            HeightmapManagerVm heightmapManager,
            Func<string, TrackVm> trackGeneratorFunc)
        {
            Logging = logging;
            HeightmapManager = heightmapManager;
            _configuration = configuration;
            _geoLoader = geoLoader;
            _trackGeneratorFunc = trackGeneratorFunc;

            AvailableTracks = new ObservableCollection<TrackVm>(configuration.LastUsedTrackNames.Select(_trackGeneratorFunc));
            LoadFromFileCommand = new DelegateCommand(t => true, LoadTrackFromFileAsync);            
        }

        private async void LoadTrackFromFileAsync(object obj)
        {
            Logging.ResetLog();
            var availableFormats = _geoLoader.GetAvailableFormatList().ToArray();

            if (availableFormats.Length == 0)
            {
                Logging.LogError(Resources.TrackManagerVm_LoadTrackFromFileAsync_No_loader_middleware);
                return;
            }

            var openFileDialog = new OpenFileDialog
            {
                DefaultExt = @"."+availableFormats.FirstOrDefault()?.Item2.FirstOrDefault(),
                Filter =string.Join(@"|",
                    availableFormats.Select(
                        tuple => 
                            tuple.Item1+@"|"+string.Join(@";",
                                tuple.Item2.Select(i2=>@"*."+i2))))
            };

            if (openFileDialog.ShowDialog() != true)
                return;

            IsLoading = true;

            try
            {
                var candidateTrack = _trackGeneratorFunc(openFileDialog.FileName);

                if (!await candidateTrack.LoadAsync())
                    return;

                if(!AvailableTracks.Contains(candidateTrack))
                    AvailableTracks.Add(candidateTrack);

                ActiveTrack = candidateTrack;

                if (!_configuration.LastUsedTrackNames.Contains(candidateTrack.SourceTrackFileName))
                    _configuration.LastUsedTrackNames.Add(candidateTrack.SourceTrackFileName);

                _configuration.LastLoadedTrackFilename = candidateTrack.SourceTrackFileName;
                _configuration.Save();
            }
            finally
            {
                IsLoading = false;
            }
        }

        public async Task Initialize()
        {
            var activeTrackFilename = _configuration.LastLoadedTrackFilename;

            if (string.IsNullOrWhiteSpace(activeTrackFilename))
                return;

            var candidateTrack = _trackGeneratorFunc(activeTrackFilename);

            if (!candidateTrack.Exists())
            {
                AvailableTracks.Remove(candidateTrack);

                _configuration.LastUsedTrackNames.Remove(candidateTrack.SourceTrackFileName);
                _configuration.LastLoadedTrackFilename = null;
                _configuration.Save();

                return;
            }

            ActiveTrack = candidateTrack;        
        }

        public async Task ReloadActiveTrackAsync()
        {
            IsLoading = true;

            try
            {
                if (ActiveTrack == null)
                    return;
                                
                Logging.ResetLog();
                await ActiveTrack.LoadAsync();

            }
            finally
            {
                IsLoading = false;
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
