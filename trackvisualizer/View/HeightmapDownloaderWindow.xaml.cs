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
    /// Interaction logic for HeightmapDownloaderWindow.xaml
    /// </summary>
    public partial class HeightmapDownloaderWindow : Window
    {
        public HeightmapDownloaderWindow(Func<HeightmapDownloaderVm> viewmodelGenerator)
        {
            var heightmapDownloaderVm = viewmodelGenerator();

            DataContext = heightmapDownloaderVm;
            Owner = Application.Current.MainWindow;

            InitializeComponent();

            heightmapDownloaderVm.WorkCompleted += HeightmapDownloaderVm_WorkCompleted;

            Loaded += HeightmapDownloaderWindow_Loaded;
        }

        private void HeightmapDownloaderVm_WorkCompleted()
        {
            Close();
        }

        private async void HeightmapDownloaderWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if(!await ((HeightmapDownloaderVm) DataContext).Initialize())
                Close();
        }
    }
}
