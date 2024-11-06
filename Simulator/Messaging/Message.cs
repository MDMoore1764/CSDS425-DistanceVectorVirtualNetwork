using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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

        private const string EndOfMessage = "\u0003\u0003\u0003";
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
            StringBuilder messageBuilder = new StringBuilder(JsonConvert.SerializeObject(this));
            messageBuilder.Append(EndOfMessage);

            return Encoding.UTF8.GetBytes(messageBuilder.ToString());
        }

        public static List<Message> Decode(byte[] messageBytes)
        {
            var messages = 
                Encoding.UTF8.GetString(messageBytes)
                .Split(EndOfMessage)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(JObject.Parse)
                .Where(m => m != null)
                .Select(j =>
                {
                    var type = (MessageType)j.GetValue(nameof(Type))!.Value<int>();

                    return type switch
                    {
                        MessageType.JOIN => j.ToObject<JoinMessage>(),
                        MessageType.UPDATE => j.ToObject<UpdateMessage>(),
                        _ => null as Message,
                    };
                })
                .Cast<Message>()
                .ToList();

            return messages;
        }
    }
}
