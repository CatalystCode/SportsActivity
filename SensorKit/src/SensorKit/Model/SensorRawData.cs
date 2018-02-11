using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SensorKitSDK
{
    public class SensorRawData
    {
        public long t { get; set; }
        public double wx { get; set; }
        public double wy { get; set; }
        public double wz { get; set; }
        public double ax { get; set; }
        public double ay { get; set; }
        public double az { get; set; }
        public int o { get; set; }
        
    }

}
