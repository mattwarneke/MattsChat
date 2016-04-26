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
        private const int Port = 443;
        private TcpListener server;
        private readonly byte[] Buffer = new byte[BufferSize];
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
                byte[] bufferArray = new byte[1024];
                ArraySegment<byte> receiveBuffer = new ArraySegment<byte>(bufferArray);

                if (webSocket.State == WebSocketState.Open)
                {
                    WebSocketReceiveResult receiveResult = await webSocket.ReceiveAsync(receiveBuffer, CancellationToken.None);

                    if (receiveBuffer.Any())
                    {
                        
                        client.AppendBytes(receiveBuffer.Array.Where(b => !b.Equals((byte)0)).ToArray());
                    }

                    if (receiveResult.EndOfMessage == false)
                    {
                        this.Listen(client);
                        return;
                    }

                    InboundMessage message = new InboundMessage(client.CurrentBytesSentWithoutNewLine.ToArray());

                    client.ClearBytes();

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
                if (webSocket != null)
                    webSocket.Dispose();
            }
            //finally
            //{
            //    if (webSocket != null)
            //        webSocket.Dispose();
            //}
        }

        public void CloseAllSockets()
        {
            List<WebClient> clients = this.ClientService.GetWebClients();
            foreach(WebSocket socket in clients.Select(c => c.Socket))
            {
                socket.CloseAsync(WebSocketCloseStatus.Empty, string.Empty, CancellationToken.None);
            }
        }

        private void CloseClientConnection(IClient client)
        {
            Console.WriteLine("close connection " + client.Nickname);
            this.ClientService.LeaveChatRoom(client);
            this.ClientService.DisconnectClient(client);

            client.Disconnect();
        }
    }

    public static class HelperExtensions
    {        
        public static Task GetContextAsync(this HttpListener listener)
        {
            return Task.Factory.FromAsync<HttpListenerContext>(listener.BeginGetContext, listener.EndGetContext, TaskCreationOptions.None);
        }
    }
}
