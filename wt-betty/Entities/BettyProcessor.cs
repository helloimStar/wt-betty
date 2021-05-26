using System;
using System.Collections.Generic;
using System.Linq;
using System.Media;
using System.Text;
using System.Threading.Tasks;

namespace wt_betty.Entities
{
    public sealed class BettyProcessor : VoiceMessageProcessor
    {
        public BettyProcessor()
        {
            MsgBingoFuel = new SoundMessage() { Sound = new SoundPlayer(Properties.Resources.Bingo) };
            MsgAoAMaximum = new SoundMessage() { Sound = new SoundPlayer(Properties.Resources.MaximumAngleOfAttack) };
            MsgAoAOverLimit = new SoundMessage() { Sound = new SoundPlayer(Properties.Resources.AngleOfAttackOverLimit), Looped = true, PlayInOut = false };
            MsgGMaximum = new SoundMessage() { Sound = new SoundPlayer(Properties.Resources.OverG) };
            MsgGOverLimit = new SoundMessage() { Sound = new SoundPlayer(Properties.Resources.GOverLimit), Looped = true, PlayInOut = false };
            MsgPullUp = new SoundMessage() { Sound = new SoundPlayer(Properties.Resources.PullUp) };
            MsgOverspeed = new SoundMessage() { Sound = new SoundPlayer(Properties.Resources.MaximumSpeed) };
            MsgGearUp = new SoundMessage() { Sound = new SoundPlayer(Properties.Resources.GearUp) };
            MsgGearDown = new SoundMessage() { Sound = new SoundPlayer(Properties.Resources.GearDown) };
            MsgSinkRate = new SoundMessage() { Sound = new SoundPlayer(Properties.Resources.SinkRate) };
        }
    }
}
