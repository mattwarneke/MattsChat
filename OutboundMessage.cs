namespace MattsChat
{
    using System;
    using System.Text;

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

        public void AddHeader(string header)
        {
            this.Message = header + this.Message;
        }
    }
}
