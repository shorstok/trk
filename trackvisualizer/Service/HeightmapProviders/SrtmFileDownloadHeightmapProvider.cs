using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Security.Permissions;
using System.Threading;
using System.Threading.Tasks;
using trackvisualizer.Annotations;
using trackvisualizer.Config;

namespace trackvisualizer.Service.HeightmapProviders
{
    public class SrtmFileDownloadHeightmapProviderSettings : INotifyPropertyChanged
    {
        private readonly TrekplannerConfiguration _configuration;

        public string SrtmBasePathTemplate
        {
            get => _configuration.Heightmap.SrtmBaseUrlTemplate;
            set
            {
                if (value == _configuration.Heightmap.SrtmBaseUrlTemplate) return;
                _configuration.Heightmap.SrtmBaseUrlTemplate = value;
                _configuration.Save();
                OnPropertyChanged();
            }
        }

        public SrtmFileDownloadHeightmapProviderSettings(TrekplannerConfiguration configuration)
        {
            _configuration = configuration;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class SrtmFileDownloadHeightmapProvider : IHeightmapProvider, INotifyPropertyChanged
    {
        private readonly TrekplannerConfiguration _configuration;
        private readonly IUiService _uiService;

        public Guid InternalId => Guid.Parse("6E5C367A-4C4E-4E8E-BA95-FDBDFC39F85A");

        private bool _isAvailable = true;
        public bool IsAvailable
        {
            get => _isAvailable;
            private set
            {
                if (value == _isAvailable) return;
                _isAvailable = value;
                OnPropertyChanged();
            }
        }
        
        public int Priority => 1;
        public string Description { get; } = "SRTM";
        public int MaxConcurrentInstances => 1;
        public object Settings { get; }

        public SrtmFileDownloadHeightmapProvider(TrekplannerConfiguration configuration, IUiService uiService)
        {
            Settings = new SrtmFileDownloadHeightmapProviderSettings(configuration);
            _configuration = configuration;
            _uiService = uiService;
        }

        public async Task<bool> DownloadHeightmap(string missingSrtmName, Action<double, string> reportProgressAsync,
            CancellationToken token)
        {
            var srtmCleanerPath = PathService.StartupDir + "\\" + "srtm_interp.exe";

            var temporaryFilenameUnpacked =                     
                Path.Combine(PathService.GetOrCreateTempDirectory(_configuration), $"downloaded-{missingSrtmName}.hgt");
            var temporaryArchiveName = temporaryFilenameUnpacked + ".zip";

            var srtmFilePacked = missingSrtmName + ".hgt.zip";

            if (!File.Exists(srtmCleanerPath))
            {
                await _uiService.NofityError($"Не найдена програма коррекции высот `{srtmCleanerPath}`");
                return false;
            }

            try
            {
                using (var client = new WebClient())
                {
                    token.Register(() => client.CancelAsync());

                    var sourcePath = PathService.MapPath(_configuration.Heightmap.SrtmBaseUrlTemplate,
                        Tuple.Create(HeightmapTemplateTokens.SrtmZippedName,srtmFilePacked));
                
                    reportProgressAsync(0, "Загрузка " + srtmFilePacked + " ...");

                    client.DownloadProgressChanged += (sender, args) =>
                    {
                        var progressRelative = 0.8 * args.BytesReceived / args.TotalBytesToReceive;

                        reportProgressAsync(progressRelative, $"Загрузка {args.BytesReceived/1e3:0.0} КБ...");
                    };

                    await client.DownloadFileTaskAsync(
                        sourcePath,
                        temporaryArchiveName);
                }
            }
            catch (System.Net.WebException e)
            {
                if ((e.Response as HttpWebResponse)?.StatusCode == HttpStatusCode.NotFound)
                {
                    var extendedError = "Удаленный сервер вернул 404 (не найден). Причины могут быть следующие:" +Environment.NewLine+
                                        "🔘 SRTM-файлов больше нет на севрере, адрес устарел и нужно искать следующий" +Environment.NewLine+
                                        "🔘 Неверно прописан путь к SRTM в окне загрузчика";

                    _uiService.NofityError(extendedError).ConfigureAwait(false);
                }
                else
                    _uiService.NofityError(e.Message).ConfigureAwait(false);
                return false;
            }

            reportProgressAsync(0.8, "Распаковка " + srtmFilePacked);
            
            try
            {
                var srtmFileUnpackedName = missingSrtmName + ".hgt";

                using (var archive = ZipFile.OpenRead(temporaryArchiveName))
                {
                    var hgtEntry =
                        archive.Entries.FirstOrDefault(e =>
                            e.FullName.EndsWith(".hgt", StringComparison.InvariantCultureIgnoreCase));

                    if (null == hgtEntry)
                    {
                        reportProgressAsync(0.8,srtmFilePacked + " - файл .hgt не найден в архиве, что-то не так");
                        return false;
                    }

                    if (File.Exists(srtmFileUnpackedName))
                        File.Delete(srtmFileUnpackedName);

                    hgtEntry.ExtractToFile(srtmFileUnpackedName);
                }

                File.Delete(temporaryArchiveName);

                reportProgressAsync(0.9,missingSrtmName + ".hgt, подчистка лакун... ");

                if (!await ExecuteCommandAsync(srtmCleanerPath, srtmFileUnpackedName))
                    return false;

                reportProgressAsync(1,srtmFilePacked + " готов. ");

                return true;
            }
            catch (Exception e)
            {
                await _uiService.NofityError($"Ошибка при распаковке: `{e.Message}`! Файл " + srtmFilePacked +
                                             " загружен, но вам придётся распаковать его вручную. ");

                return false;
            }
        }

        [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
        public async Task<bool> ExecuteCommandAsync(string file, string args)
        {
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo(file, args)
                    {
                        UseShellExecute = false
                    },
                    EnableRaisingEvents = true
                };

                var tcs = new TaskCompletionSource<int>();

                process.Exited += (s, a) =>
                {
                    tcs.SetResult(process.ExitCode);
                    process.Dispose();
                };

                process.Start();

                return await tcs.Task!=1;
            }
            catch (Exception ex)
            {
                await _uiService.NofityError("Ошибка: " + ex.Message);
                return false;
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