using System.ComponentModel;
using System.Runtime.CompilerServices;
using trackvisualizer.Annotations;

namespace trackvisualizer.Vm
{
    public class DownloadProgressItemVm : INotifyPropertyChanged
    {
        private double _progress;
        private string _message;
        public string SrtmName { get; }

        public double Progress
        {
            get => _progress;
            set
            {
                if (value.Equals(_progress)) return;
                _progress = value;
                OnPropertyChanged();
            }
        }

        public string Message
        {
            get => _message;
            set
            {
                if (value == _message) return;
                _message = value;
                OnPropertyChanged();
            }
        }

        public DownloadProgressItemVm(string srtmName)
        {
            SrtmName = srtmName;
        }

        public void ReportProgress(double progress, string message)
        {
            Progress = progress;
            Message = message;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }


    }
}