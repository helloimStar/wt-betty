﻿using SharpDX.Multimedia;
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
    public enum VoiceTemplate
    {
        US_Betty = 0,
        RU_Rita
    }

    public abstract class VoiceProcessor : IDisposable
    {

        private bool m_Disposed;
        private readonly BlockingCollection<SoundMessage> m_MessagesQueue = new BlockingCollection<SoundMessage>();

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
                    m_MessagesQueue.CompleteAdding();
                    Stop();
                    m_MessagesQueue.Dispose();
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
                            continue;
                        else
                            return;
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

        public void Stop()
        {
            m_Current?.Stop();
            m_Current = null;
            m_CancellationToken.Cancel();
        }

        private void PlayMsg(SoundMessage msg)
        {
            if (msg != null)
            {
                msg.Actual = true;
                if (msg.Background)
                    msg.Play();
                else
                    m_MessagesQueue.Add(msg);
            }
        }

        private void CancelMsg(SoundMessage msg)
        {
            if (msg != null)
                msg.Actual = false;
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

        protected class SoundMessage : IDisposable
        {
            private bool m_Disposed;
            private SourceVoice m_Audio;
            private SoundStream m_SoundStream;
            private AudioBuffer m_AudioBuffer;
            private CountdownEvent m_PlaySync = new CountdownEvent(1);

            internal XAudio2 m_Device { get; set; }

            internal SoundPlayer Sound { get; set; }
            internal bool Background { get; set; }
            internal bool PlayInOut { get; set; } = true;
            internal bool Loaded => m_Audio != null;

            //Is this message actual for current flight data or should be ignored in queue
            private bool m_Actual = true;
            internal bool Actual
            {
                get => m_Actual;
                set
                {
                    m_Actual = value;
                    if (!m_Actual)
                        Stop();
                }
            }

            private bool IsPlaying { get; set; }

            internal SoundMessage()
            {
            }

            private void Load()
            {
                m_SoundStream = new SoundStream(Sound.Stream);
                var waveFormat = m_SoundStream.Format;

                m_AudioBuffer = new AudioBuffer
                {
                    Stream = m_SoundStream.ToDataStream(),
                    AudioBytes = (int)m_SoundStream.Length,
                    Flags = BufferFlags.EndOfStream
                };
                m_SoundStream.Close();

                m_Audio = new SourceVoice(m_Device, waveFormat, true);
                m_Audio.BufferEnd += (context) =>
                {
                    if (Background)
                    {
                        if (Actual)
                        {
                            m_Audio.SubmitSourceBuffer(m_AudioBuffer, m_SoundStream.DecodedPacketsInfo);
                            m_Audio.Start();
                        }
                    }
                    else
                    {
                        m_PlaySync.Signal();
                        IsPlaying = false;
                    }
                };
            }

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
                        m_Audio.DestroyVoice();
                        m_Audio.Dispose();
                        m_AudioBuffer.Stream.Dispose();

                        Sound?.Dispose();
                        m_Device = null;
                    }
                    m_Disposed = true;
                }
            }

            public void Play()
            {
                if (!IsPlaying)
                {
                    IsPlaying = true;
                    if (!Loaded)
                        Load();

                    if (!Background)
                        m_PlaySync.Reset();

                    m_Audio.SubmitSourceBuffer(m_AudioBuffer, m_SoundStream.DecodedPacketsInfo);
                    m_Audio.Start();
                    if (!Background)
                        m_PlaySync.Wait();
                }
            }

            public void Stop()
            {
                if (Actual)
                    Actual = false;
                else
                {
                    m_Audio?.Stop();
                    m_Audio?.FlushSourceBuffers();
                    IsPlaying = false;
                }
            }            
        }
    }
}
