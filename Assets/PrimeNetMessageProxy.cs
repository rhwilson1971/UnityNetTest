using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RMSIDCUTILS.NetCommander;

public class PrimeNetMessageProxy 
{
    private readonly IPrimeNetService _networkService;

    public PrimeNetMessageProxy(IPrimeNetService netService)
    {
        _networkService = netService;
    }

    public void Message(EPrimeNetMessage messageCode, string body = "")
    {
        var message = new PrimeNetMessage()
        {
            NetMessage = messageCode,
            MessageBody = body
        };

        _networkService.Broadcast(message);
    }
}
