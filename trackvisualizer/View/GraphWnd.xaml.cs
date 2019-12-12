using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace trackvisualizer.View
{
    /// <summary>
    ///     Interaction logic for GraphWnd.xaml
    /// </summary>
    public partial class GraphWnd : Window
    {
        public GraphWnd()
        {
            InitializeComponent();
        }


        private void SaveGraphAsImageButton_OnClick(object sender, RoutedEventArgs e)
        {
            var sfd = new SaveFileDialog
            {
                Filter = "PNG| *.png",
                FileName = "graph.png",
                DefaultExt = "*.png"
            };

            if (sfd.ShowDialog() != true)
                return;

            var rtb = new RenderTargetBitmap((int) ScatterGraphElement.ActualWidth,
                (int) ScatterGraphElement.ActualHeight, 96, 96, PixelFormats.Pbgra32);
            rtb.Render(ScatterGraphElement);

            var png = new PngBitmapEncoder();
            png.Frames.Add(BitmapFrame.Create(rtb));

            using (var stream = File.OpenWrite(sfd.FileName))
            {
                png.Save(stream);
            }
        }
    }
}