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
using trackvisualizer.Vm;

namespace trackvisualizer.View
{
    /// <summary>
    /// Interaction logic for ChoiceWindow.xaml
    /// </summary>
    public partial class ChoiceWindow : Window
    {
        public class Choice
        {
            public string Description { get; }
            public string Id { get; }


            public Choice(string id,string description)
            {
                Description = description;
                Id = id;
            }
        }

        
        public ICommand ChoseItemCommand { get; }

        public string ChosenId { get; set; }

        public Choice[] Options { get; }

        public ChoiceWindow(IEnumerable<Tuple<string, string>> options, string title)
        {
            Options = options.Select(t => new Choice(t.Item1, t.Item2)).ToArray();

            ChoseItemCommand = new DelegateCommand(t=>true, ChooseItem);

            ChoiceTitle.Text = title;

            InitializeComponent();
        }

        private void ChooseItem(object obj)
        {
            if(!(obj is Choice choice))
                return;

            ChosenId = choice.Id;
            Close();
        }

        private void ChoiceWindow_OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            ChosenId = null;
            Close();
        }
    }
}
