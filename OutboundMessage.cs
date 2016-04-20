using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MattsChat
{
    public class OutboundMessage
    {
        public OutboundMessage(string msg)
        {
            string protocolMessage = "<= " + msg + Environment.NewLine;
            this.Message = protocolMessage;
        }

        public string Message { get; private set; }

        public byte[] ToBytes()
        {
            return Encoding.ASCII.GetBytes(this.Message);
        }
    }

    public static class OutboundMessageBuilder
    {
        public static OutboundMessage NewClientEntersRoomMsg(Client client)
        {
            return new OutboundMessage("* new user joined chat: " + client.Nickname);
        }

        public static OutboundMessage EndOfListBytes()
        {
            return new OutboundMessage("end of list.");
        }

        public static OutboundMessage WelcomeMessage()
        {
            return new OutboundMessage("Welcome to the matts chat server");
        }
    }
}
