using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using trackvisualizer.Annotations;
using trackvisualizer.Config;

namespace trackvisualizer.Vm
{
    public class MainSettingsVm : INotifyPropertyChanged
    {
        private readonly TrekplannerConfiguration _configuration;

        //xaml
        public MainSettingsVm()
        {
            
        }

        public MainSettingsVm(TrekplannerConfiguration configuration)
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
}
