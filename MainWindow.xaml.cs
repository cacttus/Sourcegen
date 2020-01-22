using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

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
        private KeyboardHook _hook = null; //Literally, one of the best gems in a while.
        private static string DefaultSettingsFileName = "DefaultSettings.json";
        private string c_strHelperText = "Type Files Here";
        private bool _bMouseDown = false;
        private System.Drawing.Point? _vLastMouse = null;

        public MainWindow()
        {
            InitializeComponent();

            Globals.MainWindow = this;
            Globals.About = new About();

            _hook = new KeyboardHook();
            _hook.KeyDown += new KeyboardHook.HookEventHandler((object sender, HookEventArgs e)=> {
                UInt32 key = e.key;
                if (key == 13)//enter
                {
                    Generate();
                }
                else if(key == 27)//esc
                {
                    Close();
                }
            });

            Title = Globals.GetTitle() + " v" + Globals.GetVersion();

            AllowUserToMoveWindowByDragging();

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
            SetFilenameTextboxIndicator();

            Keyboard.Focus(_txtFilename);
        }
        private void About_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            Globals.About.Show();
        }
        private void _txtSpaces_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !new Regex("[^0-9.-]+").IsMatch(e.Text);
        }
        private void _btnDefaultLicense_Click(object sender, RoutedEventArgs e)
        {
            _txtLicense.Text = GetBSDLicense();
        }
        private void _txtFilename_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (_txtFilename != null && _txtFilenamePreview != null)
            {
                //If user deleted last char, then put this text back.
                if (_txtFilename.Text.Length == 1 && _txtFilename.SelectionStart > 0 && e.Key == Key.Back)
                {
                    SetFilenameTextboxIndicator();
                }
                else
                {
                    ClearFilenameTextboxIndicator();
                }
            }
        }
        private void _txtFilename_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_txtFilenamePreview != null)
            {
                UpdateFilenamePreview();
            }
        }
        private void _Configure_Shake(object sender, RoutedEventArgs e)
        {
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
            UpdateFormHeight();
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
        private void _chkGenerateDoc_Checked(object sender, RoutedEventArgs e)
        {
            bool b = (_chkGenerateDoc.IsChecked == true);
            _chkAuthor.IsChecked = _chkAuthor.IsEnabled = b;
            _chkCopyright.IsChecked = _chkCopyright.IsEnabled = b;
            _chkLicense.IsChecked = _chkLicense.IsEnabled = b;
            _chkDateTime.IsChecked = _chkDateTime.IsEnabled = b;
        }
        private void _chkPromptToSave_Checked(object sender, RoutedEventArgs e)
        {
            bool noPrompt = (_chkPromptToSave.IsChecked == false);
            if (_txtDefaultDirectory != null)
            {
                _txtDefaultDirectory.Visibility = ToggleVisibility(noPrompt);
            }
            if (_btnEditDefaultDirectory != null)
            {
                _btnEditDefaultDirectory.Visibility = ToggleVisibility(noPrompt);
            }
            if (_lblPromptSave != null)
            {
                _lblPromptSave.Visibility = ToggleVisibility(noPrompt);
            }
            if (_chkAutoOverwrite != null && _chkPromptOverwrite != null)
            {
                _chkAutoOverwrite.Visibility = ToggleVisibility(noPrompt);
                _chkPromptOverwrite.Visibility = ToggleVisibility(noPrompt && (_chkAutoOverwrite.IsChecked == false));
            }
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
        private void _txtDefaultDirectory_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateFilenamePreview();
        }
        private void _txtFilename_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            ClearFilenameTextboxIndicator();
        }
        private void _txtFilenamePreview_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateFormHeight();
        }
        private void _chkAutoOverwrite_Checked(object sender, RoutedEventArgs e)
        {
            _chkPromptOverwrite.Visibility = ToggleVisibility(_chkAutoOverwrite.IsChecked == false);
        }
        private void _Generate_Click(object sender, RoutedEventArgs e)
        {
            Generate();
        }
        #endregion

        #region Private:Methods
        private void AllowUserToMoveWindowByDragging()
        {
            //Lots of low level windows BS
            MouseHook.HookWindow((WM code) =>
            {
                if (code == WM.MOUSEMOVE || code == WM.NCMOUSEMOVE)
                {
                    if (_bMouseDown)
                    {
                        System.Drawing.Point curMouse = MouseHook.GetMousePosition();
                        if (_vLastMouse != null)
                        {
                            System.Drawing.Point delta = new System.Drawing.Point(
                                _vLastMouse.Value.X - curMouse.X,
                                _vLastMouse.Value.Y - curMouse.Y);
                            Top -= delta.Y;
                            Left -= delta.X;
                        }
                        _vLastMouse = new System.Drawing.Point(curMouse.X, curMouse.Y);
                    }
                }
            });
        }
        private void _MoveWindow_MouseDown(object sender, MouseButtonEventArgs e)
        {
            System.Drawing.Point curMouse = MouseHook.GetMousePosition();
            _vLastMouse = new System.Drawing.Point(curMouse.X, curMouse.Y);
            _bMouseDown = true;
        }
        private void _MoveWindow_MouseUp(object sender, MouseButtonEventArgs e)
        {
            _bMouseDown = false;
        }
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
        private void NewConfig()
        {
            _txtFilenamePreview.Text = "";
            _grpOptions.IsExpanded = true;
            _grpOptions.IsExpanded = false;

            _txtLicense.Text = GetBSDLicense();
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
            //set.Filetype = GetSelectedFiletype();

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
            set._bOpenFolders = _chkOpenContainingFoldersAfterGenerating.IsChecked == true;
            set._strDefaultSaveLocation = _txtDefaultDirectory.Text;

            set._bAutoOverwrite = _chkAutoOverwrite.IsChecked == true;
            set._bPromptOverwrite = _chkPromptOverwrite.IsChecked == true;

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
                _chkOpenContainingFoldersAfterGenerating.IsChecked = set._bOpenFolders;

                _chkAutoOverwrite.IsChecked = set._bAutoOverwrite;
                _chkPromptOverwrite.IsChecked = set._bPromptOverwrite;

                _txtDefaultDirectory.Text = set._strDefaultSaveLocation;
            }
            else
            {
                throw new Exception("Failed to load, could not cast the JSON to a Settings class.");
            }
        }
        private void SetFilenameTextboxIndicator()
        {
            if (_txtFilename != null)
            {
                _txtFilename.Text = c_strHelperText;
                _txtFilename.FontStyle = FontStyles.Italic;
                _txtFilename.Foreground = Brushes.SlateGray;
            }
        }
        private void ClearFilenameTextboxIndicator()
        {
            //Call this after first key is pressed in textbox.
            if (_txtFilename != null)
            {
                if (_txtFilename.FontStyle == FontStyles.Italic)
                {
                    _txtFilename.Text = "";
                    _txtFilename.FontStyle = FontStyles.Normal;
                    _txtFilename.Foreground = Brushes.Black;
                }
            }
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
            string guard = "__" + GetFileName().ToUpper().Replace(".","_").Replace("-", "_").Replace(" ", "_") + "_" + RandomDigits() + "_H__";
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
        private string GetFileDirectory()
        {
            string dir = _txtDefaultDirectory.Text;
            return dir;
        }
        private List<string> GetCleanFileNames()
        {
            //Split strings by quotes
            string text = _txtFilename.Text;
            string[] names = Regex.Matches(text, @"[\""].+?[\""]|[^ ]+")
                .Cast<Match>()
                .Select(m => m.Value)
                .ToArray();
            //Clean up possible errors in the array.
            names = names.Select(x => x.Trim()).ToArray();
            names = names.Select(x => x.Replace(",", "")).ToArray();
            names = names.Select(x => x.Replace("\"", "")).ToArray();
            names = names.Where(x => x.Length > 0).ToArray();

            List<string> namesList = names.ToList();
            return namesList;
        }
        private Filetype GetFileType(string file)
        {
            string ext = System.IO.Path.GetExtension(file);
            Filetype ret = ExtensionList.ExtensionToFiletype(ext, true);
            return ret;
        }
        private List<string> GetFilesToGenerate()
        {
            List<string> files_fullpath = new List<string>();
            if (_chkPromptToSave != null)
            {
                if (!_txtFilename.Text.Equals(c_strHelperText))
                {
                    List<string> files = GetCleanFileNames();

                    //If "prompt to save" is checked, then the SaveFileDialog will handle the location of the file.
                    string add_dir = "";
                    if (_chkPromptToSave.IsChecked == false)
                    {
                        add_dir = GetFileDirectory();
                    }

                    foreach (string file in files)
                    {
                        try
                        {
                            string file_with_dir = "";
                            string specified_dir = System.IO.Path.GetDirectoryName(file);
                            if (specified_dir.Trim().Length > 0)
                            {
                                //User specified a directory specifically.  Don't add the default directory.
                                file_with_dir = file;
                            }
                            else
                            {
                                file_with_dir = System.IO.Path.Combine(add_dir, file);
                            }
                            files_fullpath.Add(file_with_dir);
                        }
                        catch (Exception ex)
                        {
                            Globals.LogError("Error processing file " + file + " =>\r\n " + ex.ToString());
                        }
                    }
                }
            }

            return files_fullpath;
        }
        private void Generate()
        {
            List<string> files = GetFilesToGenerate();

            foreach (string file in files)
            {
                Filetype ft = GetFileType(file);
                if (ft == Filetype.Java)
                {
                    string to_write = Indent(GetJavaSource());
                    WriteSourceFile(file, to_write);
                }
                else if (ft == Filetype.CPP_Class)
                {
                    WriteSourceFile(file, Indent(getHeaderCommentBlock() + getHeaderBody()));
                    WriteSourceFile(file, Indent(GetCPPSource()));
                }
                else if (ft == Filetype.CPP_Header)
                {
                    WriteSourceFile(file, Indent(getHeaderCommentBlock() + getHeaderBody()));
                }
                else if (ft == Filetype.CPP_Source)
                {
                    WriteSourceFile(file, Indent(GetCPPSource()));
                }
            }

            if (_chkOpenContainingFoldersAfterGenerating.IsChecked == true)
            {
                OpenGeneratedFileFolders();
            }

            _lstGeneratedFiles.Clear();

            if (_chkCloseAfterGenerate.IsChecked == true)
            {
                Close();
            }
        }
        private void OpenGeneratedFileFolders()
        {
            //De-dupe paths
            List<string> uniquePaths = new List<string>();
            List<string> uniqueFiles = new List<string>();
            foreach (string file in _lstGeneratedFiles)
            {
                string path = System.IO.Path.GetDirectoryName(file).Trim();
                if (!uniquePaths.Contains(path))
                {
                    uniquePaths.Add(path);
                    uniqueFiles.Add(file);
                }
            }
            //Open all unique folders.
            foreach (string file in uniqueFiles)
            {
                try
                {
                    //https://stackoverflow.com/questions/334630/opening-a-folder-in-explorer-and-selecting-a-file
                    //string path = System.IO.Path.GetDirectoryName(file);
                    string file_mod = file;
                    file_mod = file_mod.Replace('/', '\\');
                    string argument = "/select, \"" + file_mod + "\"";
                    System.Diagnostics.Process.Start("explorer.exe", argument);
                }

                catch (Exception ex)
                {
                    Globals.LogError(ex.ToString());
                }
            }
        }
        private List<string> _lstGeneratedFiles = new List<string>();
        private void WriteSourceFile(string filename_full, string data)
        {
            try
            {
                bool continueSave = false;
                string selected_fname = "";
                if (_chkPromptToSave.IsChecked == true)
                {
                    string directory = System.IO.Path.GetDirectoryName(filename_full);
                    string filename = System.IO.Path.GetFileName(filename_full);

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
                    selected_fname = filename_full;
                    if (_chkAutoOverwrite.IsChecked == false)
                    {
                        if (System.IO.File.Exists(filename_full) == false)
                        {
                            //We could add to "overwrite file" here, if we want.
                            continueSave = true;
                        }
                        else
                        {
                            if (_chkPromptOverwrite.IsChecked == true)
                            {
                                MessageBoxResult mr = MessageBox.Show("Overwrite '" + selected_fname + "'?", "Confirm Overwrite",
                                    MessageBoxButton.YesNo, MessageBoxImage.Exclamation);
                                if (mr == MessageBoxResult.Yes)
                                {
                                    continueSave = true;
                                }
                            }
                            else
                            {
                                Globals.LogError("File '" + filename_full + "' already exists.");
                            }

                        }
                    }
                    else
                    {
                        Globals.Log("Overwriting '" + filename_full + "'.");
                        continueSave = true;
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
                    _lstGeneratedFiles.Add(selected_fname);
                    Globals.Log("Generated Filename = " + selected_fname);
                }
            }
            catch (Exception ex)
            {
                Globals.LogError("Error writing file: " + ex.ToString());
            }
        }
        private void DisableHide(TextBox tb, CheckBox ch)
        {
            bool bEnabled = (ch == null) ? false : (ch.IsChecked == true);
            tb.IsEnabled = bEnabled;
        }
        private double GetExpanderTitleHeight(Expander exp)
        {
            return 45;//yep
        }
        private void UpdateFormHeight()
        {
            _grpOptions.Margin = new Thickness(
            _grpOptions.Margin.Left,
            _txtFilenamePreview.Margin.Top + _txtFilenamePreview.ActualHeight,
            _grpOptions.Margin.Right,
            _grpOptions.Margin.Bottom);

            //Probably not the Best way to do this, but for now, it works.
            List<Control> controls = new List<Control> {
                _mnuMenu,_txtFilename,_txtFilenamePreview, _grpOptions,_statusBar
            };
            double height = 0;
            foreach (Control child in controls)
            {
                if (child is Expander)
                {
                    if (!(child as Expander).IsExpanded)
                    {
                        height += GetExpanderTitleHeight(_grpOptions);
                    }
                    else
                    {
                        height += _grpOptions.ActualHeight + 15;
                    }
                }
                else
                {
                    var prop = child.GetType().GetProperty("ActualHeight");
                    if (prop != null)
                    {
                        object b = prop.GetValue(child, null);
                        if (b != null && b is double)
                        {
                            height += (double)b;
                        }
                    }
                }

            }

            Height = height;
        }
        private void UpdateFilenamePreview()
        {
            //Reset the height, then catch the height reset in SizeChanged
            if (_txtFilenamePreview != null)
            {
                List<string> files = GetFilesToGenerate();
                if (files != null && files.Count > 0)
                {
                    _txtFilenamePreview.Text = string.Join("\r\n", files);
                }
                else
                {
                    _txtFilenamePreview.Text = "";
                }
                _txtFilenamePreview.Height = GetFilenamePreviewBoxHeight() + 10;
                //Form height gets updated when the SizeChanged event gets fired.
            }
        }
        private Size MeasureString(TextBox tb)
        {
            var formattedText = new FormattedText(
                tb.Text,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface(tb.FontFamily, tb.FontStyle, tb.FontWeight, tb.FontStretch),
                tb.FontSize,
                Brushes.Black,
                new NumberSubstitution(),
                1);

            return new Size(formattedText.Width, formattedText.Height);
        }
        private double GetFilenamePreviewBoxHeight()
        {
            double ret = 0;
            if (!_txtFilename.Text.Equals(c_strHelperText))
            {
                if (_txtFilename.Text.Trim().Length > 0)
                {
                    ret += (_txtFilenamePreview.Text.Count(x => x == '\n') + 1) * MeasureString(_txtFilename).Height;//.FontSize;
                }
            }
            return ret;
        }
        private Visibility ToggleVisibility(bool b)
        {
            if (b)
            {
                return Visibility.Visible;
            }
            else
            {
                return Visibility.Hidden;
            }
        }
        #endregion

    }
}
