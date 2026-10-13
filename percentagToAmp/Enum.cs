using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace percentagToAmp
{
    public enum ePlcDevice
    {
        None = 0,
        Water_Consumption = 8010,
    }

    public enum eConnectionState
    {
        Connecting,
        NotConnected,
        Connected,
        PartialConnected
    }
}
