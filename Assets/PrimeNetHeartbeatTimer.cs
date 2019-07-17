﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System.Threading;
using System.Threading.Tasks;

namespace RMSIDCUTILS.Network
{
    public class PrimeNetHeartbeatTimer
    {
        #region Private properties
        int _numRetries;
        private bool _shouldQuit = false;
        ManualResetEvent _resetHeartbeat = new ManualResetEvent(false);
        IPrimeNetClient _netClient;
        
        #endregion

        #region Public properties
        public int MaxRetries { get; private set; }
        public int Timeout { get; set; }
        
        #endregion

        #region Constructors
        public PrimeNetHeartbeatTimer(IPrimeNetClient netClient, int maxRetries)
        {
            MaxRetries = maxRetries;
            _netClient = netClient;
            _shouldQuit = false;
        }

        public PrimeNetHeartbeatTimer(IPrimeNetClient netClient) : this (netClient, 3)
        {
            
        }

        #endregion

        public void Start()
        {
            new Task(ProcessTimer);
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
                var status = _resetHeartbeat.WaitOne(1000);

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
                            _numRetries = 0;
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