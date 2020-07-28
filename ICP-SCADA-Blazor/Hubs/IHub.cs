using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ICP_SCADA_Blazor
{
    public interface IHub
    {
        Task ReceivedUpdate(string msg);
    }
}
