using System;
using System.Collections.Generic;
using System.Linq;
using System.Media;
using System.Text;
using System.Threading.Tasks;

namespace wt_betty.Entities
{
    public sealed class RitaVoiceProcessor : VoiceProcessor
    {
        public static readonly RitaVoiceProcessor Instance = new RitaVoiceProcessor();

        private RitaVoiceProcessor()
        {
            MsgBingoFuel = new SoundMessage() { Sound = new SoundPlayer(Properties.Resources.RITA_Bingo) };
            MsgAoAMaximum = new SoundMessage() { Sound = new SoundPlayer(Properties.Resources.RITA_MaximumAngleOfAttack) };
            MsgAoAOverLimit = new SoundMessage() { Sound = new SoundPlayer(Properties.Resources.RITA_AngleOfAttackOverLimit), Looped = true, PlayInOut = false };
            MsgGMaximum = new SoundMessage() { Sound = new SoundPlayer(Properties.Resources.RITA_GOverLimit) };
            MsgGOverLimit = new SoundMessage() { Sound = new SoundPlayer(Properties.Resources.RITA_GOverLimit), Looped = true, PlayInOut = false };
            MsgPullUp = new SoundMessage() { Sound = new SoundPlayer(Properties.Resources.RITA_PullUp) };
            MsgOverspeed = new SoundMessage() { Sound = new SoundPlayer(Properties.Resources.RITA_MaximumSpeed) };
            MsgGearUp = new SoundMessage() { Sound = new SoundPlayer(Properties.Resources.RITA_GearUp) };
            MsgGearDown = new SoundMessage() { Sound = new SoundPlayer(Properties.Resources.RITA_GearDown) };
        }
    }
}
