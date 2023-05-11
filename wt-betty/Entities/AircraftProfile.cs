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
        public bool EnableG { get; set; }
        public int GForce { get; set; }
        public bool EnableAoA { get; set; }
        public int AoA { get; set; }
        public bool EnablePullUp { get; set; }
        public bool EnableFuel { get; set; }
        public bool EnableGear { get; set; }
        public int GearDown { get; set; }
        public int GearUp { get; set; }
        public bool EnableOverSpeed { get; set; }
        public int OverSpeed { get; set; }
        public VoiceTemplate Voice { get; set; }
        public string Monitoring { get; set; }
        
        public AircraftProfile()
        {
            Reset();
        }

        public void Reset()
        {
            EnableG = true;
            GForce = 6;
            EnableAoA = true;
            AoA = 12;
            EnablePullUp = true;
            EnableFuel = true;
            EnableGear = true;
            GearDown = 270;
            GearUp = 290;
            EnableOverSpeed = true;
            OverSpeed = 820;
            Voice = VoiceTemplate.US_Betty;
        }
    }
}
