using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SensorKitSDK
{
    public class AzureUploader
    {
        CloudBlobContainer container = null;
        const string AZURE_STORAGE_ACCOUNT_NAME =  "<ADD YOUR AZURE STORAGE ACCOUNT NAME>";
        const string AZURE_STORAGE_ACCOUNT_KEY =  "< ADD YOUR AZURE STORAGE ACCOUNT KEY>";
        const string AZURE_STORAGE_CONTAINER =  "<ADD YOUR AZURE STORAGE CONTAINER NAME>";


        public AzureUploader(){
            var account = new CloudStorageAccount(new StorageCredentials(AZURE_STORAGE_ACCOUNT_NAME, AZURE_STORAGE_ACCOUNT_KEY), true);
            // Create the blob client.
            var blobClient = account.CreateCloudBlobClient();
            // Retrieve reference to a previously created container.
            container = blobClient.GetContainerReference(AZURE_STORAGE_CONTAINER);
        }

        public async Task<bool> UploadFileAsync(string name, string path)
        {
            try
            {
                if (container != null)
                {
                    // Retrieve reference to a blob named "myblob".
                    var blockBlob = container.GetBlockBlobReference(name);
                    using (var stream = File.OpenRead(path))
                    {
                        await blockBlob.UploadFromStreamAsync(stream);
                        return true;
                    }
                }
            }
            catch (Exception x)
            {
                Debug.WriteLine(x);
            }
            return false;
        }

        

    }
}