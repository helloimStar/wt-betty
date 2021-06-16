using System;
using System.Collections.Generic;
using System.Linq;
using System.Media;
using System.Text;
using System.Threading.Tasks;

namespace wt_betty.Entities
{
    public sealed class BettyVoiceProcessor : VoiceProcessor
    {
        public static readonly BettyVoiceProcessor Instance = new BettyVoiceProcessor();

        private BettyVoiceProcessor()
        {
            MsgBingoFuel = new SoundMessage() { Sound = new SoundPlayer(Properties.Resources.Bingo) };
            SupportedMessages.Add(MsgBingoFuel);

            MsgAoAMaximum = new SoundMessage() { Sound = new SoundPlayer(Properties.Resources.MaximumAngleOfAttack), Background = true, PlayInOut = false };
            SupportedMessages.Add(MsgAoAMaximum);

            MsgAoAOverLimit = new SoundMessage() { Sound = new SoundPlayer(Properties.Resources.AngleOfAttackOverLimit), Background = true, PlayInOut = false };
            SupportedMessages.Add(MsgAoAOverLimit);

            MsgGMaximum = new SoundMessage() { Sound = new SoundPlayer(Properties.Resources.OverG) };
            SupportedMessages.Add(MsgGMaximum);

            MsgGOverLimit = new SoundMessage() { Sound = new SoundPlayer(Properties.Resources.GOverLimit), Background = true, PlayInOut = false };
            SupportedMessages.Add(MsgGOverLimit);

            MsgPullUp = new SoundMessage() { Sound = new SoundPlayer(Properties.Resources.PullUp) };
            SupportedMessages.Add(MsgPullUp);

            MsgOverspeed = new SoundMessage() { Sound = new SoundPlayer(Properties.Resources.MaximumSpeed) };
            SupportedMessages.Add(MsgOverspeed);

            MsgGearUp = new SoundMessage() { Sound = new SoundPlayer(Properties.Resources.GearUp) };
            SupportedMessages.Add(MsgGearUp);

            MsgGearDown = new SoundMessage() { Sound = new SoundPlayer(Properties.Resources.GearDown) };
            SupportedMessages.Add(MsgGearDown);

            MsgSinkRate = new SoundMessage() { Sound = new SoundPlayer(Properties.Resources.SinkRate) };
            SupportedMessages.Add(MsgSinkRate);
        }
    }
}
