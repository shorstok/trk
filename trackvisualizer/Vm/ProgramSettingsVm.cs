using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using trackvisualizer.Annotations;
using trackvisualizer.Config;
using trackvisualizer.Service;

namespace trackvisualizer.Vm
{
    public class ProgramSettingsVm : INotifyPropertyChanged
    {
        public class LocalizationVm
        {
            public string Name { get; }
            public string Code { get; }

            public LocalizationVm(string name, string code)
            {
                Name = name;
                Code = code;
            }
        }

        public LocalizationVm[] Localizations { get; }

        public LocalizationVm ActiveLocalization
        {
            get => _activeLocalization;
            set
            {
                if (Equals(value, _activeLocalization)) return;
                _activeLocalization = value;
                OnPropertyChanged();
            }
        }

        private readonly TrekplannerConfiguration _configuration;
        private LocalizationVm _activeLocalization;


        public ICommand SaveChangesCommand { get; }

        public ProgramSettingsVm(TrekplannerConfiguration configuration, LocalizationManager localizationManager)
        {
            _configuration = configuration;
            
            Localizations = localizationManager.AvailableLocalizations.Select(p => new LocalizationVm(p.Item2, p.Item1))
                .ToArray();

            ActiveLocalization = Localizations.FirstOrDefault(l => l.Code == configuration.CurrentLanguage) ??
                                 Localizations.First();

            SaveChangesCommand = new DelegateCommand(t=>true, OnSaveChanges);
        }

        private void OnSaveChanges(object obj)
        {
            bool restartNeeded = !Equals(_configuration.CurrentLanguage, ActiveLocalization?.Code);

            _configuration.CurrentLanguage = ActiveLocalization?.Code;

            _configuration.Save();

            if (restartNeeded)
            {
                System.Diagnostics.Process.Start(Application.ResourceAssembly.Location);
                Application.Current.Shutdown();
            }
        }

        public ProgramSettingsVm()
        {
            
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
