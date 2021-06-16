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
            SupportedMessages.Add(MsgBingoFuel);

            MsgAoAMaximum = new SoundMessage() { Sound = new SoundPlayer(Properties.Resources.RITA_MaximumAngleOfAttack) };
            SupportedMessages.Add(MsgAoAMaximum);

            MsgAoAOverLimit = new SoundMessage() { Sound = new SoundPlayer(Properties.Resources.RITA_AngleOfAttackOverLimit), Background = true, PlayInOut = false };
            SupportedMessages.Add(MsgAoAOverLimit);

            MsgGMaximum = new SoundMessage() { Sound = new SoundPlayer(Properties.Resources.RITA_GOverLimit) };
            SupportedMessages.Add(MsgGMaximum);

            MsgGOverLimit = new SoundMessage() { Sound = new SoundPlayer(Properties.Resources.RITA_GOverLimit) };
            SupportedMessages.Add(MsgGOverLimit);

            MsgPullUp = new SoundMessage() { Sound = new SoundPlayer(Properties.Resources.RITA_PullUp) };
            SupportedMessages.Add(MsgPullUp);

            MsgOverspeed = new SoundMessage() { Sound = new SoundPlayer(Properties.Resources.RITA_MaximumSpeed) };
            SupportedMessages.Add(MsgOverspeed);

            MsgGearUp = new SoundMessage() { Sound = new SoundPlayer(Properties.Resources.RITA_GearUp) };
            SupportedMessages.Add(MsgGearUp);

            MsgGearDown = new SoundMessage() { Sound = new SoundPlayer(Properties.Resources.RITA_GearDown) };
            SupportedMessages.Add(MsgGearDown);
        }
    }
}
