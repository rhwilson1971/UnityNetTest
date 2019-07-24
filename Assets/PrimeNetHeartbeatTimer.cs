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
    }

    public class PrimeNetHeartbeatTimer : IHeartbeatTimer
    {
        #region Private properties
        int _numRetries;
        private bool _shouldQuit = false;
        ManualResetEvent _resetHeartbeat = new ManualResetEvent(false);
        INetTransportClient _netClient;
        Thread _hbThread;
        #endregion

        #region Public properties
        public int MaxRetries { get; private set; }
        public int Timeout { get; set; }
        
        #endregion

        #region Constructors
        public PrimeNetHeartbeatTimer(INetTransportClient netClient, int maxRetries)
        {
            MaxRetries = maxRetries;
            _netClient = netClient;
            _shouldQuit = false;
            _numRetries = 1;
        }

        public PrimeNetHeartbeatTimer(INetTransportClient netClient) : this (netClient, 3)
        {
            
        }

        #endregion

        public void Start()
        {
            Debug.Log("Starting HB Timer");
            _hbThread = new Thread(ProcessTimer);
            _hbThread.Start();
        }

        public void ResetTimer()
        {
            _resetHeartbeat.Set();
        }

        void ProcessTimer()
        {
            Debug.Log("timer started");
            while (_shouldQuit == false)
            {
                var status = _resetHeartbeat.WaitOne(3000);

                if (_shouldQuit)
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
            }
        }

        public void Stop()
        {
            _shouldQuit = true;
        }
    }
}