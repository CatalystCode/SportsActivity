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
    public class UserData : ViewModel
    {
        [JsonProperty(PropertyName = "id")]
        public string id { get; set; }

        [JsonProperty(PropertyName = "userId")]
        public string userId { get; set; }

        [JsonProperty(PropertyName = "type")]
        public string type { get; set; }

        [JsonProperty(PropertyName = "firstName")]
        public string firstName { get; set; }

        [JsonProperty(PropertyName = "lastName")]
        public string lastName { get; set; }

        [JsonProperty(PropertyName = "state")]
        public string state { get; set; }

        [JsonProperty(PropertyName = "height")]
        public double height { get; set; }

        [JsonProperty(PropertyName = "weight")]
        public double weight { get; set; }

        [JsonProperty(PropertyName = "isFemale")]
        public bool isFemale { get; set; }

        [JsonProperty(PropertyName = "stress")]
        public double stress { get; set; }

        [JsonProperty(PropertyName = "stressAvg")]
        public double stressAvg { get; set; }

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


        [IgnoreDataMember]
        public UserData Self
        {
            get
            {
                return this;
            }
        }

        [IgnoreDataMember]
        public string name
        {
            get
            {
                return $"{firstName} {lastName}";
            }
        }

        [IgnoreDataMember]
        public SensorSummaryData[] sensorSummary { get; set; }

        [IgnoreDataMember]
        public UserSensorData sensorData { get; set; }
    }


}
