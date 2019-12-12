using System;
using System.Windows;
using System.Windows.Controls;
using trackvisualizer.Vm;

namespace trackvisualizer.View
{
    /// <summary>
    ///     Interaction logic for BackpackWeightWnd.xaml
    /// </summary>
    public partial class BackpackWeightWnd : Window
    {
        public WeightSettings Result;

        public BackpackWeightWnd(WeightSettings settings)
        {
            Result = settings;
            DataContext = settings;
            InitializeComponent();
        }

        private void BCancel_Click(object sender, RoutedEventArgs e)
        {
            Result = null;
            Close();
        }

        private void BOk_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}