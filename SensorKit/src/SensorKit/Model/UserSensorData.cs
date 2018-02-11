using Microsoft.Azure.Documents;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace SensorKitSDK
{
    public class UserSensorData
    {
        [JsonProperty(PropertyName = "id")]
        public string id { get; set; }

        [JsonProperty(PropertyName = "userId")]
        public string userId { get; set; }

        [JsonProperty(PropertyName = "type")]
        public string type { get; set; }

        [JsonProperty(PropertyName = "splits")]
        public int splits { get; set; }

        [JsonProperty(PropertyName = "vertical")]
        public double vertical { get; set; }

        [JsonProperty(PropertyName = "speed")]
        public double speed { get; set; }

        [JsonProperty(PropertyName = "distance")]
        public double distance { get; set; }

        [JsonConverter(typeof(IsoDateTimeConverter))]
        [JsonProperty("readingTime")]
        public DateTime readingTime { get; set; }

        [JsonProperty(PropertyName = "sensorSummary")]
        public SensorSummaryData[] sensorSummary { get; set; }

       


    }


}
