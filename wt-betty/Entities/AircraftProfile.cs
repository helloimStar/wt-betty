using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wt_betty.Entities
{
    public class AircraftProfile
    {
        public string Name { get; set; }

        public bool EnableG { get; set; } = true;
        public int GForce { get; set; } = 6;

        public bool EnableAoA { get; set; } = true;
        public int AoA { get; set; } = 12;

        public bool EnablePullUp { get; set; } = true;

        public bool EnableFuel { get; set; } = true;

        public bool EnableGear { get; set; } = true;
        public int GearDown { get; set; } = 270;
        public int GearUp { get; set; } = 290;

        public bool EnableOverSpeed { get; set; } = true;
        public int OverSpeed { get; set; } = 820;

        public VoiceTemplate Voice { get; set; } = VoiceTemplate.US_Betty;
    }
}
