using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediaButler.WorkflowStep;
using Microsoft.WindowsAzure.MediaServices.Client;

namespace MBFLiveArchiveIndexerCustomSteps
{
    
    class ArchiveTopBitrateStep : ICustomStepExecution
    {

        private void CopyCaptions(IAsset myAssetFrom, IAsset myAssetTo)
        {
            foreach (var assetFile in myAssetFrom.AssetFiles)
            {
                if (assetFile.Name.EndsWith(".ttml") || assetFile.Name.EndsWith(".vtt"))
                {
                    string magicName = assetFile.Name;
                    assetFile.Download(magicName);
                    IAssetFile newFile = myAssetTo.AssetFiles.Create(assetFile.Name);
                    newFile.Upload(magicName);
                    System.IO.File.Delete(magicName);
                    newFile.Update();
                }
            }
            myAssetTo.Update();
        }
        public bool execute(ICustomRequest request)
        {
            var _MediaServicesContext = new CloudMediaContext(request.MediaAccountName, request.MediaAccountKey);
            IAsset asset = (from m in _MediaServicesContext.Assets select m).Where(m => m.Id == request.AssetId).FirstOrDefault();
            CopyCaptions(asset, asset.ParentAssets.FirstOrDefault());
            request.AssetId = asset.ParentAssets.FirstOrDefault().Id;
            asset.Delete();
            return true;
        }
    }
}
