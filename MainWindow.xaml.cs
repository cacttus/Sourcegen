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
    /// <summary>
    /// Custom attribute storing a list of possible extensions for a filetype.  Includes utility methods to convert to/from filetype.
    /// </summary>
    public class ExtensionList : Attribute
    {
        public string _atts = null;
        public ExtensionList(string atts)
        {
            this._atts = atts;
        }
        /// <summary>
        /// Returns a list of filetypes that contain the given extension.  Extension starts with a '.'.
        /// </summary>
        /// <param name="extension"></param>
        /// <returns></returns>
        public static List<Filetype> GetPossibleExtensionMappings(string extension)
        {
            List<Filetype> ft = new List<Filetype>();
            MemberInfo[] fis = typeof(Filetype).GetFields();

            foreach (var fi in fis)
            {
                ExtensionList[] extlists = (ExtensionList[])fi.GetCustomAttributes(typeof(ExtensionList), false);
                foreach (ExtensionList extlist in extlists)
                {
                    foreach (string ext in ExtensionList.AttsToList(extlist._atts))
                    {
                        if (ext.Trim().ToLower().Equals(extension.Trim().ToLower()))
                        {
                            ft.Add((Filetype)Enum.Parse(typeof(Filetype), fi.Name));
                        }
                    }
                }
            }
            return ft;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="extension"></param>
        /// <param name="unique_only">Set to true to only return a 1-1 mapping of type to extension.  
        /// For instance C++ Class type is {h,cpp} however C++ source is {cpp}, setting this would return the C++ source.  Otherwise, we return the first filetype that contains the extension.</param>
        /// <returns></returns>
        public static Filetype ExtensionToFiletype(string extension, bool unique_only)
        {
            List<Filetype> fts = GetPossibleExtensionMappings(extension);
            foreach (Filetype ft in fts)
            {
                List<string> exts = ExtensionList.Get(ft);
                if (unique_only)
                {
                    if (exts.Count == 1)
                    {
                        return ft;
                    }
                }
                else
                {
                    return ft;
                }
            }
            return Filetype.None;
        }
        public static List<string> AttsToList(string atts)
        {
            List<string> ret = new List<string>() { ".error" };
            if (atts != null)
            {
                ret = atts.Split(',').ToList();

                //Clear empty extensions.
                for (int i = ret.Count - 1; i >= 0; i--)
                {
                    if (ret[i].Trim().Length == 0)
                    {
                        ret.RemoveAt(i);
                    }
                }
            }
            return ret;
        }
        public static List<string> Get(object enm)
        {
            if (enm != null)
            {
                MemberInfo[] mi = enm.GetType().GetMember(enm.ToString());
                if (mi != null && mi.Length > 0)
                {
                    ExtensionList attr = Attribute.GetCustomAttribute(mi[0],
                        typeof(ExtensionList)) as ExtensionList;
                    if (attr != null)
                    {
                        return ExtensionList.AttsToList(attr._atts);

                    }
                }
            }
            return null;
        }
    }

    public enum Filetype
    {
        [Description("None")]
        [ExtensionList("")]
        None,
        [Description("Java")]
        [ExtensionList(".java")]
        Java,
        [Description("C++ Class")]
        [ExtensionList(".h,.cpp")]
        CPP_Class,
        [Description("C++ Header")]
        [ExtensionList(".h")]
        CPP_Header,
        [Description("C++ Source")]
        [ExtensionList(".cpp")]
        CPP_Source,
    }

    public partial class MainWindow : Window
    {
        private List<TextBox> _dataControls;
        private double _optionsHeight = 0;
        private KeyboardHook _hook = null; //Literally, one of the best gems in a while.
        private bool _bEditedNamespace = false;
        private Filetype _eUserInputType = Filetype.None;
        private static string DefaultSettingsFileName = "DefaultSettings.json";

        public MainWindow()
        {
            InitializeComponent();

            Globals.MainWindow = this;
            Globals.About = new About();

            _hook = new KeyboardHook();
            _hook.KeyDown += new KeyboardHook.HookEventHandler(OnHookKeyDown);

            this.Title = Globals.GetTitle() + " v" + Globals.GetVersion();
        }

        #region Public: Methods

        public void SetStatus(string x)
        {
            _lblStatus.Content = x;
        }
        #endregion

        #region UI Callbacks
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            NewConfig();
            LoadDefaultSettings();
            _txtFilename.Text = "";
            Keyboard.Focus(_txtFilename);
            UpdateFilenameHighlight();
        }
        private void _grpOptions_Expanded1(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }
        private void About_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            Globals.About.Show();
        }
        private void _btnGenerate_Click(object sender, RoutedEventArgs e)
        {
            Generate();
        }
        private void _txtSpaces_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !new Regex("[^0-9.-]+").IsMatch(e.Text);
        }
        private void _btnDefaultLicense_Click(object sender, RoutedEventArgs e)
        {
            _txtLicense.Text = GetBSDLicense();
        }
        private void _txtFilename_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateFilenamePreview();
            UserInputFiletype();
            UpdateFilenameHighlight();
        }
        private void _cboFileType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Filetype ft = GetSelectedFiletype();
            if (ft == Filetype.Java)
            {
                if (_bEditedNamespace == false)
                {
                    _chkNamespace.Content = "Package";
                    _txtNamespace.Text = "My.Package";
                }
                _grpOptions.IsEnabled = true;
            }
            else if (ft == Filetype.CPP_Class || ft == Filetype.CPP_Header || ft == Filetype.CPP_Source)
            {
                if (_bEditedNamespace == false)
                {
                    _chkNamespace.Content = "Namespace";
                    _txtNamespace.Text = "MyNamespace";
                }
                _grpOptions.IsEnabled = true;
            }
            _grpOptions.IsEnabled = (ft != Filetype.None);

            _btnGenerate.IsEnabled = _grpOptions.IsEnabled;

            UpdateFilenamePreview();
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
        private void _chkLicense_Checked(object sender, RoutedEventArgs e)
        {
            DisableHide(_txtLicense, _chkLicense);
        }
        private void _chkAuthor_Checked(object sender, RoutedEventArgs e)
        {
            DisableHide(_txtAuthor, _chkAuthor);
        }
        private void _chkCopyright_Checked(object sender, RoutedEventArgs e)
        {
            DisableHide(_txtCopyright, _chkCopyright);
        }
        private void _chkNamespace_Checked(object sender, RoutedEventArgs e)
        {
            DisableHide(_txtNamespace, _chkNamespace);
        }
        private void _chkBaseClass_Copy_Checked(object sender, RoutedEventArgs e)
        {
            DisableHide(_txtBaseClass, _chkBaseClass);
        }
        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
        private void _grpOptions_Expanded(object sender, RoutedEventArgs e)
        {
            ExpandOrContractOptionsArea();
        }
        private void Window_Closed(object sender, EventArgs e)
        {
            //Wasn't exiting..
            System.Windows.Application.Current.Shutdown();
        }
        private void _New_Click(object sender, RoutedEventArgs e)
        {
            NewConfig();
        }
        private void _Save_Click(object sender, RoutedEventArgs e)
        {
            SaveConfig();
        }
        private void _Load_Click(object sender, RoutedEventArgs e)
        {
            LoadConfig();
        }
        private void _btnEditLicense_Click(object sender, RoutedEventArgs e)
        {
            TextEditor te = new TextEditor();
            te.Control = _txtLicense;
            te.ShowDialog();
        }
        private void _chkUseTabs_Checked(object sender, RoutedEventArgs e)
        {
            _txtSpaces.IsEnabled = (_chkUseTabs.IsChecked == true);
        }
        private void _txtNamespace_KeyDown(object sender, KeyEventArgs e)
        {
            //This is a little gimmick to show the preview.
            _bEditedNamespace = true;
        }
        private void _chkGenerateDoc_Checked(object sender, RoutedEventArgs e)
        {
            bool b = (_chkGenerateDoc.IsChecked == true);
            _chkAuthor.IsChecked = _chkAuthor.IsEnabled = b;
            _chkCopyright.IsChecked = _chkCopyright.IsEnabled = b;
            _chkLicense.IsChecked = _chkLicense.IsEnabled = b;
            _chkDateTime.IsChecked = _chkDateTime.IsEnabled = b;
            //_chk.IsEnabled = _chkAuthor.IsChecked = (_chkGenerateDoc.IsChecked == true);
        }
        private void _chkPromptToSave_Checked(object sender, RoutedEventArgs e)
        {
            _txtDefaultDirectory.IsEnabled = (_chkPromptToSave.IsChecked == false);
            UpdateFilenamePreview();
        }
        private void _chkDefaultSettings_Click(object sender, RoutedEventArgs e)
        {
            if (_chkDefaultSettings.IsChecked == true)
            {
                SetDefaultSettings();
            }
            else
            {
                ClearDefaultSettings();
            }
        }
        #endregion

        #region Private:Methods
        private static string GetSettingsFilePath()
        {
            string ad = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string path = System.IO.Path.Combine(ad, "Sourcegen");
            path = System.IO.Path.Combine(path, DefaultSettingsFileName);
            return path;
        }
        private void LoadDefaultSettings()
        {
            try
            {
                string st = GetSettingsFilePath();
                if (System.IO.File.Exists(st))
                {
                    LoadConfigFromFile(st);
                    _chkDefaultSettings.IsChecked = true;
                }
            }
            catch (Exception ex)
            {
                Globals.LogError(ex.ToString());
                _chkDefaultSettings.IsChecked = false;
            }
        }
        private void SetDefaultSettings()
        {
            try
            {
                string st = GetSettingsFilePath();
                SaveConfigToFile(st);
                _chkDefaultSettings.IsChecked = true;
            }
            catch (Exception ex)
            {
                Globals.LogError(ex.ToString());
            }
        }
        private void ClearDefaultSettings()
        {
            try
            {
                string st = GetSettingsFilePath();
                if (System.IO.File.Exists(st))
                {
                    MessageBoxResult mr = MessageBox.Show("You are about to delete '" + st + "', continue?", "Warning", MessageBoxButton.OKCancel, MessageBoxImage.Exclamation);
                    if (mr == MessageBoxResult.OK)
                    {
                        System.IO.File.Delete(st);
                        SetStatus("Deleted " + st);
                        _chkDefaultSettings.IsChecked = false;
                    }
                    else
                    {
                        _chkDefaultSettings.IsChecked = true;
                    }
                }
            }
            catch (Exception ex)
            {
                Globals.LogError(ex.ToString());
            }
        }
        private void OnHookKeyDown(object sender, HookEventArgs e)
        {
            UInt32 key = e.key;
            if (key == 13)
            {
                Generate();
            }
        }
        private void NewConfig()
        {
            _txtFilenamePreview.Text = "";
            _dataControls = new List<TextBox>() {
                _txtLicense,
                _txtAuthor,
                _txtCopyright,
                _txtNamespace,
                _txtBaseClass};

            foreach (TextBox t in _dataControls)
            {
                DisableHide(t, null);
            }
            _grpOptions.IsEnabled = true;
            _grpOptions.IsExpanded = true;

            _grpOptions.IsExpanded = false;
            _grpOptions.IsEnabled = false;

            _txtLicense.Text = GetBSDLicense();

            //Populate listbox
            foreach (Filetype v in Enum.GetValues(typeof(Filetype)).Cast<Filetype>())
            {
                string desc = Globals.GetEnumDescription((Filetype)v);
                ListBoxItem lbi = new ListBoxItem();
                lbi.Content = desc;
                lbi.Tag = v;
                _cboFileType.Items.Add(lbi);
            }
        }
        private void SaveConfig()
        {
            try
            {
                string name = "config.json";
                string ext = System.IO.Path.GetExtension(name);
                Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
                dlg.FileName = name;
                dlg.DefaultExt = ext;
                dlg.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

                dlg.Filter = "Text documents (" + ext + ")|*" + ext;
                bool continueSave = (dlg.ShowDialog() == true);

                if (continueSave)
                {
                    SaveConfigToFile(dlg.FileName);
                }
            }
            catch (Exception ex)
            {
                Globals.LogError("Exception Saving: " + ex.ToString());
            }
        }
        private void SaveConfigToFile(string fullPath)
        {
            Settings set = new Settings();
            set.Filetype = GetSelectedFiletype();

            set._bComments = _chkGenerateDoc.IsChecked == true;
            set._bLicense = _chkLicense.IsChecked == true;
            set._strLicense = _txtLicense.Text;
            set._bAuthor = _chkAuthor.IsChecked == true;
            set._strAuthor = _txtAuthor.Text;
            set._bCopyright = _chkCopyright.IsChecked == true;
            set._strCopyright = _txtCopyright.Text;
            set._bDate = _chkDateTime.IsChecked == true;

            set._bBaseClass = _chkBaseClass.IsChecked == true;
            set._strBaseClass = _txtBaseClass.Text;
            set._bNamespace = _chkNamespace.IsChecked == true;
            set._strNamespace = _txtNamespace.Text;
            set._bTabs = _chkUseTabs.IsChecked == true;
            set._iIndent = GetIndentSize();

            set._bCloseAfterGenerating = _chkCloseAfterGenerate.IsChecked == true;
            set._bPromptSave = _chkPromptToSave.IsChecked == true;

            set._strDefaultSaveLocation = _txtDefaultDirectory.Text;

            set.SaveAs(fullPath, true);
        }
        private void LoadConfig()
        {
            try
            {
                string name = "config.json";
                string ext = System.IO.Path.GetExtension(name);
                Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
                dlg.FileName = name;
                dlg.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                dlg.DefaultExt = ext;
                dlg.Filter = "Text documents (" + ext + ")|*" + ext;
                bool continueLoad = (dlg.ShowDialog() == true);

                if (continueLoad)
                {
                    LoadConfigFromFile(dlg.FileName);
                }
            }
            catch (Exception ex)
            {
                Globals.LogError("Exception Loading: " + ex.ToString());
            }
        }
        private void LoadConfigFromFile(string file)
        {
            Settings setc = new Settings();
            Settings set = setc.Load(file) as Settings;

            if (set != null)
            {
                SelectFileType(set.Filetype);
                _chkGenerateDoc.IsChecked = set._bComments;
                _chkLicense.IsChecked = set._bLicense;
                _txtLicense.Text = set._strLicense;
                _chkAuthor.IsChecked = set._bAuthor;
                _txtAuthor.Text = set._strAuthor;
                _chkCopyright.IsChecked = set._bCopyright;
                _txtCopyright.Text = set._strCopyright;
                _chkDateTime.IsChecked = set._bDate;

                _chkBaseClass.IsChecked = set._bBaseClass;
                _txtBaseClass.Text = set._strBaseClass;
                _chkNamespace.IsChecked = set._bNamespace;
                _txtNamespace.Text = set._strNamespace;
                _chkUseTabs.IsChecked = set._bTabs;
                _txtSpaces.Text = set._iIndent.ToString();

                _chkCloseAfterGenerate.IsChecked = set._bCloseAfterGenerating;
                _chkPromptToSave.IsChecked = set._bPromptSave;

                _txtDefaultDirectory.Text = set._strDefaultSaveLocation;

                _bEditedNamespace = true;   //Prevent default namespace from beging replaced
            }
            else
            {
                throw new Exception("Failed to load, could not cast the JSON to a Settings class.");
            }
        }
        private double GetToggleHeight()
        {
            //Doesn't work.
            //if (VisualTreeHelper.GetChildrenCount(_grpOptions) > 0)
            //{
            //    var border = VisualTreeHelper.GetChild(_grpOptions, 0);
            //    var dockpanel = VisualTreeHelper.GetChild(border, 0);
            //    var togglebutton = VisualTreeHelper.GetChild(dockpanel, 0); // it may be not 0th, so please enumerate all children using VisualTreeHelper.GetChildrenCount(dockpanel) and find that ToggleButton
            //    if ((togglebutton as Button) != null)
            //    {
            //        return (togglebutton as Button).ActualHeight;
            //    }
            //}
            return 30;
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
        private string Indent(string x)
        {
            string y = x.Replace("$", GetIndent());
            return y;
        }
        private int GetIndentSize()
        {
            int n = 0;
            if (!Int32.TryParse(_txtSpaces.Text, out n))
            {
                Globals.Log("Error parsing spaces number.");
            }
            return n;
        }
        private string GetIndent()
        {
            string ret = "";
            if (_chkUseTabs.IsChecked == true)
            {
                ret = new string('\t', GetIndentSize());
            }
            else
            {
                ret = new string(' ', GetIndentSize());
            }
            return ret;
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
            if (_chkGenerateDoc.IsChecked == true)
            {
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
            }
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
                head += "namespace " + GetNamespace() + " {\n";
            }
            //Class doc
            if (_chkGenerateDoc.IsChecked == true)
            {
                head += "/**\n";
                head += "*$@class ";
                head += GetFileName();
                head += "\n";
                head += "*$@brief\n";
                head += "*\n";
                head += "*/\n";
            }
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
                head += "}//ns " + GetNamespace() + "\n";
            }
            head += "\n";
            head += "#endif\n";

            return head;
        }
        private string GetCPPSource()
        {
            string head = "";
            head += "#include \"./" + GetFileName() + ".h\"\n";
            head += "\n";
            head += "\n";
            if (_chkNamespace.IsChecked == true)
            {
                head += "namespace " + GetNamespace() + " {\n";
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
                head += "}//ns " + GetNamespace() + "\n";
            }
            return head;
        }
        private string GetJavaSource()
        {
            string head = "";

            if (_chkGenerateDoc.IsChecked == true)
            {
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
            }

            if (_chkNamespace.IsChecked == true)
            {
                head += "package " + GetNamespace() + ";\n";
            }
            if (_chkGenerateDoc.IsChecked == true)
            {
                head += "//import\n";
                head += "\n";
                head += "/**\n";
                head += "*$@class " + GetFileName() + "\n";
                head += "*$@brief\n";
                head += "*/\n";
            }
            head += "public class " + GetFileName() + (_chkBaseClass.IsChecked == true ? (" extends " + _txtBaseClass.Text + " ") : "") + " {\n";
            head += " \n";
            head += "}\n";

            return head;
        }
        private void WriteJavaSource()
        {
            string to_write = Indent(GetJavaSource());

            string fn = GetFileName();
            if (CheckAddExts(fn))
            {
                fn = fn + ".java";
            }
            WriteSourceFile(fn, GetFileDirectory(), to_write);
        }
        private void WriteCPPHeader()
        {
            string to_write = Indent(getHeaderCommentBlock() + getHeaderBody());
            string fn = GetFileName();
            if (CheckAddExts(fn))
            {
                fn = fn + ".h";
            }
            WriteSourceFile(fn, GetFileDirectory(), to_write);
        }
        private void WriteCPPSource()
        {
            string to_write = Indent(GetCPPSource());
            string fn = GetFileName();
            if (CheckAddExts(fn))
            {
                fn = fn + ".cpp";
            }
            WriteSourceFile(fn, GetFileDirectory(), to_write);
        }
        private string GetFileDirectory()
        {
            string dir = _txtDefaultDirectory.Text;
            return dir;
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
        private void Generate()
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

            if (_chkCloseAfterGenerate.IsChecked == true)
            {
                Close();
            }
        }
        private void WriteSourceFile(string filename, string directory, string data)
        {
            try
            {
                bool continueSave = false;
                string selected_fname = "";
                if (_chkPromptToSave.IsChecked == true)
                {
                    string ext = System.IO.Path.GetExtension(filename);
                    Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
                    dlg.FileName = filename; // Default file name
                    dlg.InitialDirectory = directory;
                    dlg.DefaultExt = ext; // Default file extension
                    dlg.Filter = "Text documents (" + ext + ")|*" + ext; // Filter files by extension

                    // Show save file dialog box
                    continueSave = (dlg.ShowDialog() == true);
                    if (continueSave)
                    {
                        selected_fname = dlg.FileName;
                    }
                }
                else
                {
                    selected_fname = System.IO.Path.Combine(directory, filename);
                    Globals.Log("Generated Filename = " + selected_fname);
                    if (System.IO.File.Exists(selected_fname) == false)
                    {
                        //We could add to "overwrite file" here, if we want.
                        continueSave = true;
                    }
                    else
                    {
                        Globals.LogError("File '" + filename + "' already exists.");
                    }
                }

                // Process save file dialog box results
                if (continueSave == true)
                {
                    //Try to create file directory
                    string dir = System.IO.Path.GetDirectoryName(selected_fname);
                    try
                    {
                        if (!System.IO.Directory.Exists(dir))
                        {
                            System.IO.Directory.CreateDirectory(dir);
                        }
                    }
                    catch (Exception ex)
                    {
                        Globals.LogError("Could not create directory '" + dir + "': " + ex.ToString());
                    }


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
                Globals.LogError("Error writing file: " + ex.ToString());
            }
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
        private void DisableHide(TextBox tb, CheckBox ch)
        {
            bool bEnabled = (ch == null) ? false : (ch.IsChecked == true);
            tb.IsEnabled = bEnabled;
        }
        private void ExpandOrContractOptionsArea()
        {
            if (_optionsHeight > 0)
            {
                if (_grpOptions.IsExpanded == true)
                {
                    this.Height += _optionsHeight;
                }
                else
                {
                    this.Height -= _optionsHeight;
                }
            }

            //Compute Expanded Height AFTER first expanding the options
            if (_grpOptions.IsExpanded)
            {
                _optionsHeight = _grpOptions.ActualHeight;
                _optionsHeight -= GetToggleHeight();
            }
        }
        private bool CheckAddExts(string filename)
        {
            //Return true if we need to add the filename extension based on the selected type.
            bool addExts = true;
            string fext = System.IO.Path.GetExtension(filename);
            if (String.IsNullOrEmpty(fext) == false)
            {
                string e = fext.Trim().ToLower();
                if (e.Equals(".java") || e.Equals(".cpp") || e.Equals(".h"))
                {
                    addExts = false;
                }
            }
            return addExts;
        }
        private void UpdateFilenamePreview()
        {
            if (_txtFilenamePreview != null)
            {
                string filename = _txtFilename.Text;
                bool addExts = CheckAddExts(filename);
                string add_dir = "";
                if (_chkPromptToSave.IsChecked == false)
                {
                    add_dir = GetFileDirectory();
                }

                if (addExts)
                {
                    _txtFilenamePreview.Text = "";
                    List<string> exts = ExtensionList.Get(GetSelectedFiletype());
                    if (exts != null)
                    {
                        foreach (string ext in exts)
                        {
                            _txtFilenamePreview.Text += System.IO.Path.Combine(add_dir , filename + ext) + "\r\n";
                        }
                    }
                }
                else
                {
                    _txtFilenamePreview.Text = System.IO.Path.Combine(add_dir, filename );
                }
            }
        }
        private void UserInputFiletype()
        {
            string ext = System.IO.Path.GetExtension(_txtFilename.Text);
            Filetype t = ExtensionList.ExtensionToFiletype(ext, true);
            if (t != Filetype.None)
            {
                _eUserInputType = t;
                SelectFileType(t);
            }
        }
        #endregion

        private void _btnEditDefaultDirectory_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new Ookii.Dialogs.Wpf.VistaFolderBrowserDialog();
                if (dialog.ShowDialog(this).GetValueOrDefault())
                {
                    _txtDefaultDirectory.Text = dialog.SelectedPath;
                }
            }
            catch (Exception ex)
            {
                Globals.LogError(ex.ToString());
            }
        }
        Brush _defaultTextboxBorderBrush = null;
        private void UpdateFilenameHighlight()
        {
            if (_defaultTextboxBorderBrush == null)
            {
                _defaultTextboxBorderBrush = _txtFilename.BorderBrush;
            }
            if (String.IsNullOrEmpty(_txtFilename.Text.Trim()))
            {
                _txtFilename.BorderBrush = Brushes.Red;
            }
            else
            {
                _txtFilename.BorderBrush = _defaultTextboxBorderBrush;
            }
        }

        private void _txtDefaultDirectory_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateFilenamePreview();
        }
    }
}
