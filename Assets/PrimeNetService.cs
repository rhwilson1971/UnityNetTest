using System;
using System.Collections.Generic;
using System.Net;
using UnityEngine;
using UnityEngine.UI;

namespace RMSIDCUTILS.Network
{
	public interface IPrimeNetService  
	{
		// all clients or server
		void Broadcast(PrimeNetMessage m);
		
		// specific message to a specific client
		void Send(Guid client, PrimeNetMessage message);
		
		// start the network interfaces
		void StartService();
		
		// shutdown the network interface
		void StopService();

		// when the client sends a message, add it to the concurrent priority queue
		void ProcessIncommingMessages(PrimeNetMessage message);
		
		// 
		List<PrimeNetClient> GetClients();
	}

	public class PrimeNetService : MonoBehaviour, IPrimeNetService
	{
		#region Unity Editor Interface
		[Header("Connection Propertties")]
		public bool _IsManager;
		public string _HostNameOrIP;
		public uint _Port;
        public Text _Text;
        #endregion

        #region Private Properties
        static PrimeNetService _instance = null;
		PrimeNetServer _networkServer=null;
		ConnectionInfo _conn=null;
		Queue<PrimeNetMessage> _messageQueue = new Queue<PrimeNetMessage>();

        List<string> _clientList = new List<string>();

		#endregion
		
		#region Public Properties
		public bool IsRunning;
        public static PrimeNetService Instance
        {
            get
            {
                if( _instance == null )
                {
                    _instance = new PrimeNetService();
                }

                return _instance;
            }
        }
		#endregion
		
		#region Constructors
		public PrimeNetService()
		{
			IsRunning=false;
		}
        #endregion

        #region Public Interfaces
        public void StartService()
		{
            if (IsRunning)
                return;

            if (_networkServer == null)
            {
                _conn = new ConnectionInfo()
                {
                    HosHostAddress = IPAddress.Parse(_HostNameOrIP),
                    IsServer = _IsManager,
                    Port = _Port == 0 ? ConnectionInfo.DefaultPort : _Port,
                    Protocol = 0
                };

                var message = string.Format("IP:{0}, Port:{1}, IsServer:{2}", _conn.HosHostAddress, _conn.Port, _conn.IsServer);
                Debug.Log(message);
                _Text.text = message;

                _networkServer = new PrimeNetServer(_conn);
                _networkServer.NetworkMessageReceived += HandleMessageReceived;
                _networkServer.Startup();
            }
            IsRunning = true;
		}
		
		public void Broadcast(PrimeNetMessage m)
		{
            Debug.Log("Broadcasting message.  Running state? " + IsRunning);
            if (!IsRunning) return;

            Debug.Log("Broadcasting message");
			_networkServer.Broadcast(m);
		}

        public void StopService()
		{
			if(!IsRunning) return;
			_networkServer.Shutdown();
		}

        public void Send(Guid id, PrimeNetMessage message)
		{
			if(!IsRunning) return;
			_networkServer.Send(id, message);
		}
        #endregion

        public event EventHandler MessageAvailable;

        public void ProcessIncommingMessages(PrimeNetMessage message)
        {
            _messageQueue.Enqueue(message);
            OnMessageAvailable(new EventArgs());
        }

		public void HandleMessageReceived(object sender, NetworkMessageEvent e)
		{
            // switch (e.Data.NetMessage}
            Debug.Log("Should have gotten a new message from handler");
            // _messageQueue.Enqueue(e.Data);
            _Text.text = string.Format("Got message - {0}", e.Data.MessageBody);
            ProcessIncommingMessages(e.Data);
		}

        protected virtual void OnMessageAvailable(EventArgs e)
        {
            // Make a temporary copy of the event to avoid possibility of
            // a race condition if the last subscriber unsubscribes
            // immediately after the null check and before the event is raised.
            MessageAvailable?.Invoke(this, e);
        }

        public List<PrimeNetClient> GetClients()
        {
            //_networkServer.

            return null;
        }
        #region Unity
        private void Awake()
        {
            if( !IsRunning )
                StartService();
        }
        #endregion


    }
}