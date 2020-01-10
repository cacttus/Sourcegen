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

namespace sourcegen
{
    /// <summary>
    /// Interaction logic for About.xaml
    /// </summary>
    public partial class About : Window
    {
        public About()
        {
            InitializeComponent();
            _txtInfo.Text =
                "Sourcegen\n" +
                "C++/Java Class Generator\n" +
                "Created By: Github / Metalmario971\n" +
                "Jan 2020\n"
                ;
        }

        private void _btnOk_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
        public void SetLog(string l)
        {
            _txtLog.Text = l;
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }
    }
}
