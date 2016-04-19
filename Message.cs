using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

// This file contains all messages defined that are passed back and forth between the client and the server.
// For this test project, only two messages are defined.
namespace MattsChat
{
    public class ByteMessage
    {
        public ByteMessage(byte[] msg)
        {
            this.Message = msg;
        }

        public byte[] Message { get; private set; }
        
        /// <summary>
        /// Deserializes an object from a binary representation.
        /// </summary>
        /// <param name="binaryObject">The byte array to deserialize.</param>
        public StringMessage Deserialize()
        {
            using (MemoryStream stream = new MemoryStream(Message))
            {
                return new StringMessage(new BinaryFormatter().Deserialize(stream).ToString());
            }
        }
    }

    /// <summary>
    /// A message containing a single string.
    /// </summary>
    [Serializable]
    public class StringMessage
    {
        public StringMessage(string msg)
        {
            this.Message = msg;
        }

        /// <summary>
        /// The string.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Serializes an object to a binary representation, returned as a byte array.
        /// </summary>
        /// <param name="message">The object to serialize.</param>
        public byte[] Serialize()
        {
            using (MemoryStream stream = new MemoryStream())
            {
                new BinaryFormatter().Serialize(stream, this.Message);
                return stream.ToArray();
            }
        }
    }
}
