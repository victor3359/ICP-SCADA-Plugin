using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ICP_SCADA_Blazor.Data
{
    public class OtherMapping
    {
        public string ControlTag { get; set; }
        public string VisibleString { get; set; }
        public string Reason { get; set; }
        public string Comment { get; set; }
        public List<string> AssociateTag { get; set; }
    }
}
