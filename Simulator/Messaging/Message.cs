using Newtonsoft.Json;
using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Simulator.Messaging
{
    internal class Message
    {
        public MessageType Type { get; set; }

        public Message()
        {
            
        }

        public Message(MessageType type)
        {
            Type = type;
        }


        public byte[] Encode()
        {
            return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(this));
        }
    }
}
