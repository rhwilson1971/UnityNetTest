using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;
using System.Threading.Tasks;

namespace RMSIDCUTILS.NetCommander
{
    public interface IHeartbeatTimer
    {
        void Start();
        void ResetTimer();
        void Stop();
        int MaxRetries { get; set; }
        int Timeout { get; set; }
    }

    public class PrimeNetHeartbeatTimer : IHeartbeatTimer
    {
        #region Private properties
        int _numRetries;
        private bool _shouldQuit = false;
        // Thread syc event used by thread to allow a wait for a defined number of seconds 
        // unless a second thread calls this .Set() method to force the thread to quit (e.g. on exit)
        ManualResetEvent _resetHeartbeat = new ManualResetEvent(false);
        INetTransportClient _netClient; // 
        Thread _hbThread;
        #endregion

        #region Public properties
        public int MaxRetries { get; set; }
        public int Timeout { get; set; }
        #endregion

        #region Constructors
        public PrimeNetHeartbeatTimer(INetTransportClient netClient, int maxRetries, int timeout)
        {
            MaxRetries = maxRetries;
            _netClient = netClient;
            _shouldQuit = false;
            _numRetries = 1;
            Timeout = timeout;
        }

        public PrimeNetHeartbeatTimer(INetTransportClient netClient) : this (netClient, 3, 3000)
        {
            
        }

        #endregion

        /// <summary>
        /// Starts a heartbeat timer thread that will be used to determine if the client 
        /// </summary>
        public void Start()
        {
            Debug.Log("Starting HB Timer");
            _hbThread = new Thread(ProcessTimer);
            _hbThread.Start();
        }

        /// <summary>
        /// 
        /// </summary>
        public void ResetTimer()
        {
            _resetHeartbeat.Set();
        }

        /// <summary>
        /// 
        /// </summary>
        void ProcessTimer()
        {
            Debug.Log("timer started");
            while (_shouldQuit == false)
            {
                var status = _resetHeartbeat.WaitOne(Timeout);

                if (_shouldQuit) // 
                    continue;

                if (status == false) // not signaled to be reset externally, continue with polling for hb
                {
                    if (_numRetries == MaxRetries) // cannot contact far remote, disconnect socket
                    {
                        _netClient.Disconnect();
                    }
                    else
                    {
                        if (!_netClient.Poll()) // hb, did not succeed, try again in 1 second
                        {
                            _numRetries++;
                        }
                        else
                        {
                            _numRetries = 1;
                        }
                    }
                }
                else
                {
                    _numRetries = 1;
                }
            }

            Debug.Log("HB Timer is stopping");
        }

        /// <summary>
        /// We are stopping the heartbeat timer, so set the quitting flag to true
        /// and clear the reset event so that it breaks out of WaitOne()
        /// </summary>
        public void Stop()
        {

            _shouldQuit = true;
            _resetHeartbeat.Set();
        }
    }
}