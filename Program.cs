namespace MattsChat
{
    using System;

    public class Program
    {
        private static void Main()
        {
            Console.Title = "Server";
            
            ClientService clientService = new ClientService();
            TcpClientController tcpController = new TcpClientController(clientService);
            WebClientController webController = new WebClientController(clientService);

            Console.ReadLine(); // keep app open until read an enter
            tcpController.CloseAllSockets();
            webController.CloseAllSockets();
        }
    }
}