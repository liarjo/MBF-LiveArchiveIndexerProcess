using System.Net;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

static string StorageConnectionString = "[your string conn here]";
static string containerName = "[Origing repo contariner name here ]";
static string targetContainerName = "[Stage porcess container name here]";
static string mp4ProfileName = "[your mp4 profile name hre, example testSet/livearchiveindexer/ArchiveTopBitrate.json]";
static string IndexProfileName = "[your indexer profile name here, example: testSet/livearchiveindexer/AzureMediaIndexer1.xml]";

private static string ReadParameter(HttpRequestMessage req, string keyName)
{
    return req.GetQueryNameValuePairs().FirstOrDefault(q => string.Compare(q.Key, keyName, true) == 0).Value;
}
static void blobCopy(string containerName, string targetContainerName, string blobName, string TargetBlobName)
{
    CloudStorageAccount storageAccount = CloudStorageAccount.Parse(StorageConnectionString);
    CloudBlobClient cloudBlobClient = storageAccount.CreateCloudBlobClient();
    CloudBlobContainer sourceContainer = cloudBlobClient.GetContainerReference(containerName);
    CloudBlobContainer targetContainer = cloudBlobClient.GetContainerReference(targetContainerName);
    CloudBlockBlob sourceBlob = sourceContainer.GetBlockBlobReference(blobName);
    CloudBlockBlob targetBlob = targetContainer.GetBlockBlobReference(TargetBlobName);
    targetBlob.StartCopy(sourceBlob);
}

static void TriggerBMF(string AssteName, string ProcessInstanceID)
{
    // Retrieve storage account from connection string.
    CloudStorageAccount storageAccount = CloudStorageAccount.Parse(StorageConnectionString);
    // Create the blob client.
    CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
    // Retrieve reference to a previously created container.
    CloudBlobContainer container = blobClient.GetContainerReference(targetContainerName);
    // Retrieve reference to a blob named "myblob".
    CloudBlockBlob blockBlob = container.GetBlockBlobReference("Incoming/" + ProcessInstanceID + "/" + ProcessInstanceID + ".control");
    string jsonControlBase = "{  \"SelectAssetBy.Type\": \"assetname\",  \"SelectAssetBy.Value\": \"" + AssteName + "\",  \"GridEncodeStep.encodeConfigList\": [ \"ArchiveTopBitrate.json\" ],  \"GridEncodeStep.MediaProcessorName\": \"Media Encoder Standard\",  \"Index2Preview.encodeConfigList\": [ \"AzureMediaIndexer1.xml\" ],  \"Index2Preview.MediaProcessorName\": \"Azure Media Indexer\",  \"Index2Preview.CopySubTitles\": \"yes\"}";
    blockBlob.UploadText(jsonControlBase);
}
public static async Task<HttpResponseMessage> Run(HttpRequestMessage req, TraceWriter log)
{
    log.Info($"C# HTTP trigger function processed a request. RequestUri={req.RequestUri}");

    string assetname = ReadParameter(req, "assetname");
    var ProcessInstanceId = Guid.NewGuid().ToString();
    log.Info($"ProcessID: {ProcessInstanceId}");
    //Copy MP$ encoding profile
    blobCopy(containerName, targetContainerName, mp4ProfileName, "Incoming/" + ProcessInstanceId + "/ArchiveTopBitrate.json");
    //Copy Indexer encoding profile
    blobCopy(containerName, targetContainerName, IndexProfileName, "Incoming/" + ProcessInstanceId + "/AzureMediaIndexer1.xml");
    //Copy DotControl  file, proces trigger
    TriggerBMF(assetname, ProcessInstanceId);

    return assetname == null
        ? req.CreateResponse(HttpStatusCode.BadRequest, "Please pass a assetname on the query string or in the request body")
        : req.CreateResponse(HttpStatusCode.OK, "Hello " + assetname);
}