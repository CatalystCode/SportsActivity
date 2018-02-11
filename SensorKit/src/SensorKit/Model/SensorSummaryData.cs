using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SensorKitSDK
{

    public class SensorSummaryData 
    {
        public string deviceId { get; set; }

        public string name { get; set; }

        public string tag { get; set; }

        public long t { get; set; }

        public int size { get; set; }

        public int steps { get; set; }

        public int air { get; set; }

        public double airgmax { get; set; }

        public double airaltmax { get; set; }

        public int turns { get; set; }

        public double tgmax { get; set; }

        public double tgavg { get; set; }

        public double travg { get; set; }

        public double airgavg { get; set; }

        public double airt { get; set; }

        public double turnt { get; set; }

        public double stress { get; set; }

        public double stressAvg { get; set; }

        public string userId { get; set; }

        [JsonConverter(typeof(IsoDateTimeConverter))]
        [JsonProperty("readingTime")]
        public DateTime readingTime { get; set; }


    }

    


}
