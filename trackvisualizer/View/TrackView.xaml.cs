using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using trackvisualizer.Service;
using trackvisualizer.Vm;

namespace trackvisualizer.View
{
    /// <summary>
    /// Interaction logic for TrackView.xaml
    /// </summary>
    public partial class TrackView : Window
    {
        private readonly Func<TrackManagerVm> _trackManagerGenerator;

        public TrackView(Func<TrackManagerVm> trackManagerGenerator)
        {
            _trackManagerGenerator = trackManagerGenerator;
            
            InitializeComponent();
            Loaded += TrackView_Loaded;
        }

        private async void TrackView_Loaded(object sender, RoutedEventArgs e)
        {
            DataContext = _trackManagerGenerator();

            await ((TrackManagerVm) DataContext).Initialize();
        }
    }
}
