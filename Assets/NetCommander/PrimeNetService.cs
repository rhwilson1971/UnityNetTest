using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using UnityEngine;
using UnityEngine.UI;

namespace RMSIDCUTILS.NetCommander
{
    public interface IPrimeNetService
    {
        // all clients or server
        void Broadcast(PrimeNetMessage m);

        // specific message to a specific client
        void Send(Guid client, PrimeNetMessage message);

        // start the network interfaces
        void StartService();

        void StartService(bool isServer, string ipAddress, int port);

        // shutdown the network interface
        void StopService();

        // List of Connected Clients, call list again to get latest number of connected clients
        List<PrimeNetTransportClient> GetClients();

        // when the client sends a message, add it to the concurrent priority queue
        void ProcessIncommingMessages(PrimeNetMessage message);

        PrimeNetMessage Dequeue();

        PrimeNetMessage Peek();
    }

    /// <summary>
    /// 
    /// </summary>
    public class PrimeNetService : MonoBehaviour, IPrimeNetService
    {
        #region Unity Editor Interface
        [Header("Connection Propertties")]
        public bool _IsServer;
        public string _HostNameOrIP;
        public uint _Port;
        public Text _Text;
        #endregion

        #region Private Properties
        private ConnectionInfo _conn = null;
        private PrimetNetTransportManager _networkServer = null;
        private readonly ConcurrentQueue<PrimeNetMessage> _mQueue = new ConcurrentQueue<PrimeNetMessage>();
        private readonly List<string> _clientList = new List<string>();
        #endregion

        #region Public Properties
        public bool IsRunning { get; private set; }
        #endregion

        #region Constructors
        public PrimeNetService()
        {
            IsRunning = false;
        }
        #endregion

        #region Public Interfaces
        /// <summary>
        /// Call this method to begin the network services for this project
        /// </summary>
        /// <param name="isServer">this is a network server which will allow incoming connections to a defined port</param>
        /// <param name="ipAddress">Typically, this would be the public IP on the network, e.g. 192.168.1.100</param>
        /// <param name="port"></param>
        public void StartService(bool isServer, string ipAddress, int port)
        {
            Debug.Log("Is the net service running ? " + IsRunning);

            if (IsRunning)
            {
                return;
            }

            _conn = new ConnectionInfo()
            {
                HosHostAddress = IPAddress.Parse(ipAddress),
                IsServer = isServer,
                Port = (uint)port,
                Protocol = 0
            };

            _IsServer = _conn.IsServer;
            _HostNameOrIP = _conn.HosHostAddress.ToString();
            _Port = _conn.Port;

            if (_networkServer == null)
            {
                _networkServer = new PrimetNetTransportManager(_conn);
            }

            _networkServer.SetConnection(_conn);
            _networkServer.NetworkMessageReceived += HandleMessageReceived;
            _networkServer.Startup();

            var message = string.Format("IP:{0}, Port:{1}, IsServer:{2}", _conn.HosHostAddress, _conn.Port, _conn.IsServer);
            Debug.Log(message);
            _Text.text = message;

            IsRunning = true;
        }

        /// <summary>
        /// Call this method to begin the network services for this project on a local network, where the 
        /// clients will also be started locally.  127.0.0.1, isServer=true, and the default port is 50515
        /// </summary>
        public void StartService()
        {
            StartService(true, ConnectionInfo.DefaultIpAddress, ConnectionInfo.DefaultPort);
        }

        public void StopService()
        {
            if(_networkServer == null )
            {
                Debug.LogWarning("Assign NetworkServer before calling");
                return;
            }

            Debug.Log("Stopping the netserverce");
            if (!IsRunning)
            {
                Debug.Log("netserverce reports its not running");
                return;
            }

            _networkServer.NetworkMessageReceived -= HandleMessageReceived;
            _networkServer.Shutdown();

            Debug.Log("Is running is set to false");
            IsRunning = false;
        }

        public void Broadcast(PrimeNetMessage m)
        {
            Debug.Log("Broadcasting message.  Running state? " + IsRunning);
            if (!IsRunning)
            {
                return;
            }

            Debug.Log("Broadcasting message");
            _networkServer.Broadcast(m);
        }

        public void Send(Guid id, PrimeNetMessage message)
        {
            if (!IsRunning)
            {
                return;
            }

            _networkServer.DirectMessage(id, message);
        }

        public void ProcessIncommingMessages(PrimeNetMessage message)
        {
            Debug.Log("Enqueuing a new message");
            _mQueue.Enqueue(message);
            PublishMessageAvailable(new EventArgs()); // send a signal that there are new messages
        }

        public List<PrimeNetTransportClient> GetClients()
        {
            return _networkServer.ClientList;
        }

        public PrimeNetMessage Dequeue()
        {
            PrimeNetMessage result = null;

            if (!_mQueue.IsEmpty)
            {
                if (_mQueue.TryDequeue(out result))
                {
                    Debug.Log("Concurrrent Dequeue succeeded - " + result.MessageBody);
                }
                else
                {
                    Debug.Log("Concurrrent Dequeue failed");
                }
            }

            return result;
        }

        public PrimeNetMessage Peek()
        {
            PrimeNetMessage nextMessage = null;

            if (_mQueue.Count > 0)
            {
                _mQueue.TryPeek(out nextMessage);
            }

            return nextMessage;
        }
        #endregion

        #region Events
        public event EventHandler MessageAvailable;
        #endregion

        #region Unity Overrides
        public void OnDestroy()
        {
            Debug.Log("Getting ready to DIE!");
            StopService();
        }
        #endregion

        #region Private Implementation
        void HandleMessageReceived(object sender, NetworkMessageEvent e)
        {
            Debug.Log("A new network message event is received, add it to the message queue");
            ProcessIncommingMessages(e.Data);
        }

        protected virtual void PublishMessageAvailable(EventArgs e)
        {
            MessageAvailable?.Invoke(this, e);
        }
        #endregion
    }
}