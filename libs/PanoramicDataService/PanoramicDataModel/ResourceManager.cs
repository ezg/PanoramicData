using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Path = System.IO.Path;

namespace PanoramicDataModel
{
    public class ResourceManager
    {
        private const string _fileName = @"panoramic_data_resources.res";
        private static readonly Dictionary<string, string> _resources = new Dictionary<string, string>();

        static ResourceManager()
        {
            if (File.Exists(_fileName))
            {
                string[] lines = File.ReadAllLines(_fileName);
                foreach (var line in lines)
                {
                    string[] entries = line.Split(new string[] { "\t" }, StringSplitOptions.RemoveEmptyEntries);
                    _resources.Add(entries[0], entries[1]);
                }
            }
        }

        public static void Add(string key, object value)
        {
            if (_resources.ContainsKey(key))
            {
                _resources[key] = value.ToString();
            }
            else
            {
                _resources.Add(key, value.ToString());
            }
        }

        public static double? GetDouble(string key)
        {
            if (_resources.ContainsKey(key))
            {
                return double.Parse(_resources[key]);
            }
            return null;
        }

        public static bool? GetBool(string key)
        {
            if (_resources.ContainsKey(key))
            {
                return bool.Parse(_resources[key]);
            }
            return null;
        }

        public static string GetString(string key)
        {
            if (_resources.ContainsKey(key))
            {
                return _resources[key];
            }
            return null;
        }

        public static void WriteToFile()
        {
            StringBuilder sb = new StringBuilder();
            foreach (var key in _resources.Keys)
            {
                sb.Append(key + "\t" + _resources[key] + "\n");
            }
            File.WriteAllText(_fileName, sb.ToString());
        }
    }
}
