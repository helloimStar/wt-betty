using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace wt_betty.Entities
{
    public class Settings
    {
        private static string PATH_FILENAME = @"settings.txt";
        public static readonly string PATH = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, PATH_FILENAME);
        public static void Save()
        {
            File.WriteAllText(PATH, JsonConvert.SerializeObject(Instance, Formatting.Indented));
        }

        public static void Load()
        {
            string json = File.ReadAllText(PATH);
            JsonConvert.PopulateObject(json, Instance);
        }

        public static readonly Settings Instance = new Settings();
        public AircraftProfile Default { get; private set; } = new AircraftProfile() { Name = "Default" };
        public SortedDictionary<string, AircraftProfile> Profiles { get; private set; } = new SortedDictionary<string, AircraftProfile>();

        private Settings()
        {

        }
    }
}
