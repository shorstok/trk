using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using trackvisualizer.Annotations;
using trackvisualizer.Service;

namespace trackvisualizer.Vm
{
    public class UiLoggingVm : IUiLoggingService, INotifyPropertyChanged
    {
        private bool _logHasEntries;

        public bool LogHasEntries
        {
            get => _logHasEntries;
            set
            {
                if (value == _logHasEntries) return;
                _logHasEntries = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<string> ActivityLog { get; } = new ObservableCollection<string>();

        public UiLoggingVm()
        {
            CollectionChangedEventManager.AddHandler(ActivityLog, (sender, args) => LogHasEntries = ActivityLog.Any());
        }

        public void ResetLog()
        {
            ActivityLog.Clear();
        }

        public async void Log(string text, bool persist = false)
        {
            ActivityLog.Add(text);

            if (persist)
                return;

            await Task.Delay(400);

            ActivityLog.Remove(text);
        }

        public void LogError(string errorText)
        {
            throw new NotImplementedException();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}