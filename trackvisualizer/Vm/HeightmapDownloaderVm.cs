using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using trackvisualizer.Annotations;
using trackvisualizer.Config;
using trackvisualizer.Service;

namespace trackvisualizer.Vm
{
    public class HeightmapDownloaderVm : INotifyPropertyChanged
    {
        private readonly HeightmapManagerVm _heightmapManager;
        private readonly IUiService _uiService;
        private readonly TrekplannerConfiguration _configuration;

        public ObservableCollection<IHeightmapProvider> HeightmapProviders { get; }

        public ICommand StartDownloadCommand { get; }
        public ICommand AbortDownloadCommand { get; }

        public CancellationTokenSource Cancellation { get; private set; } = new CancellationTokenSource();

        public ObservableCollection<DownloadProgressItemVm> DownloadProgress { get; } = new ObservableCollection<DownloadProgressItemVm>();

        private IHeightmapProvider _activeHeightmapProvider;

        public IHeightmapProvider ActiveHeightmapProvider
        {
            get => _activeHeightmapProvider;
            set
            {
                if (Equals(value, _activeHeightmapProvider)) return;
                _activeHeightmapProvider = value;

                _configuration.Heightmap.ActiveProviderId = value?.InternalId;
                _configuration.Save();

                OnPropertyChanged();
            }
        }

        private bool _isDownloadActive;
        public bool IsDownloadActive
        {
            get => _isDownloadActive;
            set
            {
                if (value == _isDownloadActive) return;
                _isDownloadActive = value;
                CommandManager.InvalidateRequerySuggested();
                OnPropertyChanged();
            }
        }

        //xaml design-time ctor
        public HeightmapDownloaderVm()
        {
            HeightmapProviders = new ObservableCollection<IHeightmapProvider>();
        }

        public HeightmapDownloaderVm(IEnumerable<IHeightmapProvider> providers,
            HeightmapManagerVm heightmapManager,
            IUiService uiService,
            TrekplannerConfiguration configuration)
        {
            _heightmapManager = heightmapManager;
            _uiService = uiService;
            _configuration = configuration;

            HeightmapProviders =
                new ObservableCollection<IHeightmapProvider>(providers.OrderByDescending(p => p.Priority));

            StartDownloadCommand = new DelegateCommand(t => !IsDownloadActive, StartDownloadAsync);
            AbortDownloadCommand = new DelegateCommand(t => IsDownloadActive, o => Cancellation.Cancel());
        }

        private async void StartDownloadAsync(object obj)
        {
            IsDownloadActive = true;

            Cancellation = new CancellationTokenSource();
            DownloadProgress.Clear();

            try
            {
                var provider = ActiveHeightmapProvider;

                if (null == provider)
                {
                    await _uiService.NofityError("Выберите источник для загрузки");
                    return;
                }

                var heightmaps = _heightmapManager.MissingHeightmapNames.ToArray();

                foreach (var missingSrtmName in heightmaps)
                {
                    var progressReporter = new DownloadProgressItemVm(missingSrtmName);

                    DownloadProgress.Add(progressReporter);

                    if (!await provider.DownloadHeightmap(missingSrtmName,
                        (progress, message) => progressReporter.ReportProgress(progress, message),
                        Cancellation.Token))
                    {
                        DownloadProgress.Remove(progressReporter);
                        return;
                    }

                    DownloadProgress.Remove(progressReporter);

                    _heightmapManager.UnregisterMissingSrtm(missingSrtmName);
                }

                OnWorkCompleted();
            }
            catch (System.Net.WebException e)
            {
                _uiService.NofityError(e.Message).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {

            }
            finally
            {
                IsDownloadActive = false;
            }
        }


        public async Task<bool> Initialize()
        {
            if (!HeightmapProviders.Any())
            {
                await _uiService.NofityError("Отсутствуют источники для загрузки карт высоты");
                return false;
            }
            
            //Select last active provider or first in list

            ActiveHeightmapProvider =
                HeightmapProviders.FirstOrDefault(hp => hp.InternalId == _configuration.Heightmap?.ActiveProviderId) ??
                HeightmapProviders.First();        

            return true;
        }

        public event Action WorkCompleted;

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {        
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected virtual void OnWorkCompleted()
        {
            WorkCompleted?.Invoke();
        }
    }
}
