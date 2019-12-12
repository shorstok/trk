using System;
using System.Threading.Tasks;
using System.Windows;
using trackvisualizer.Service;

namespace trackvisualizer.View
{
    /// <summary>
    ///     Логика взаимодействия для Error.xaml
    /// </summary>
    public partial class Error : Window
    {
        public Error(string text)
        {
            Owner = Application.Current.MainWindow;

            InitializeComponent();

            ErrorTxt.Text = text;

            PreviewMouseLeftButtonUp += delegate { Close(); };
        }
    }
}