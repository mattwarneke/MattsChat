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
}
