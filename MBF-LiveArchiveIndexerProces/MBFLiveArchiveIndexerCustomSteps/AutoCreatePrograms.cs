using MediaButler.WorkflowStep;
using Microsoft.WindowsAzure.MediaServices.Client;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MBFLiveArchiveIndexerCustomSteps
{
    class AutoCreatePrograms : ICustomStepExecution
    {
        CloudStorageAccount storageAccount;
        CloudBlobClient blobClient;
        CloudBlobContainer container;
         bool swLoopControl;
         CloudMediaContext _MediaServicesContext;
        ICustomRequest myRequest;
         int ArchiveWindowLength = 5*60 +3;
        int overLapSec = 30;
        private void Information(string txt)
        {
            Trace.TraceInformation("[{1}]{2} {0}", txt,myRequest.ProcessTypeId,myRequest.ProcessInstanceId);
        }
        private IProgram CreateAndStartProgram(IChannel channel, string ProgramlName, string AssetlName, int Seconds)
        {
            IAsset asset = _MediaServicesContext.Assets.Create(AssetlName, AssetCreationOptions.None);
            IProgram program = channel.Programs.Create(ProgramlName, TimeSpan.FromSeconds(Seconds), asset.Id);
            Information(string.Format("Created program {0}", program.Name));
            program.Start();

            Information(string.Format("Starting Program {0}", program.Name));
            return program;
        }
        private bool CheckSw()
        {
            CloudBlockBlob blockBlob = container.GetBlockBlobReference("track/" + myRequest.MediaAccountName);
            return blockBlob.Exists();
        }
        private void Start()
        {
            int pNumeber = 0;
            swLoopControl = true;
            _MediaServicesContext = new CloudMediaContext(myRequest.MediaAccountName, myRequest.MediaAccountKey);
            while (swLoopControl)
            {

                var myRunningchannel = _MediaServicesContext.Channels;
                foreach (IChannel channel in myRunningchannel)
                {
                    if (channel.State == ChannelState.Running)
                    {
                        if (channel.Programs.Where(p => p.ArchiveWindowLength == TimeSpan.FromSeconds(ArchiveWindowLength)).Count() == 0)
                        {
                            var theName = channel.Name + Guid.NewGuid().ToString();
                            Information("Creating first auto program");
                            CreateAndStartProgram(channel, theName, theName, ArchiveWindowLength);
                        }
                        //Check
                        var currentPrograms = channel.Programs;
                        foreach (var theOldProgram in currentPrograms)
                        {
                            
                            var x = (ArchiveWindowLength - overLapSec);
                            var y = (DateTime.UtcNow - theOldProgram.Created).TotalSeconds;
                            Information(string.Format("Reviwing Program {0} created  at {1} / {2}[sec] of lmit {3} [sec]", theOldProgram.Name, theOldProgram.Created, y, x));
                            if ((theOldProgram.ArchiveWindowLength == TimeSpan.FromSeconds(ArchiveWindowLength)) && (y >= x) && (theOldProgram.State == ProgramState.Running))
                            {
                                Information(string.Format("Program {0} to old",theOldProgram.Name));
                                var theName = channel.Name + "-" + myRequest.ProcessInstanceId + "-" + pNumeber;
                                var newProgram = CreateAndStartProgram(channel, theName, theName, ArchiveWindowLength);
                                Information(string.Format("Created Program {0}  {1}", newProgram.Name, newProgram.Created));
                                theOldProgram.Stop();
                                Information(string.Format("Stoped Program {0}  {1}", theOldProgram.Name, theOldProgram.Created));
                                pNumeber += 1;
                            }
                            else
                            {
                                Information(string.Format("Program {0} created {1} ok", theOldProgram.Name, theOldProgram.Created));
                            }

                        }
                    }
                }


                Information("Waiting for 30 seconds");
                DateTime start = DateTime.Now;
                do
                {
                    swLoopControl = CheckSw();
                    System.Threading.Thread.Sleep(2 * 1000);
                } while ((swLoopControl) && ((DateTime.Now-start).TotalSeconds<=30));

                swLoopControl = CheckSw();
            }
            Information("Finish Stop");
        }
        private void Stop()
        {
            //lease
            swLoopControl = false;
            CloudBlockBlob blockBlob = container.GetBlockBlobReference("track/" + myRequest.MediaAccountName);
            blockBlob.Delete();
        }
        public bool execute(ICustomRequest request)
        {
            myRequest = request;
             storageAccount = CloudStorageAccount.Parse(myRequest.ProcessConfigConn);
             blobClient = storageAccount.CreateCloudBlobClient();
             container = blobClient.GetContainerReference(myRequest.ProcessTypeId);
            if (CheckSw())
            {
                Trace.TraceWarning("Another Process Running, abort");
                Information("Start Stop");
                Stop();
                
            }
            else
            {
                Information("Start process");
                CloudBlockBlob blockBlob = container.GetBlockBlobReference("track/" + request.MediaAccountName);
                blockBlob.UploadText(request.ProcessInstanceId);
                Start();
            }
                return true;

        }
    }
}
