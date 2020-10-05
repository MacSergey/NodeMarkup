using ICities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NodeMarkup
{
    public class AssetDataExtension : AssetDataExtensionBase
    {
        public override void OnAssetLoaded(string name, object asset, Dictionary<string, byte[]> userData)
        {
            base.OnAssetLoaded(name, asset, userData);
        }
        public override void OnAssetSaved(string name, object asset, out Dictionary<string, byte[]> userData)
        {
            base.OnAssetSaved(name, asset, out userData);
        }
        public override void OnCreated(IAssetData assetData)
        {
            base.OnCreated(assetData);
        }
    }
}
