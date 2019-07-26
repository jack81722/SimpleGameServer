using GameSystem.GameCore.Network;
using System;
using System.Threading;

namespace SimpleGameServer
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello");
            SimpleServer server = new SimpleServer(FormmaterSerializer.GetInstance());
            server.ConnectKey = "Test";
            server.MaxPeers = 100;
            server.Start(8888);

            AppDomain.CurrentDomain.ProcessExit += (sender, e) => server.Close();

            while (server.isRunning)
            {
                Thread.Sleep(100);
            }
            Console.ReadLine();
        }

    }
}
