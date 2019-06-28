using System;
using System.Collections.Generic;
using System.Net;
using UnityEngine;

namespace RMSIDCUTILS.Network
{
	public interface INetManager  
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
		List<NetworkClient> GetClients();
	}

	public class NetMan : MonoBehaviour, INetManager
	{
		#region Unity Editor Interface
		[Header("Connection Propertties")]
		public bool _IsManager;
		public string _HostNameOrIP;
		public uint _Port;
		#endregion
		
		#region Private Properties
		NetworkService _networkService=null;
		ConnectionInfo _conn=null;
		Queue<PrimeNetMessage> _messageQueue = new Queue<PrimeNetMessage>();
		#endregion
		
		#region Public Properties
		public bool IsRunning;
		#endregion
		
		#region Constructors
		public NetMan()
		{
			IsRunning=false;
		}
        #endregion

        #region Public Interfaces
        public void StartServer()
		{
			if(IsRunning) return;

            _conn = new ConnectionInfo(_IsManager, _Port, _HostNameOrIP );
			_networkService = new NetworkService(_conn);
			_networkService.NetworkMessageReceived += HandleMessageReceived;
            _networkService.Startup();
			IsRunning = true;
		}
		
		public void Broadcast(PrimeNetMessage m)
		{
			if(!IsRunning) return;
			_networkService.Broadcast(m);
		}

        public void StopServer()
		{
			if(!IsRunning) return;
			_networkService.Shutdown();
		}

        public void Send(Guid id, PrimeNetMessage message)
		{
			if(!IsRunning) return;
			_networkService.Send(id, message);
		}
		#endregion
		
		public void ProcessIncommingMessages()
        {

        }
		public void HandleMessageReceived(object sender, NetworkMessageEvent e)
		{
			_messageQueue.Enqueue(e.Data);
		}

        public List<NetworkClient> GetClients()
        {
            throw new NotImplementedException("not implemented yet");
        }
	}

    public class ConnectionInfo 
	{
        public const int DefaultPort = 50515;

        public bool IsServer;
		public uint Port;
		public uint Protocol;
		public IPAddress HosHostAddress;
		
		public ConnectionInfo() : this (false, DefaultPort, "127.0.0.1", 0)
		{
			
		}
		
		public ConnectionInfo(bool isServer, uint port, string host, uint protocol=0)
        {
            IsServer = isServer;
			Port = port;
            var ips = Dns.GetHostAddresses(host);

            foreach(var ip in ips)
            {
                HosHostAddress = ip;
            }

			Protocol=protocol;
		}
	}
}