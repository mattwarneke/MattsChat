using System;
using System.Collections.Generic;
using System.Text;

namespace MattsChat
{
    class ChatRoom
    {
        public ChatRoom(string name)
        {
            this.Name = name;
            this.Clients = new List<Client>();
            this.UniqueId = Guid.NewGuid();
        }

        public Guid UniqueId { get; private set; }

        public string Name { get; private set; }

        public ICollection<Client> Clients { get; private set; }

        public void ClientJoin(Client client)
        {
            //broadcast entry before client joins, so we don't have to exclude them
            this.BroadCastMessage(OutboundMessageBuilder.NewClientEntersRoomMsg(client).ToBytes());

            this.Clients.Add(client);
        }

        public void ClientLeave(Client client)
        {
            //remove client first so we can tailor message
            this.Clients.Remove(client);

            client.LeaveChatroom();

            this.BroadCastMessage(new OutboundMessage(
                " * user has left chat: " + client.Nickname).ToBytes());

            lock (client.Socket)
            {
                //we use a blocking mode send, no async on the outgoing
                //since this is primarily a multithreaded application, shouldn't cause problems to send in blocking mode
                client.Socket.Send(new OutboundMessage(
                    "* user has left chat: " + client.Nickname + " (** this is you)").ToBytes());
            }
        }

        public void BroadCastMessage(byte[] byteMsg)
        {
            foreach (Client receiver in this.Clients)
            {
                lock (receiver.Socket)
                {
                    //we use a blocking mode send, no async on the outgoing
                    //since this is primarily a multithreaded application, shouldn't cause problems to send in blocking mode
                    receiver.Socket.Send(byteMsg);
                }
                
            }
        }
    }
}
