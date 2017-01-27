using System.Net;
using System.Threading.Tasks;

namespace MattsChat
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Net.Sockets;
    using System.Net.WebSockets;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Web;
    using System.Web.Http;
    using System.Web.WebSockets;

    public class WebClientController
    {
        private const int BufferSize = 2048;
        private ClientService ClientService { get; set; }

        public WebClientController(ClientService clientService)
        {
            this.ClientService = clientService;
            this.Start();
        }

        public async void Start()
        {
            HttpListener listener = new HttpListener();
            listener.Prefixes.Add("http://+:80/wsChat/");
            
            listener.Start();
            Console.WriteLine("Listening...");
           
            while (true)
            {
                HttpListenerContext listenerContext = await listener.GetContextAsync();
                if (listenerContext.Request.IsWebSocketRequest)
                {
                    this.AcceptClient(listenerContext);
                }
                else
                {
                    listenerContext.Response.StatusCode = 400;
                    listenerContext.Response.Close();
                }
            }
        }
    
        private async void AcceptClient(HttpListenerContext listenerContext)
        {
            WebSocketContext webSocketContext = null;
            try
            {
                webSocketContext = await listenerContext.AcceptWebSocketAsync(subProtocol: null);
            }
            catch(Exception e)
            {
                // The upgrade process failed somehow. For simplicity lets assume it was a failure on the part of the server and indicate this using 500.
                listenerContext.Response.StatusCode = 500;
                listenerContext.Response.Close();
                Console.WriteLine("Exception: {0}", e);
                return;
            }
                                
            WebSocket webSocket = webSocketContext.WebSocket;

            IClient newClient = new WebClient(webSocket);
            newClient.ClearBytes();

            this.ClientService.AddClient(newClient);

            newClient.Send(new OutboundMessage("Welcome to the matts chat server"));

            this.ClientService.PromptForNickname(newClient);

            this.Listen((WebClient)newClient);
        }

        private async void Listen(WebClient client)
        {
            WebSocket webSocket = client.Socket;
            
            try
            {
                byte[] byteBuffer = new byte[BufferSize];
                ArraySegment<byte> receiveBuffer = new ArraySegment<byte>(byteBuffer);

                if (webSocket.State == WebSocketState.Open)
                {
                    WebSocketReceiveResult receiveResult;
                    try
                    {
                        //CancellationTokenSource ct = new CancellationTokenSource(10000);
                        //CancellationToken token = ct.Token;
                        receiveResult = await webSocket.ReceiveAsync(receiveBuffer, CancellationToken.None);
                    }
                    catch (Exception)
                    {
                        this.Listen(client);
                        return;
                    }
                    
                    if (!receiveBuffer.Any())
                    {
                        this.Listen(client);
                    }

                    client.AppendBytes(receiveBuffer.Array.Where(b => !b.Equals((byte)0)).ToArray());
                    
                    if (receiveResult.EndOfMessage == false)
                    {
                        this.Listen(client);
                        return;
                    }

                    InboundMessage message = new InboundMessage(client.CurrentBytesSentWithoutNewLine.ToArray());

                    client.ClearBytes();

                    Console.WriteLine("WEB Text: " + message.StringMessage);

                    if (message.StringMessage == "/quit") // Client wants to exit gracefully
                    {
                        client.Send(new OutboundMessage("BYE"));
                        CloseClientConnection(client);
                        return;
                    }

                    this.ClientService.ProcessMessage(client, message);

                    this.Listen(client);
                }
            }
            catch(Exception e)
            {
                Console.WriteLine("Exception: {0}", e);

                this.CloseClientConnection(client);

                if (webSocket != null)
                    webSocket.Dispose();
            }
        }

        public void CloseAllSockets()
        {
            List<WebClient> clients = this.ClientService.GetWebClients();
            foreach(WebClient client in clients)
            {
                this.CloseClientConnection(client);
            }
        }

        private void CloseClientConnection(WebClient client)
        {
            Console.WriteLine("WEB close connection " + client.Nickname);
            this.ClientService.LeaveChatRoom(client);
            this.ClientService.DisconnectClient(client);

            client.Disconnect();
        }
    }
}
