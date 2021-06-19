using SharpDX.XAudio2;
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
    public abstract class VoiceProcessor : IDisposable
    {

        private bool m_Disposed;
        private readonly MessagesQueue m_MessagesQueue;
        private readonly object m_StartStopMonitor = new object();

        private readonly XAudio2 m_Device;
        private readonly MasteringVoice m_MasteringVoice;

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

        protected List<SoundMessage> SupportedMessages { get; private set; } = new List<SoundMessage>();
        #endregion

        protected VoiceProcessor()
        {
            m_MessagesQueue = new MessagesQueue();
            m_Device = new XAudio2();
            m_MasteringVoice = new MasteringVoice(m_Device);
        }
        
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
                    Stop();
                    m_CancellationToken?.Dispose();
                    m_Current?.Dispose();
                    m_ProcessingTask.Dispose();

                    SoundMsgStart?.Dispose();
                    SoundMsgEnd?.Dispose();

                    foreach (var msg in SupportedMessages)
                        msg?.Dispose();

                    m_MasteringVoice.Dispose();
                    m_Device.Dispose();
                }
                m_Disposed = true;
            }
        }
        #endregion

        public void Start()
        {
            lock (m_StartStopMonitor)
            {

                if (m_ProcessingTask != null && !m_ProcessingTask.IsCompleted)
                {
                    m_ProcessingTask.Wait();
                    m_ProcessingTask.Dispose();
                }

                m_CancellationToken?.Dispose();
                m_CancellationToken = new CancellationTokenSource();
                foreach (var msg in SupportedMessages)
                    msg.m_Device = m_Device;

                m_ProcessingTask = Task.Factory.StartNew(() =>
                {
                    try
                    {
                        while (!m_CancellationToken.IsCancellationRequested)
                        {
                            bool playInOut = false;
                            var msgToPlay = m_MessagesQueue.PeekOrWait(m_CancellationToken.Token);
                            {
                                if (msgToPlay != null)
                                {
                                    m_Current = msgToPlay;
                                    if (!playInOut)
                                        playInOut = msgToPlay.PlayInOut;

                                    if (playInOut)
                                        SoundMsgStart?.PlaySync();

                                    msgToPlay.Play();
                                    m_Current = null;
                                }
                                else
                                    continue;
                            }
                            if (playInOut)
                                SoundMsgEnd?.PlaySync();
                        }
                    }
                    catch (ThreadInterruptedException) { /*ignored*/}
                    catch (OperationCanceledException) { /*ignored*/}
                    catch (Exception)
                    {
                        if (!m_CancellationToken.IsCancellationRequested)
                            Start();
                    }

                }, m_CancellationToken.Token);
            }
        }

        public void Stop()
        {
            lock (m_StartStopMonitor)
            {
                m_Current?.Stop();
                m_Current = null;

                foreach (var msg in SupportedMessages)
                    msg.Stop();

                m_CancellationToken.Cancel();
                m_MessagesQueue.Reset();
            }
        }

        private void PlayMsg(SoundMessage msg)
        {
            if (msg != null)
            {
                if (msg.Background)
                    msg.Play();
                else
                    m_MessagesQueue.Enqueue(msg);
            }
        }

        private void CancelMsg(SoundMessage msg)
        {
            if (!msg.Background)
                m_MessagesQueue.Remove(msg);
            msg.Stop();
        }

        private void ProcessMsg(SoundMessage msg, bool actual)
        {
            if (actual)
                PlayMsg(msg);
            else
                CancelMsg(msg);
        }

        #region Messages
        public void BingoFuel(bool actual = true) => ProcessMsg(MsgBingoFuel, actual);

        public void AoAMaximum(bool actual = true)
        {
            if (actual)
                CancelMsg(MsgAoAOverLimit);
            ProcessMsg(MsgAoAMaximum, actual);
        }

        public void AoAOverLimit(bool actual = true)
        {
            if (actual)
                CancelMsg(MsgAoAMaximum);
            ProcessMsg(MsgAoAOverLimit, actual);
        }

        public void GMaximum(bool actual = true)
        {
            if (actual)
                CancelMsg(MsgGOverLimit);
            ProcessMsg(MsgGMaximum, actual);
        }

        public void GOverLimit(bool actual = true)
        {
            if (actual)
                CancelMsg(MsgGMaximum);
            ProcessMsg(MsgGOverLimit, actual);
        }

        public void PullUp(bool actual = true) => ProcessMsg(MsgPullUp, actual);
        public void Overspeed(bool actual = true) => ProcessMsg(MsgOverspeed, actual);
        public void GearUp(bool actual = true) => ProcessMsg(MsgGearUp, actual);
        public void GearDown(bool actual = true) => ProcessMsg(MsgGearDown, actual);
        public void SinkRate(bool actual = true) => ProcessMsg(MsgSinkRate, actual);
        #endregion       
    }
}
