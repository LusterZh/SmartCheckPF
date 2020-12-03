using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace samrtCheckPF.utils
{
    class NetworkUtils
    {
        [DllImport("wininet.dll", EntryPoint = "InternetGetConnectedState")]
        public extern static bool InternetGetConnectedState(out int conState, int reader);

        public static bool IsNetworkConnected
        {
            get
            {
                return InternetGetConnectedState(out int n, 0);
            }
        }
    }
}
