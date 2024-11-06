using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Simulator.ThreadLogger
{
    public static class Locks
    {
        public static object LoggingLock = new();
    }
}
