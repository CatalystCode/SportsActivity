using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SensorKitSDK
{

    public class SensorAirData
    {
        public long t { get; set; }
        public double dt { get; set; }
        public double alt { get; set; }
        public double g { get; set; }
    }

}
