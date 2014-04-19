using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimeExt
{
    internal static class EventHelper
    {
	[DebuggerHidden]
        internal static void Raise(EventHandler handler, object sender, EventArgs args) 
        {
            if (handler != null)
                handler(sender, args);
        }

	[DebuggerHidden]
        internal static void Raise<TEventArgs>(EventHandler<TEventArgs> handler, object sender, TEventArgs args) where TEventArgs : EventArgs
        {
            if (handler != null)
                handler(sender, args);
        }
    }
}
