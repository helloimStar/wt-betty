using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wt_betty.Entities
{
    public class Settings
    {
        private const string PATH = @"\settings.txt"; 
        public static void Save()
        {
            File.WriteAllText(PATH, JsonConvert.SerializeObject(Instance, Formatting.Indented));
        }

        private static Settings Load()
        {
            return JsonConvert.DeserializeObject<Settings>(File.ReadAllText(PATH));
        }

        public static readonly Settings Instance = Load();

        public AircraftProfile Default { get; private set; } = new AircraftProfile() { Name = "Default" };
        public Dictionary<string, AircraftProfile> Profiles { get; private set; } = new Dictionary<string, AircraftProfile>();

        private Settings()
        {

        }
    }
}
