
# MBF-LiveArchiveIndexerProcess
### Introduction

[image here]
### MBF-LiveArchiveIndexerProcess Process
The process flow is:

1. Start
2. Select Asset program by Name
2. Encode to generaate single MP4
3. Extract captions using Azure Media Services Index from the single Mp4
4. Insert text captions on Azure Search and index it
4. Copy captions to the original program and delete temporal assets
5. Send notification mail with Program information
6. Finish

## How to deploy MBF-LiveArchiveIndexerProcess


### Setup pre requisites
* Microsoft Azure Subscription
* Azure Media Services Account
* Azure Search service
* Azure Function App
* Sendgrid service
* Media Butler Framework deployed, you can see how to [here](http://aka.ms/mediabutlerframework) 

### Deploy MBF-LiveArchiveIndexerProcess

#### Process configuration on ButlerConfiguration
####A. Add process definition:

a. PartitionKey: **MediaButler.Common.workflow.ProcessHandler**

b.  RowKey: **livearchiveindexer.ChainConfig**

c.  ConfigurationValue (json process definition):

```json
[
  {
    "AssemblyName": "MediaButler.BaseProcess.dll",
    "TypeName": "MediaButler.BaseProcess.MessageHiddeControlStep",
    "ConfigKey": ""
  },

  {
    "AssemblyName": "MediaButler.BaseProcess.dll",
    "TypeName": "MediaButler.BaseProcess.SelectAssetByStep",
    "ConfigKey": ""
  },

  {
    "AssemblyName": "MediaButler.BaseProcess.dll",
    "TypeName": "MediaButler.BaseProcess.GridEncodeStep",
    "ConfigKey": ""
  },
  {
    "AssemblyName": "MediaButler.BaseProcess.dll",
    "TypeName": "MediaButler.BaseProcess.MediaAnalytics.AzureMediaIndexer2Step",
    "ConfigKey": ""
  },
  {
    "AssemblyName": "MediaButler.BaseProcess.dll",
    "TypeName": "MediaButler.BaseProcess.Control.MediaButlerCustomStep",
    "ConfigKey": "ArchiveTopBitrateStep"
  },
  {
    "AssemblyName": "MediaButler.BaseProcess.dll",
    "TypeName": "MediaButler.BaseProcess.AzureSearch.InjectTTML",
    "ConfigKey": ""
  },
  {
    "AssemblyName": "MediaButler.BaseProcess.dll",
    "TypeName": "MediaButler.BaseProcess.SendGridStep",
    "ConfigKey": ""
  },
  {
    "AssemblyName": "MediaButler.BaseProcess.dll",
    "TypeName": "MediaButler.BaseProcess.DeleteOriginalBlobStep",
    "ConfigKey": ""
  },
  {
    "AssemblyName": "MediaButler.BaseProcess.dll",
    "TypeName": "MediaButler.BaseProcess.MessageHiddeControlStep",
    "ConfigKey": ""
  }
]
```
#### B. Add process Configuration
a. PartitionKey: **MediaButler.Common.workflow.ProcessHandler**

b.  RowKey: **livearchiveindexer.config**

c.  ConfigurationValue (json process configuration):
```json
{
  "SendGridStep.Config": "{\"UserName\":\"[your Sengrid User]\", \"Pswd\":\"[your Password Sengrid]\", \"To\":\"[TO mail address]\", \"FromName\":\"MBF Notification\", \"FromMail\":\"[From mail address]\"}",
  "Index2Preview.CopySubTitles": "yes",
  "InjectTTML.searchServiceName": "[Azure Search Service Name]",
  "InjectTTML.adminApiKey": "[Azure Search Key]",
  "InjectTTML.indexName": "[Azure Search Ibdex Name]"
}
```
#### C. Add Custom Step configuration
a. PartitionKey: **MediaButler.Common.workflow.ProcessHandler**

b.  RowKey: **ArchiveTopBitrateStep.StepConfig**

c.  ConfigurationValue (json Custom Step configuration):
```json
{
  "AssemblyName": "MBFLiveArchiveIndexerCustomSteps.dll",
  "TypeName": "MBFLiveArchiveIndexerCustomSteps.ArchiveTopBitrateStep"
}
```
#### D. Upload Custom Step DLL
a. Upload MBFLiveArchiveIndexerCustomSteps.dll (This C# project Binary ) to MBF blob stoarge in **mediabutlerbin** container 

#### E. Create Staging storage container for this process, create container **livearchiveindexer** on MBF stoarge

#### F. Upload transcoding profile definitions
a. Upload **ArchiveTopBitrate.json** to **livearchiveindexer** container

b. Upload **AAzureMediaIndexer1.xml** to **livearchiveindexer** container

#### G. Update  on **ButlerConfiguration** table, the container list register.  It is the
register with PartitionKey **MediaButler.Workflow.WorkerRole** and
RowKey **ContainersToScan**. You need to add to the list container name
**livearchiveindexer**. For example, the value could be:

testbasicprocess,livearchiveindexer

#### H. Create New Azure C# HttpTriggerCSharp  Function
a. New Funciton with name **livearchiveindexer** and autorization level Function

b. On View Files option, delete run.csx

c. Upload files **run.csx** and **project.json**

d. On run.csx Update  variables StorageConnectionString, containerName, targetContainerName, mp4ProfileName and IndexProfileName. For example

```cs
static string StorageConnectionString = "DefaultEndpointsProtocol=https;AccountName=XXXXXX;AccountKey=XXXXXXXXXXX";
static string containerName = "livearchiveindexer";
static string targetContainerName = "livearchiveindexer";
static string mp4ProfileName = "ArchiveTopBitrate.json";
static string IndexProfileName = "AzureMediaIndexer1.xml";
```
#### I. Restart MBF WebJobs.

### Test MBF-LiveArchiveIndexerProcess
To test MBF-LiveArchiveIndexerProcess you should call the HTTP endpoint using your Azure Function URL like this 
https://[yourDNS].azurewebsites.net/api/livearchiveindexer?code=[yourKey]& assetname=[your program name]
**it is important to have the archived program ready before try this MBF process.**

Reporting issues and feedback
-----------------------------
If you encounter any bugs with the tool please file an issue in the Issues section of our GitHub repo.

License
------------
MIT
