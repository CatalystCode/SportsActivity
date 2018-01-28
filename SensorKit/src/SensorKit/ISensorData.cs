using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SensorKitSDK
{
    public interface ISensorData
    {
        int AirCount { get; set; }
        int TurnCount { get; set; }
        double stressAvg { get; set; }
    }
}
