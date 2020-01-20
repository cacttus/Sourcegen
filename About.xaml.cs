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
                Globals.GetTitle() + " v" + Globals.GetFullVersion() + "\r\n" +
                "C++/Java Class Generator\n" +
                "Created By Github/Metalmario971\n" +
                "\n" +
                "Usage\r\n " +
                "Fill out the \"type files here\" box with file names you want to generate,\r\n ex: myfile.h." +
                "  You may use paths, c:\\myfile.h, and double quotes for spaces \"c:\\my dir\\myfile.txt\".\r\n You can type multiple file paths in the box.\r\n\r\n" +
                "Click the expander arrow below to show the configuration options. " +
                " You can save this configuration for future sources with File->Save. Additionally checking the \"Set Current Configuration As Default\" " +
                " menu option sets the current configuration values as the default values each time Sourcegen loads.\r\n\r\n" +
                " Pressing Enter at any time will generate the files, or you can select 'Generate' from the 'File' menu."
                ;
        }

        private void _btnOk_Click(object sender, RoutedEventArgs e)
        {
            Hide();
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
