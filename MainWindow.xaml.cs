using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace sourcegen
{
    enum Filetype
    {
        [Description("Java")]
        Java,
        [Description("C++ Class")]
        CPP_Class,
        [Description("C++ Header")]
        CPP_Header,
        [Description("C++ Source")]
        CPP_Source,
        [Description("None")]
        None
    }

    public partial class MainWindow : Window
    {
        List<TextBox> _dataControls;
        About _about = new About();
        string _log;
        private double optionsHeight = 0;

        public static string GetEnumDescription(Enum value)
        {
            FieldInfo fi = value.GetType().GetField(value.ToString());

            DescriptionAttribute[] attributes = fi.GetCustomAttributes(typeof(DescriptionAttribute), false) as DescriptionAttribute[];

            if (attributes != null && attributes.Any())
            {
                return attributes.First().Description;
            }

            return value.ToString();
        }

        public MainWindow()
        {
            InitializeComponent();

            _dataControls = new List<TextBox>() {
                _txtLicense,
                _txtAuthor,
                _txtCopyright,
                _txtNamespace,
                _txtBaseClass,
                _txtPackage};

            foreach (TextBox t in _dataControls)
            {
                DisableHide(t, false);
            }
            _grpOptions.IsExpanded = true;
            optionsHeight = _grpOptions.Height - GetToggleHeight();

            _grpOptions.IsExpanded = false;
            _grpOptions.Visibility = Visibility.Hidden;

            _txtLicense.Text = GetBSDLicense();

            //Populate listbox
            foreach (Filetype v in Enum.GetValues(typeof(Filetype)).Cast<Filetype>())
            {
                string desc = GetEnumDescription((Filetype)v);
                ListBoxItem lbi = new ListBoxItem();
                lbi.Content = desc;
                lbi.Tag = v;
                _cboFileType.Items.Add(lbi);
            }
        }
        private double GetToggleHeight()
        {
            if (VisualTreeHelper.GetChildrenCount(_grpOptions) > 0)
            {
                var border = VisualTreeHelper.GetChild(_grpOptions, 0);
                var dockpanel = VisualTreeHelper.GetChild(border, 0);
                var togglebutton = VisualTreeHelper.GetChild(dockpanel, 0); // it may be not 0th, so please enumerate all children using VisualTreeHelper.GetChildrenCount(dockpanel) and find that ToggleButton
                if ((togglebutton as Button) != null)
                {
                    return (togglebutton as Button).ActualHeight;
                }
            }
            return 20;
        }
        private void Log(string x)
        {
            _log += (x + Environment.NewLine);
            if (_about != null)
            {
                _about.SetLog(_log);
            }
        }
        private void About_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            _about.Show();
        }
        private string GetFileName()
        {
            return System.IO.Path.GetFileName(_txtFilename.Text);
        }
        private string RandomDigits()
        {
            string ret = "";
            Random r = new Random();
            for (int i = 0; i < 16; ++i)
            {
                ret += (r.Next() % 9).ToString();
            }
            return ret;
        }
        private int IndentSize()
        {
            int n = 0;
            if (!Int32.TryParse(_txtSpaces.Text, out n))
            {
                Log("Error parsing spaces number.");
            }
            return n;
        }
        private string Indent(string x)
        {
            string y = x.Replace("$", new string(' ', IndentSize()));
            return y;
        }
        private string GetBSDLicense()
        {
            string x = "*$THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS \"AS IS\" AND ANY EXPRESS OR IMPLIED\n" +
                "*$WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A\n" +
                "*$PARTICULAR PURPOSE ARE DISCLAIMED.IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR\n" +
                "*$ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES(INCLUDING, BUT NOT LIMITED\n" +
                "*$TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION)\n" +
                "*$HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT(INCLUDING\n" +
                "*$NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE\n" +
                "*$POSSIBILITY OF SUCH DAMAGE.\n";
            return x;
        }
        private string GetCompactDate()
        {
            return DateTime.Now.ToString("MM/dd/yyyy");
        }
        private string getHeaderCommentBlock()
        {
            //$ = tab
            string head = "";
            head += "/**\n";
            head += "*\n";
            head += "*$@file " + GetFileName() + "\n";
            if (_chkDateTime.IsChecked == true)
            {
                head += "*$@date " + GetCompactDate() + "\n";
            }
            if (_chkAuthor.IsChecked == true)
            {
                head += "*$@author " + _txtAuthor.Text + "\n";
            }
            if (_chkCopyright.IsChecked == true)
            {
                head += "*$@Copyright " + DateTime.Now.ToString("yyyy") + " " + _txtCopyright.Text + "\n";
            }
            head += "*\n";
            head += "*/\n";
            return head;
        }
        private string GetNamespace()
        {
            return _txtNamespace.Text;
        }
        private string getHeaderBody()
        {
            string head = "";
            head += "#pragma once\n";
            string guard = "__" + GetFileName().ToUpper() + "_" + RandomDigits() + "_H__";
            head += "#ifndef " + guard + "\n";
            head += "#define " + guard + "\n";
            head += "\n";
            head += "\n";
            head += "\n";
            if (_chkNamespace.IsChecked == true)
            {
                head += "namespace " + _txtNamespace.Text + " {\n";
            }
            //Class doc
            head += "/**\n";
            head += "*$@class ";
            head += GetFileName();
            head += "\n";
            head += "*$@brief\n";
            head += "*\n";
            head += "*/\n";
            //Class definition
            head += "class " + GetFileName();
            if (_chkBaseClass.IsChecked == true)
            {
                head += " : public " + _txtBaseClass.Text;
            }
            head += " {\n";
            head += "public:\n";
            head += "$";
            //Constructor
            head += GetFileName() + "();\n";
            head += "$virtual ~" + GetFileName() + "() " + (_chkBaseClass.IsChecked == true ? "override" : "") + ";\n";
            head += "};\n";
            head += "\n";
            if (_chkNamespace.IsChecked == true)
            {
                head += "}//ns " + _txtNamespace.Text + "\n";
            }
            head += "\n";
            head += "\n";
            head += "\n";
            head += "#endif\n";

            return head;
        }
        string GetCPPSource()
        {
            string head = "";
            head += "#include \"./" + GetFileName() + ".h\"\n";
            head += "\n";
            head += "\n";
            if (_chkNamespace.IsChecked == true)
            {
                head += "namespace " + _txtNamespace.Text + " {\n";
            }
            //Ctor
            head += GetFileName() + "::" + GetFileName() + "() {\n";
            head += "\n";
            head += "}\n";
            head += GetFileName() + "::~" + GetFileName() + "() {\n";
            head += "\n";
            head += "}\n";
            head += "\n";
            if (_chkNamespace.IsChecked == true)
            {
                head += "}//ns " + _txtNamespace.Text + "\n";
            }
            return head;
        }
        private string GetJavaSource()
        {
            string head = "";

            head += "/**\n";
            head += "*$@file " + GetFileName() + "\n";
            if (_chkDateTime.IsChecked == true)
            {
                head += "*$@date " + GetCompactDate() + "\n";
            }
            if (_chkAuthor.IsChecked == true)
            {
                head += "*$@author " + _txtAuthor.Text + "\n";
            }
            head += "*/\n";
            head += "\n";
            if (_chkPackage.IsChecked == true)
            {
                head += "package bro.cliffs;\n";
            }
            head += "//import\n";
            head += "\n";
            head += "/**\n";
            head += "*$@class " + GetFileName() + "\n";
            head += "*$@brief\n";
            head += "*/\n";
            head += "public class " + GetFileName() + (_chkBaseClass.IsChecked == true ? (" extends " + _txtBaseClass.Text + " ") : "") + " {\n";
            head += " \n";
            head += "}\n";
            head += "\n";
            head += "\n";
            head += "\n";
            head += "\n";

            return head;
        }
        private void WriteJavaSource()
        {
            string to_write = Indent(GetJavaSource());
            WriteFile(GetFileName() + ".java", to_write);
        }
        private void WriteCPPHeader()
        {
            string to_write = Indent(getHeaderCommentBlock() + getHeaderBody());
            WriteFile(GetFileName() + ".h", to_write);
        }
        private void WriteCPPSource()
        {
            string to_write = Indent(GetCPPSource());
            WriteFile(GetFileName() + ".cpp", to_write);
        }
        private Filetype GetSelectedFiletype()
        {
            if (_cboFileType.SelectedItem != null)
            {
                Filetype? tag = (_cboFileType.SelectedItem as ListBoxItem).Tag as Filetype?;
                if (tag != null)
                {
                    return tag.Value;
                }
            }
            return Filetype.None;
        }
        private void _btnGenerate_Click(object sender, RoutedEventArgs e)
        {
            Filetype ft = GetSelectedFiletype();
            if (ft == Filetype.Java)
            {
                WriteJavaSource();
            }
            else if (ft == Filetype.CPP_Class)
            {
                WriteCPPHeader();
                WriteCPPSource();
            }
            else if (ft == Filetype.CPP_Header)
            {
                WriteCPPHeader();
            }
            else if (ft == Filetype.CPP_Source)
            {
                WriteCPPSource();
            }
        }
        private void WriteFile(string filename, string data)
        {
            try
            {
                string ext = System.IO.Path.GetExtension(filename);
                Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
                dlg.FileName = filename; // Default file name
                dlg.DefaultExt = ext; // Default file extension
                dlg.Filter = "Text documents (" + ext + ")|*" + ext; // Filter files by extension

                // Show save file dialog box
                bool? result = dlg.ShowDialog();

                // Process save file dialog box results
                if (result == true)
                {
                    // Save document
                    string selected_fname = dlg.FileName;

                    using (var fs = new System.IO.FileStream(selected_fname, System.IO.FileMode.Create, System.IO.FileAccess.Write))
                    {
                        using (System.IO.StreamWriter bw = new System.IO.StreamWriter(fs))
                        {
                            bw.Write(data);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log("Error writing file: " + ex.ToString());
            }
        }

        private void _txtSpaces_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !new Regex("[^0-9.-]+").IsMatch(e.Text);
        }

        private void _btnDefaultLicense_Click(object sender, RoutedEventArgs e)
        {
            _txtLicense.Text = GetBSDLicense();
        }

        private void _cboFileType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Filetype ft = GetSelectedFiletype();
            if (ft == Filetype.Java)
            {
                _lblPackage.Visibility = _chkPackage.Visibility = _txtPackage.Visibility = Visibility.Visible;
                _lblNamespace.Visibility = _chkNamespace.Visibility = _txtNamespace.Visibility = Visibility.Hidden;

                _grpOptions.Visibility = Visibility.Visible;
            }
            else if (ft == Filetype.CPP_Class || ft == Filetype.CPP_Header || ft == Filetype.CPP_Source)
            {
                _lblPackage.Visibility = _chkPackage.Visibility = _txtPackage.Visibility = Visibility.Hidden;
                _lblNamespace.Visibility = _chkNamespace.Visibility = _txtNamespace.Visibility = Visibility.Visible;

                _grpOptions.Visibility = Visibility.Visible;
            }
            else if (ft == Filetype.None)
            {
                _lblPackage.Visibility = _chkPackage.Visibility = _txtPackage.Visibility = Visibility.Hidden;
                _lblNamespace.Visibility = _chkNamespace.Visibility = _txtNamespace.Visibility = Visibility.Hidden;

                _grpOptions.Visibility = Visibility.Hidden;
            }

        }
        private void _Configure_Shake(object sender, RoutedEventArgs e)
        {
            SelectFileType(Filetype.CPP_Class);

            _chkLicense.IsChecked = true;
            _txtLicense.Text = GetBSDLicense();

            _chkAuthor.IsChecked = true;
            _txtAuthor.Text = "MetalMario971";

            _chkDateTime.IsChecked = true;

            _chkCopyright.IsChecked = true;
            _txtCopyright.Text = "MetalMario971";

            _chkNamespace.IsChecked = true;
            _txtNamespace.Text = "Game";

            _chkBaseClass.IsChecked = true;
            _txtBaseClass.Text = "GameMemory";

            _txtSpaces.Text = "2";
        }
        private void _Configure_MonogameToolkit(object sender, RoutedEventArgs e)
        {
            SelectFileType(Filetype.CPP_Class);

            _chkLicense.IsChecked = true;
            _txtLicense.Text = GetBSDLicense();

            _chkAuthor.IsChecked = true;
            _txtAuthor.Text = "MetalMario971";

            _chkDateTime.IsChecked = true;

            _chkCopyright.IsChecked = true;
            _txtCopyright.Text = "MetalMario971";

            _chkNamespace.IsChecked = true;
            _txtNamespace.Text = "MonoTK";

            _chkBaseClass.IsChecked = false;
            _txtBaseClass.Text = "";

            _txtSpaces.Text = "2";
        }
        private void SelectFileType(Filetype ft)
        {
            foreach (object o in _cboFileType.Items)
            {
                if (o is ListBoxItem)
                {
                    Filetype? tag = (o as ListBoxItem).Tag as Filetype?;

                    if (tag != null && tag == ft)
                    {
                        _cboFileType.SelectedItem = o;
                        break;
                    }
                }
            }
        }

        private void _chkLicense_Checked(object sender, RoutedEventArgs e)
        {
            DisableHide(_txtLicense, (_chkLicense.IsChecked == true));
        }
        private void _chkAuthor_Checked(object sender, RoutedEventArgs e)
        {
            DisableHide(_txtAuthor, (_chkAuthor.IsChecked == true));
        }
        private void _chkCopyright_Checked(object sender, RoutedEventArgs e)
        {
            DisableHide(_txtCopyright, (_chkCopyright.IsChecked == true));
        }
        private void _chkNamespace_Checked(object sender, RoutedEventArgs e)
        {
            DisableHide(_txtNamespace, (_chkNamespace.IsChecked == true));
        }
        private void _chkBaseClass_Copy_Checked(object sender, RoutedEventArgs e)
        {
            DisableHide(_txtBaseClass, (_chkBaseClass.IsChecked == true));
        }
        private void _chkPackage_Checked(object sender, RoutedEventArgs e)
        {
            DisableHide(_txtPackage, (_chkPackage.IsChecked == true));
        }
        private void DisableHide(TextBox tb, bool b)
        {
            tb.IsEnabled = b;
            tb.Visibility = b ? Visibility.Visible : Visibility.Hidden;
        }
        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Application.Current.Shutdown();
        }
        private void _btnClear_Click(object sender, RoutedEventArgs e)
        {
            foreach (TextBox t in _dataControls)
            {
                t.Text = "";
            }
            _grpOptions.Visibility = Visibility.Hidden;
            _cboFileType.SelectedItem = null;
            _txtLicense.Text = GetBSDLicense();

        }
        private void _grpOptions_Expanded(object sender, RoutedEventArgs e)
        {
            if (optionsHeight > 0)
            {
                if (_grpOptions.IsExpanded == true)
                {
                    this.Height += optionsHeight;
                }
                else
                {
                    this.Height -= optionsHeight;
                }
            }
        }
        private void Window_Closed(object sender, EventArgs e)
        {
            //Wasn't exiting..
            System.Windows.Application.Current.Shutdown();
        }
    }
}
