using SharpDX.Multimedia;
using SharpDX.XAudio2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Media;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace wt_betty.Entities
{
    public class SoundMessage : IDisposable
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
                    if (IsPlaying)
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
            IsPlaying = false;
            m_Audio?.Stop();
            m_Audio?.FlushSourceBuffers();
        }
    }
}
