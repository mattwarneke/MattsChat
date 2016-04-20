using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

// This file contains all messages defined that are passed back and forth between the client and the server.
// For this test project, only two messages are defined.
namespace MattsChat
{
    using System.Linq;
    using System.Text;

    public class InboundMessage
    {
        public InboundMessage(byte[] msg)
        {
            this.StringMessage = Encoding.ASCII.GetString(msg);

            char[] characterArray = 
                this.StringMessage.Where(c => 
                    (char.IsLetterOrDigit(c) || 
                        char.IsWhiteSpace(c) ||
                        char.IsPunctuation(c) ||
                        char.IsControl(c))).ToArray();

            this.StringMessage = new string(characterArray);

            this.StringMessage = this.StringMessage.ToLower();

            if (this.StringMessage.EndsWith(Environment.NewLine))
            {
                this.StringMessage = this.StringMessage.Remove(this.StringMessage.Length - 2, 2);
            }

            this.ReplaceBackspaces();
        }

        public string StringMessage { get; private set; }

        private void ReplaceBackspaces()
        {
            StringBuilder result = new StringBuilder(this.StringMessage.Length);
            foreach (char c in this.StringMessage)
            {
                if (c == '\b')
                {
                    if (result.Length > 0)
                        result.Length--;
                }
                else
                {
                    result.Append(c);
                }
            }
            this.StringMessage = result.ToString();
        }
    }
}
