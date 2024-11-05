using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Simulator.Messaging
{
    internal class JoinMessage : Message
    {
        public char Identity { get; set; }

        public JoinMessage()
        {
            
        }
        public JoinMessage(char identity) : base(MessageType.JOIN)
        {
            this.Identity = identity;
        }
    }
}
