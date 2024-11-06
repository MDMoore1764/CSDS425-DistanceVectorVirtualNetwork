using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Simulator.Messaging
{
    internal class UpdateMessage : Message
    {
        public UpdateMessage(char identity, Dictionary<char, int> distanceVector) : base(MessageType.UPDATE)
        {
            Identity = identity;
            DistanceVector = distanceVector;
        }

        public char Identity { get; }
        public Dictionary<char, int> DistanceVector { get; }
    }
}
