using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using wt_betty.Entities;
using wt_betty.Utils;

namespace wt_betty
{
    public class ConnectionEventArgs : EventArgs
    {
        public bool Connected { get; set; }
    }

    public class StateEventArgs : EventArgs
    {
        public Indicator Indicator { get; set; }
        public State State { get; set; }
        public string ErrorDetails { get; set; }
    }

    public class ConnectionManager
    {
        public const string URL_INDICATORS = "http://localhost:8111/indicators";
        public const string URL_STATES = "http://localhost:8111/state";

        private static readonly TimeSpan CONNECTION_CHECK_TIMEOUT = new TimeSpan(0, 0, 5);
        private static readonly TimeSpan DATA_UPDATE_TIMEOUT = new TimeSpan(0, 0, 0, 0, 200);
        
        public bool Running { get => m_CancellationTokenSource != null; }
        private volatile bool m_Connected;
        public bool Connected
        {
            get => m_Connected;
            private set
            {
                m_Connected = value;
                OnConnectionChanged?.Invoke(this, new ConnectionEventArgs() { Connected = m_Connected });
            }
        }

        private Task m_Worker;
        private CancellationTokenSource m_CancellationTokenSource;
        private Task m_CheckConnectionTask, m_GetDataTask;
        
        public event EventHandler OnConnectionChanged;
        public event EventHandler OnStateUpdated;

        public void Start()
        {
            if (!Running)
            {
                m_CancellationTokenSource = new CancellationTokenSource();

                var oldWorker = m_Worker;
                m_Worker = Task.Run(() =>
                {
                    List<Task> waitForEndList = new List<Task>();
                    if (oldWorker != null && !oldWorker.IsCompleted)
                        waitForEndList.Add(oldWorker);
                    if (m_CheckConnectionTask != null && !m_CheckConnectionTask.IsCompleted)
                        waitForEndList.Add(m_CheckConnectionTask);
                    if (m_GetDataTask != null && !m_GetDataTask.IsCompleted)
                        waitForEndList.Add(m_GetDataTask);

                    m_CheckConnectionTask = RepeatableTask.Run(() =>
                    {
                        if (waitForEndList != null)
                            Task.WaitAll(waitForEndList.ToArray());
                        waitForEndList = null;

                        if (!Connected)
                        {
                            try
                            {
                                TcpClient connectionTest = new TcpClient("localhost", 8111);
                                connectionTest.Close();
                                Connected = true;
                            }
                            catch (Exception e)
                            {
                                Connected = false;
                            }
                        }
                    }, new TimeSpan(0, 0, 5), m_CancellationTokenSource.Token);

                    m_GetDataTask = RepeatableTask.Run(() =>
                    {
                        if (Connected)
                        {
                            try
                            {
                                var indicator = JsonSerializer._download_serialized_json_data<Indicator>(URL_INDICATORS);
                                var state = JsonSerializer._download_serialized_json_data<State>(URL_STATES);

                                bool dataIsValid = state != null && indicator != null;
                                if (dataIsValid)
                                {
                                    dataIsValid = state.valid == "true" && indicator.valid == "true";
                                    OnStateUpdated?.Invoke(this, new StateEventArgs() { Indicator = indicator, State = state });
                                }
                                else
                                    Connected = false;
                            }
                            catch (Exception e)
                            {
                                OnStateUpdated?.Invoke(this, new StateEventArgs() { ErrorDetails = e.Message });
                                Connected = false;
                            }
                        }
                    }, DATA_UPDATE_TIMEOUT, m_CancellationTokenSource.Token);
                }, m_CancellationTokenSource.Token);
            }
        }

        public void Stop()
        {
            if (Running)
            {
                m_CancellationTokenSource.Cancel(true);
                m_CancellationTokenSource = null;
                m_Connected = false;
            }
        }
    }
}
