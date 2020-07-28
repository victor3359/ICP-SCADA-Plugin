using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace ICP_SCADA_Blazor
{
    public class SignalRHub : Hub<IHub>
    {
        public async Task UpdateAllComponent()
        {
            await Clients.All.ReceivedUpdate("UpdateAllComponent");
        }
    }
}
