using System;
using System.Collections.Generic;
using System.Net;
using UnityEngine;
using UnityEngine.UI;

namespace RMSIDCUTILS.Network
{
	public interface IPrimeNetService  
	{
		// all clients
		void Broadcast(PrimeNetMessage m);
		
		// specific message to a specific client
		void Send(Guid client, PrimeNetMessage message);
		
		// start the network interfaces
		void StartServer();
		
		// shutdown the network interface
		void StopServer();

		// when the client sends a message, add it to the concurrent priority queue
		void ProcessIncommingMessages();
		
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
        public void StartServer()
		{
			if(IsRunning) return;

            _conn = new ConnectionInfo(_IsManager, _Port, _HostNameOrIP );
			_networkServer = new PrimeNetServer(_conn);
			_networkServer.NetworkMessageReceived += HandleMessageReceived;
            _networkServer.Startup();
			IsRunning = true;
		}
		
		public void Broadcast(PrimeNetMessage m)
		{
			if(!IsRunning) return;
			_networkServer.Broadcast(m);
		}

        public void StopServer()
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

        public void ProcessIncommingMessages()
        {

        }

		public void HandleMessageReceived(object sender, NetworkMessageEvent e)
		{
			// _messageQueue.Enqueue(e.Data);
            
		}

        public List<PrimeNetClient> GetClients()
        {
            throw new NotImplementedException("not implemented yet");
        }

        private void HandleNetworkMessage(object sender, NetworkMessageEvent e)
        {
            _Text.text = e.Data.MessageBody;
        }

        #region Unity
        private void Awake()
        {
            if (_networkServer == null)
            {
                _conn = new ConnectionInfo()
                {
                    HosHostAddress = IPAddress.Parse(_HostNameOrIP),
                    IsServer = _IsManager,
                    Port = _Port == 0 ? ConnectionInfo.DefaultPort : _Port,
                    Protocol = 0
                };

                _networkServer = new PrimeNetServer(_conn);
                _networkServer.NetworkMessageReceived += HandleNetworkMessage;
                _networkServer.Startup();
            }
        }
        #endregion


    }
}