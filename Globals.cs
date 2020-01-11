using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.ComponentModel;

namespace sourcegen
{
    public static class Globals
    {
        private static string _log = "";
        public static MainWindow MainWindow = null;
        public static About About = null;

        public static void LogError(string x)
        {
            Log("Error:" + x);
        }
        public static string GetTitle()
        {
            string st = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;
            return st;
        }
        public static string GetVersion()
        {
            Version version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            DateTime buildDate = new DateTime(2000, 1, 1)
                                    .AddDays(version.Build).AddSeconds(version.Revision * 2);
            string displayableVersion = $"{version} ({buildDate})";
            return displayableVersion;
        }
        public static void Log(string x)
        {
            _log += (x + Environment.NewLine);
            if (About != null)
            {
                About.SetLog(_log);
            }
            MainWindow.SetStatus(x);
        }
        public static string TimeSpanToString(TimeSpan t)
        {
            string shortForm = "";
            if (t.Hours > 0)
            {
                shortForm += string.Format("{0}h", t.Hours.ToString());
            }
            if (t.Minutes > 0)
            {
                shortForm += string.Format("{0}m", t.Minutes.ToString());
            }
            if (t.Seconds > 0)
            {
                shortForm += string.Format("{0}s", t.Seconds.ToString());
            }
            if (t.Milliseconds > 0)
            {
                shortForm += string.Format("{0}ms", t.Milliseconds.ToString());
            }
            return shortForm;
        }
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

    }
}
