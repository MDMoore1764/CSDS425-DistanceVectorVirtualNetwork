using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Simulator.Messaging
{
    internal class UpdateMessage : Message
    {
        public UpdateMessage(Dictionary<char, int> distanceVector) : base(MessageType.UPDATE)
        {
            DistanceVector = distanceVector;
        }

        public Dictionary<char, int> DistanceVector { get; }
    }
}
