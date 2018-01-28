using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SensorKitProcessor.Models
{
    class SensorKitDataTable
    {
        public DataTable GetSensorKitDataTable()
        {
            DataTable d = new DataTable();
            d.Columns.Add("timeStamp", typeof(DateTimeOffset));
            d.Columns.Add("experimentId", typeof(int));
            d.Columns.Add("activityTypeId", typeof(int));
            d.Columns.Add("subjectId", typeof(string));
            d.Columns.Add("deviceId", typeof(string));
            d.Columns.Add("itemType", typeof(string));
            d.Columns.Add("tag", typeof(string));
            d.Columns.Add("deviceName", typeof(string));
            d.Columns.Add("duration", typeof(float)); // sec
            d.Columns.Add("alt", typeof(float));
            d.Columns.Add("g", typeof(float)); // g force
            d.Columns.Add("steps", typeof(int));
            d.Columns.Add("lat", typeof(float));
            d.Columns.Add("lon", typeof(float));
            d.Columns.Add("speed", typeof(float));
            d.Columns.Add("incl", typeof(float));
            d.Columns.Add("temp", typeof(float));
            d.Columns.Add("humidity", typeof(float));
            d.Columns.Add("radius", typeof(float));
            return d;
        }
    }
}
