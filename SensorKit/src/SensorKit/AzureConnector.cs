using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;

namespace SensorKitSDK
{
    public class AzureConnector
    {
        const string accountURL = @"<YOUR COSMOS DB ACCOUNT URL>";
        const string accountKey = @"<YOUR COSMOS DB KEY>";
        const string databaseId = @"<YOUR COSMOS DB DATABASE>";
        const string collectionId = @"<YOUR COSMOS DB COLLECTION>";

        private Uri collectionLink = UriFactory.CreateDocumentCollectionUri(databaseId, collectionId);

        private DocumentClient client;

        public AzureConnector()
        {
            //ConnectionPolicy connectionPolicy = new ConnectionPolicy { ConnectionMode = ConnectionMode.Direct, ConnectionProtocol = Protocol.Tcp };
            //connectionPolicy.RetryOptions.MaxRetryAttemptsOnThrottledRequests = 100;
            //connectionPolicy.RetryOptions.MaxRetryWaitTimeInSeconds = 60;
            client = new DocumentClient(new System.Uri(accountURL), accountKey);
        }

        public async Task<bool> UploadFileAsync(string name, string path, string key)
        {
            try
            {
                using (var stream = File.OpenRead(path))
                {
                    var doc = Document.LoadFrom<Document>(stream);
                    if (key != null)
                    {
                        doc.Id = key;
                    }
                    doc.SetPropertyValue("type", "userSensors");
                    await client.UpsertDocumentAsync(collectionLink, doc);
                    Debug.WriteLine($"Uploaded {name}");
                }
            }
            catch (Exception x)
            {
                Debug.WriteLine(x);
            }
            return false;
        }

        public async Task InsertAsync(object data)
        {
            try
            {
                await client.UpsertDocumentAsync(collectionLink, data);
            }
            catch (Exception x)
            {
                Debug.WriteLine(x);
            }
        }

        public bool CheckUserExists(string userId)
        {
            try
            {
                //var users =
                //from f in client.CreateDocumentQuery<UserData>(collectionLink)
                //where f.type == "user" && f.userId == userId
                //select f;

                //if (users.ToList().Count == 1)
                //    return true;

                var query = new SqlQuerySpec(
                   "SELECT f.teamId FROM Items f where f.type = \"team\" and ARRAY_CONTAINS (f.users, @userId)",
                   new SqlParameterCollection(new SqlParameter[] { new SqlParameter { Name = "@userId", Value = userId } }));

                dynamic doc = client.CreateDocumentQuery<dynamic>(collectionLink, query,
                   new FeedOptions { MaxItemCount = -1, EnableCrossPartitionQuery = true }).ToList();

                if (doc.Count > 0)
                    return true;
                else
                    return false;


            }
            catch (Exception x)
            {
                Debug.WriteLine(x);
            }
            return false;
        }

        public async Task InsertUserSensorDataAsync(UserSensorData data)
        {
            try
            {
                await client.UpsertDocumentAsync(collectionLink, data);
            }
            catch (Exception x)
            {
                Debug.WriteLine(x);
            }
        }

        public async Task<List<UserTeam>> GetTeamsAsync(string userId)
        {

            try
            {
                //Query asychronously. Optionally set FeedOptions.MaxItemCount to control page size
                var query = new SqlQuerySpec(
                    "SELECT * FROM Items f where f.type = \"team\" and ARRAY_CONTAINS (f.users, @userId)",
                    new SqlParameterCollection(new SqlParameter[] { new SqlParameter { Name = "@userId", Value = userId } }));

                var queryable = client.CreateDocumentQuery<UserTeam[]>(collectionLink, query,
                    new FeedOptions { MaxItemCount = -1, EnableCrossPartitionQuery = true }).AsDocumentQuery();

                var items = new List<UserTeam>();
                while (queryable.HasMoreResults)
                {
                    items.AddRange(await queryable.ExecuteNextAsync<UserTeam>());
                }
                return items;
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
            }

            return null;
        }

        public async Task<UserSensorData> GetSensorDataAsync(string userId)
        {

            try
            {
                if (userId != null)
                {
                    var query = client.CreateDocumentQuery<UserSensorData>(collectionLink, new FeedOptions { MaxItemCount = -1, EnableCrossPartitionQuery = true })
                          .Where(t => t.type == "userSensors" && t.userId == userId)
                          .AsDocumentQuery();
                    var items = new List<UserSensorData>();
                    while (query.HasMoreResults)
                    {
                        items.AddRange(await query.ExecuteNextAsync<UserSensorData>());
                    }

                    return items.FirstOrDefault();
                }

            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
            }

            return null;
        }

    }

}