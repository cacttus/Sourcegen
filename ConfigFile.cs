using System;
using Newtonsoft.Json;
using System.IO;

namespace sourcegen
{
    public abstract class OutFile
    {
        [JsonIgnore]
        public string LoadedOrSavedFileName { get; set; } = "";

        public OutFile()
        {

        }
        public void Save()
        {
            SaveAs(LoadedOrSavedFileName, true);
        }

        public virtual void PostLoad()
        {
            //Override to post load stuff
        }

        public abstract void SaveAs(string loc, bool createDirectory);
        public abstract object Load(string loc);

        public object CreateOrLoad(string loc, bool createDirectory)
        {
            if (!System.IO.File.Exists(loc))
            {
                SaveAs(loc, createDirectory);
            }

            return Load(loc);
        }
    }

    public class ConfigFile<T> : OutFile where T : ConfigFile<T>
    {
        public override void SaveAs(string loc, bool createDirectory)
        {
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            {
                LoadedOrSavedFileName = loc;

                string jsonLoc = Path.Combine(Path.GetDirectoryName(loc), Path.GetFileNameWithoutExtension(loc) + ".json");

                if (createDirectory)
                {
                    string dir = Path.GetDirectoryName(jsonLoc);
                    if (!System.IO.Directory.Exists(dir))
                    {
                        System.IO.Directory.CreateDirectory(dir);
                    }
                }

                try
                {
                    string output = JsonConvert.SerializeObject(this as T);
                    File.WriteAllText(jsonLoc, output);
                }
                catch (Exception ex)
                {
                    Globals.LogError("Failed to save Json File: " + ex.ToString());
                }
            }
            sw.Stop();
            Globals.MainWindow.SetStatus("Saved '" + loc + "' in " + Globals.TimeSpanToString(sw.Elapsed));
        }

        public override object Load(string loc)
        {
            T ret = null;
            try
            {
                System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
                sw.Start();
                {
                    string text = System.IO.File.ReadAllText(loc);
                    ret = JsonConvert.DeserializeObject<T>(text);
                    ret.LoadedOrSavedFileName = loc;

                    ret.PostLoad();
                }
                sw.Stop();
                Globals.MainWindow.SetStatus("Loaded '" + loc + "' in " + Globals.TimeSpanToString(sw.Elapsed));
            }
            catch (Exception ex)
            {
                Globals.LogError("Failed to save Json File: " + ex.ToString());
            }
            return ret;
        }

    }

    public class Settings : ConfigFile<Settings>
    {
        public Filetype Filetype = Filetype.CPP_Class;
         
        public bool _bComments = true;
        public bool _bLicense = true;
        public string _strLicense = "BSD License";
        public bool _bAuthor = true;
        public string _strAuthor = "Me";
        public bool _bCopyright = true;
        public string _strCopyright = "Copyright (c) Me";
        public bool _bDate = true;
         
        public bool _bBaseClass = true;
        public string _strBaseClass = "MyBaseClass";
        public bool _bNamespace = true;
        public string _strNamespace = "MyNamespace";
        public bool _bTabs = true;
        public int _iIndent = 2;

        public bool _bCloseAfterGenerating = true;
        public bool _bPromptSave = true;

        public string _strDefaultSaveLocation = "";
    }















}
