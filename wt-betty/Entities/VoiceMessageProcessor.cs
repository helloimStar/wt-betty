using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Media;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace wt_betty.Entities
{
    public enum VoiceTemplate
    {
        US_Betty = 0,
        RU_Rita
    }

    public abstract class IVoiceMessageProcessor
    {
        private SoundMessage m_Current;
        private readonly BlockingCollection<SoundMessage> m_MessagesQueue = new BlockingCollection<SoundMessage>();

        protected SoundPlayer SoundMsgStart { get; set; }
        protected SoundPlayer SoundMsgEnd { get; set; }

        protected SoundMessage MsgBingoFuel { get; set; }
        protected SoundMessage MsgAoAMaximum { get; set; }
        protected SoundMessage MsgAoAOverLimit { get; set; }
        protected SoundMessage MsgGMaximum { get; set; }
        protected SoundMessage MsgGOverLimit { get; set; }
        protected SoundMessage MsgPullUp { get; set; }
        protected SoundMessage MsgOverspeed { get; set; }
        protected SoundMessage MsgGearUp { get; set; }
        protected SoundMessage MsgGearDown { get; set; }
        protected SoundMessage MsgSinkRate { get; set; }
        
        private void PlayMsg(SoundMessage msg)
        {
            if (msg == null || (msg.Looped && msg.Equals(m_Current)))
                return;
            m_MessagesQueue.Add(msg);
        }

        private void Behaviour()
        {
            while (!m_MessagesQueue.IsAddingCompleted)
            {
                if (m_MessagesQueue.Count() > 0)
                {
                    bool playInOut = false;
                    foreach (var msgToPlay in m_MessagesQueue.GetConsumingEnumerable())
                    {
                        m_Current = msgToPlay;
                        if (!playInOut)
                            playInOut = msgToPlay.PlayInOut;

                        if (playInOut)
                            SoundMsgStart?.PlaySync();

                        msgToPlay.Play();
                        m_Current = null;
                    }
                    if (playInOut)
                        SoundMsgEnd?.PlaySync();
                }
                else
                    Thread.Sleep(100);
            }
        }

        public void BingoFuel()  => PlayMsg(MsgBingoFuel);

        public void AoAMaximum()
        {
            MsgAoAOverLimit.Stop();
            PlayMsg(MsgAoAMaximum);
        }

        public void AoAOverLimit()
        {
            MsgAoAMaximum.Stop();
            PlayMsg(MsgAoAOverLimit);
        }

        public void GMaximum()
        {
            MsgGOverLimit.Stop();
            PlayMsg(MsgGMaximum);
        }

        public void GOverLimit()
        {
            MsgGMaximum.Stop();
            PlayMsg(MsgGOverLimit);
        }

        public void PullUp() => PlayMsg(MsgPullUp);
        public void Overspeed() => PlayMsg(MsgOverspeed);
        public void GearUp() => PlayMsg(MsgGearUp);
        public void GearDown() => PlayMsg(MsgGearDown);
        public void SinkRate() => PlayMsg(MsgSinkRate);

        protected class SoundMessage
        {
            internal SoundPlayer Sound { get; set; }
            internal bool Looped { get; set; }
            internal bool PlayInOut { get; set; } = true;

            internal long DurationMs { get; }

            public void Play()
            {
                if (Looped)
                    Sound?.PlayLooping();
                else
                    Sound?.PlaySync();
            }

            public void Stop()
            {
                Sound?.Stop();
            }
        }
    }
}
