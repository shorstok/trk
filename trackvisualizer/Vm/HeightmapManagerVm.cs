using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using trackvisualizer.Annotations;
using trackvisualizer.View;

namespace trackvisualizer.Vm
{
    public class HeightmapManagerVm : INotifyPropertyChanged
    {
        private readonly Func<HeightmapDownloaderWindow> _downloaderGenerator;
        private readonly Lazy<TrackManagerVm> _trackManager;
        private bool _areHeightmapsMissing;

        public ObservableCollection<string> MissingHeightmapNames { get; } = new ObservableCollection<string>();
        
        public ICommand DownloadMissingHeightmapsCommand { get; }

        public bool AreHeightmapsMissing
        {
            get => _areHeightmapsMissing;
            set
            {
                if (value == _areHeightmapsMissing) return;
                _areHeightmapsMissing = value;
                CommandManager.InvalidateRequerySuggested();
                OnPropertyChanged();
            }
        }

        public HeightmapManagerVm(Func<HeightmapDownloaderWindow> downloaderGenerator, Lazy<TrackManagerVm> trackManager)
        {
            _downloaderGenerator = downloaderGenerator;
            _trackManager = trackManager;

            CollectionChangedEventManager.AddHandler(MissingHeightmapNames,(sender, args) => AreHeightmapsMissing = MissingHeightmapNames.Any());

            DownloadMissingHeightmapsCommand = new DelegateCommand(t=>AreHeightmapsMissing,DownloadMissingHeightmapsAsync);
        }

        private async void DownloadMissingHeightmapsAsync(object obj)
        {
            var window = _downloaderGenerator();

            var cts = new TaskCompletionSource<bool>();

            window.Closed += (sender, args) => cts.TrySetResult(true);

            window.Show();

            await cts.Task;

            await _trackManager.Value.ReloadActiveTrackAsync();
        }

        public void RegisterMissingSrtm(string filename)
        {
            if(!MissingHeightmapNames.Contains(filename))
                MissingHeightmapNames.Add(filename);
        }

        public void UnregisterMissingSrtm(string missingSrtmName)
        {
            MissingHeightmapNames.Remove(missingSrtmName);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        
    }
}
