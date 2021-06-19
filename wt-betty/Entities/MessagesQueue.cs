using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace wt_betty.Entities
{
    internal class MessagesQueue
    {
        private List<SoundMessage> m_List = new List<SoundMessage>();
        private readonly object m_IOLock = new object();
        private CountdownEvent m_NotEmptyEvent = new CountdownEvent(1);

        public void Enqueue(SoundMessage message)
        {
            lock (m_IOLock)
            {
                if (!m_List.Contains(message))
                {
                    m_List.Add(message);
                    if (m_NotEmptyEvent.CurrentCount > 0)
                        m_NotEmptyEvent.Signal();
                }
            }
        }

        public void Remove(SoundMessage message)
        {
            lock (m_IOLock)
            {
                if (m_List.Contains(message))
                {
                    m_List.RemoveAll(m => m == message);
                    if (m_List.Count == 0)
                        m_NotEmptyEvent.Reset();
                }
            }
        }

        public void Reset()
        {
            lock (m_IOLock)
            {
                m_List.Clear();
            }
        }

        public SoundMessage Peek()
        {
            lock (m_IOLock)
            {
                if (m_List.Count > 0)
                {
                    var result = m_List[0];
                    Remove(result);
                    return result;
                }
            }
            return null;
        }

        public SoundMessage PeekOrWait(CancellationToken cancellationToken)
        {
            while (m_List.Count == 0)
                m_NotEmptyEvent.Wait(cancellationToken);
            lock (m_IOLock)
            { 
                return Peek();
            }
        }
    }
}
