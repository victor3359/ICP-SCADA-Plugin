using System;

namespace ICP_SCADA_Blazor.Data
{
    public class TagoutList
    {
        public int Index { get; set; }
        public string item { get; set; }
        public string VisibleString { get; set; }
        public DateTime Datetime { get; set; }
        public string Reason { get; set; }
        public string Comment { get; set; }
        public string Owner { get; set; }
        public bool Special { get; set; }
        public string ControlTag { get; set; }
    }
}
