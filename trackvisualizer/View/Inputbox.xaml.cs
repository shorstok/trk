using System.Windows;
using System.Windows.Input;

namespace trackvisualizer.View
{
    /// <summary>
    ///     Логика взаимодействия для Inputbox.xaml
    /// </summary>
    public partial class Inputbox : Window
    {
        public string Result = null;

        public Inputbox(string what, string initialValue)
        {
            Owner = Application.Current.MainWindow;

            InitializeComponent();

            DescrTxt.Text = what;
            EnteredValue.Text = initialValue;

            Loaded += Inputbox_Loaded;
            PreviewKeyDown += Inputbox_PreviewKeyDown;
        }

        private void Inputbox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Return) 
                return;

            Result = EnteredValue.Text;
            Close();
            e.Handled = true;
        }

        private void Inputbox_Loaded(object sender, RoutedEventArgs e)
        {
            EnteredValue.SelectAll();
            EnteredValue.Focus();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Result = null;
            Close();
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            Result = EnteredValue.Text;
            Close();
        }

    }
}