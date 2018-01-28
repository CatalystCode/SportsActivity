using System;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using Microsoft.Azure;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using SensorKitProcessor.Models;

namespace SensorKitProcessor
{
    public static class SensorProcessorFunction
    {
        private static int BatchSize = int.Parse(CloudConfigurationManager.GetSetting("BatchSize"));
        private static string DbConnString = CloudConfigurationManager.GetSetting("SQLConnectionString");
        private static string DbTableName = CloudConfigurationManager.GetSetting("TableNameSensor");

        [FunctionName("SensorProcessorFunction")]
        public static void Run([BlobTrigger("sensorkituploadv2/exp_{itemType}_{date}_{activityTypeId}_{userId}_{deviceId}_{tag}_{deviceName}.csv", Connection = "StorageContainer")]Stream input,
            string itemType, // airtime
            string date,
            string activityTypeId,
            string userId,
            string deviceId,
            string tag,
            string deviceName,
            TraceWriter log)
        {
            var filename = $"exp_{itemType}_{date}_{activityTypeId}_{userId}_{deviceId}_{tag}_{deviceName}";
            log.Info($"PROCESSING\n Name:{filename} \n Size: {input.Length} Bytes");

            if (String.IsNullOrEmpty(userId))
                return;

            string[] detail;
            char[] seperators = { ',' };
            int linesToSkip = 1;
            int currentLine = 0;
            string data = string.Empty;
            int itemsUploaded = 0;

            var sourceDataTable = new SensorKitDataTable();
            var cultureInfo = CultureInfo.CreateSpecificCulture("en-US");

            using (StreamReader reader = new StreamReader(input))
            {
                while (currentLine < linesToSkip)
                {
                    data = reader.ReadLine();
                    currentLine++;
                }

                var sourceTable = sourceDataTable.GetSensorKitDataTable();

                while ((data = reader.ReadLine()) != null)
                {
                    detail = data.Split(seperators, StringSplitOptions.None);

                    DataRow row = sourceTable.NewRow();
                    row["timeStamp"] = DateTimeOffset.Parse(detail[0]);
                    row["experimentId"] = 0;
                    row["activityTypeId"] = int.Parse(activityTypeId);
                    row["subjectId"] = userId;
                    row["deviceId"] = deviceId;
                    row["itemType"] = itemType;
                    row["deviceName"] = deviceName;
                    row["tag"] = tag;
                    row["duration"] = float.Parse(detail[1]);
                    if (itemType == "airtime")
                    {
                        row["alt"] = float.Parse(detail[2]);
                    }
                    else
                    {
                        row["radius"] = float.Parse(detail[2]);
                    }
                    row["g"] = float.Parse(detail[3]);

                    sourceTable.Rows.Add(row);

                    if (sourceTable.Rows.Count > BatchSize)
                    {
                        UploadSensorData(sourceTable, log);
                        itemsUploaded += sourceTable.Rows.Count;
                        sourceTable.Clear();
                    }

                }

                if (sourceTable.Rows.Count > 0)
                {
                    UploadSensorData(sourceTable, log);
                    itemsUploaded += sourceTable.Rows.Count;
                }
            }

            log.Info($"Data insert DONE! Inserted {itemsUploaded} records.");
        }

        private static void UploadSensorData(DataTable atomicTable, TraceWriter log)
        {
            try
            {
                using (var dbConnection = new SqlConnection(DbConnString))
                {
                    dbConnection.Open();
                    SqlBulkCopy bcp = new SqlBulkCopy(dbConnection) { DestinationTableName = DbTableName };
                    bcp.WriteToServer(atomicTable);
                }
            }
            catch (Exception ex)
            {
                log.Error($"Critical error '{ex.GetType()}': {ex.Message}");
            }
        }
    }
}
