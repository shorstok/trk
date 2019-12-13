using System.ComponentModel;
using System.Runtime.CompilerServices;
using trackvisualizer.Annotations;

namespace trackvisualizer.Vm
{
    public class UiLogItemVm : INotifyPropertyChanged
    {
        private bool _isError;
        public string Text { get; }
        public bool Important { get; }

        public bool IsError
        {
            get => _isError;
            set
            {
                if (value == _isError) return;
                _isError = value;
                OnPropertyChanged();
            }
        }

        public UiLogItemVm(string text, bool important)
        {
            Text = text;
            Important = important;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) => 
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}