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

    public abstract class VoiceMessageProcessor : IDisposable
    {
        private bool m_Disposed;
        private readonly BlockingCollection<SoundMessage> m_MessagesQueue = new BlockingCollection<SoundMessage>();
        
        private Task m_ProcessingTask;
        private CancellationTokenSource m_CancellationToken;
        private SoundMessage m_Current;

        #region Messages set defining
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
        #endregion

        #region disposing
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!m_Disposed)
            {
                if (disposing)
                {
                    m_MessagesQueue.CompleteAdding();
                    Stop();
                    m_MessagesQueue.Dispose();
                    m_CancellationToken?.Dispose();
                    m_Current?.Dispose();
                    m_ProcessingTask.Dispose();

                    SoundMsgStart?.Dispose();
                    SoundMsgEnd?.Dispose();

                    MsgBingoFuel?.Dispose();
                    MsgAoAMaximum?.Dispose();
                    MsgAoAOverLimit?.Dispose();
                    MsgGMaximum?.Dispose();
                    MsgGOverLimit?.Dispose();
                    MsgPullUp?.Dispose();
                    MsgOverspeed?.Dispose();
                    MsgGearUp?.Dispose();
                    MsgGearDown?.Dispose();
                    MsgSinkRate?.Dispose();
                }
                m_Disposed = true;
            }
        }
        #endregion

        public void Start()
        {
            if (m_ProcessingTask != null && !m_ProcessingTask.IsCompleted)
            {
                m_ProcessingTask.Wait();
                m_ProcessingTask.Dispose();
            }

            m_CancellationToken?.Dispose();
            m_CancellationToken = new CancellationTokenSource();

            m_ProcessingTask = Task.Factory.StartNew(() =>
            {
                while (!m_MessagesQueue.IsAddingCompleted && !m_CancellationToken.IsCancellationRequested)
                {
                    if (m_MessagesQueue.Count() > 0)
                    {
                        bool playInOut = false;
                        foreach (var msgToPlay in m_MessagesQueue.GetConsumingEnumerable(m_CancellationToken.Token))
                        {
                            if (msgToPlay.Actual)
                            {
                                m_Current = msgToPlay;
                                if (!playInOut)
                                    playInOut = msgToPlay.PlayInOut;

                                if (playInOut)
                                    SoundMsgStart?.PlaySync();

                                msgToPlay.Play();
                                m_Current = null;
                            }
                        }
                        if (playInOut)
                            SoundMsgEnd?.PlaySync();
                    }
                    else if (!m_CancellationToken.IsCancellationRequested)
                        Thread.Sleep(100);
                    else
                        return;
                }

            }, m_CancellationToken.Token);
        }

        public void Stop()
        {
            m_Current?.Stop();
            m_CancellationToken.Cancel();
        }

        private void PlayMsg(SoundMessage msg)
        {
            if (msg != null)
            {
                msg.Actual = true;
                if (msg.Equals(m_Current))
                    return;
                m_MessagesQueue.Add(msg);
            }
        }

        private void CancelMsg(SoundMessage msg)
        {
            if (msg != null)
            {
                msg.Actual = false;
                //do not interrupt if is playing now
                //msg.Stop();
            }
        }

        private void ProcessMsg(SoundMessage msg, bool actual)
        {
            if (actual)
                PlayMsg(msg);
            else
                CancelMsg(msg);
        }

        #region Messages
        public void BingoFuel(bool actual = true)  => ProcessMsg(MsgBingoFuel, actual);

        public void AoAMaximum(bool actual = true)
        {
            CancelMsg(MsgAoAOverLimit);
            ProcessMsg(MsgAoAMaximum, actual);
        }

        public void AoAOverLimit(bool actual = true)
        {
            CancelMsg(MsgAoAMaximum);
            ProcessMsg(MsgAoAOverLimit, actual);
        }

        public void GMaximum(bool actual = true)
        {
            CancelMsg(MsgGOverLimit);
            ProcessMsg(MsgGMaximum, actual);
        }

        public void GOverLimit(bool actual = true)
        {
            CancelMsg(MsgGMaximum);
            ProcessMsg(MsgGOverLimit, actual);
        }

        public void PullUp(bool actual = true) => ProcessMsg(MsgPullUp, actual);
        public void Overspeed(bool actual = true) => ProcessMsg(MsgOverspeed, actual);
        public void GearUp(bool actual = true) => ProcessMsg(MsgGearUp, actual);
        public void GearDown(bool actual = true) => ProcessMsg(MsgGearDown, actual);
        public void SinkRate(bool actual = true) => ProcessMsg(MsgSinkRate, actual);
        #endregion

        protected class SoundMessage : IDisposable
        {
            private bool m_Disposed;

            internal SoundPlayer Sound { get; set; }
            internal bool Looped { get; set; }
            internal bool PlayInOut { get; set; } = true;

            //Is this message actual for current flight data or should be ignored in queue
            internal bool Actual { get; set; } = true;

            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            protected virtual void Dispose(bool disposing)
            {
                if (!m_Disposed)
                {
                    if (disposing)
                    {
                        Stop();
                        Sound?.Dispose();
                    }
                    m_Disposed = true;
                }
            }

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
