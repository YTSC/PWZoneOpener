using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PWZoneOpener
{
    class Zone
    {
        public string hexValue { get; set; }
        public bool open { get; set; }
        public long offset { get; set; }
        public string line { get; set; }
        public string column { get; set; }

        public Zone(string line, string column,string hexValue,long offset)
        {
            if (hexValue == "01000000") this.open = true;
            else if (hexValue == "00000000") this.open = false;
            this.offset = offset;
            this.line = line;
            this.column = column;
            this.hexValue = hexValue;
        }
     
        public void openZone()
        {
            this.open = true;
            hexValue = "01000000";
        }
        public void closeZone()
        {
            this.open = false;
            hexValue = "00000000";
        }
    }
}
