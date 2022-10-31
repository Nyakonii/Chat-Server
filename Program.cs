// See https://aka.ms/new-console-template for more information

using System.Collections;
using System.Diagnostics;
using LiteNetLib;
using LiteNetLib.Utils;

namespace SocketFun;

internal static class SocketFun
{
    private static readonly EventBasedNetListener Listener = new();
    private static readonly NetManager Server = new(Listener);

    private static bool KillPro = false;
    
    private static void StartServer()
    {
        Server.Start(1576);
        Console.WriteLine("Press Q at any point to kill the server");
        Console.WriteLine("Server Listening On Port {0}", Server.LocalPort);
        
        Listener.ConnectionRequestEvent += request =>
        {
            if (Server.ConnectedPeersCount <= 2)
                request.AcceptIfKey("BigStinky");
            else
                request.Reject();
        };
        Listener.PeerConnectedEvent += peer =>
        {
            Console.WriteLine("Connection From: {0}", peer.EndPoint);
            NetDataWriter data = new();
            string greet = $"Welcome Id: {peer.Id}";
            data.Put(greet);
            peer.Send(data, DeliveryMethod.ReliableOrdered);
            data.Reset();
            string welcome = $"Socket Id: {peer.Id} Connected";
            data.Put(welcome);
            Server.SendToAll(data, DeliveryMethod.ReliableOrdered, peer);
        };
        
        Listener.NetworkReceiveEvent += (peer, reader, channel, method) =>
        {
            NetDataWriter data = new();
            string message = $"Socket Id: {peer.Id} : {reader.GetString(500)}";
            data.Put(message);
            Server.SendToAll(data, DeliveryMethod.ReliableOrdered, peer);
            reader.Recycle();
        };

        Listener.PeerDisconnectedEvent += (peer, info) =>
        {
            Console.WriteLine(info.Reason);
            Console.WriteLine("Disconnection From: {0}", peer.EndPoint);
            NetDataWriter data = new();
            string disconnect = $"Socket Id: {peer.Id} Disconnected";
            data.Put(disconnect);
            Server.SendToAll(data, DeliveryMethod.ReliableOrdered);
        };

        while (!KillPro)
        {
            Server.PollEvents();
            Thread.Sleep(100);
        }
        
        KillServer();
    }

    private static void KillServer()
    {
        NetDataWriter data = new();
        data.Put("Server Shutting Down... in 5 seconds");
        Console.WriteLine("Server Shutting Down... in 5 seconds");
        Server.SendToAll(data, DeliveryMethod.ReliableOrdered);
        Thread.Sleep(5000);
        Server.DisconnectAll();
        Server.Stop();
        Console.WriteLine("Server Shut Down : Exiting");
        Environment.Exit(0);
    }

    public static void CheckKill()
    {
        ConsoleKeyInfo inf;
        do
        {
            while (Console.KeyAvailable == false)
                Thread.Sleep(15);
            
            inf = Console.ReadKey(true);
        } while (inf.Key != ConsoleKey.Q);

        KillPro = true;
    }
    
    public static void Main(string[] args)
    {
        Thread killThread = new Thread(CheckKill);
        killThread.Start();
        StartServer();
    }
}