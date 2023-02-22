using IMT.Manager;
using ModsCommon;
using ModsCommon.Utilities;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;

namespace IMT.Utilities
{
    public class SerializableDataExtension : BaseSerializableDataExtension<SerializableDataExtension, Mod>
    {
        protected override string Id => Loader.Id;
        private string UsedPrefabsId => $"{Id}UsedPrefabs";
        protected override XElement GetSaveData() => MarkingManager.ToXml();
        protected override void SetLoadData(XElement config) => MarkingManager.FromXml(config, new ObjectsMap(), false);
        protected override void OnLoadFailed() => MarkingManager.SetFailed();

        protected override void OnSaveSucces()
        {
            SingletonMod<Mod>.Logger.Debug($"Start saving used prefabs");

            var networks = new HashSet<string>();
            var props = new HashSet<string>();
            var trees = new HashSet<string>();
            MarkingManager.GetUsedAssets(networks, props, trees);

            var config = new XElement("IMT");

            foreach (var network in networks)
            {
                if (!string.IsNullOrEmpty(network))
                {
                    var networkData = new XElement("N");
                    networkData.AddAttr("Id", network);
                    config.Add(networkData);
                }
            }

            foreach (var prop in props)
            {
                if (!string.IsNullOrEmpty(prop))
                {
                    var propData = new XElement("P");
                    propData.AddAttr("Id", prop);
                    config.Add(propData);
                }
            }

            foreach (var tree in trees)
            {
                if (!string.IsNullOrEmpty(tree))
                {
                    var treeData = new XElement("T");
                    treeData.AddAttr("Id", tree);
                    config.Add(treeData);
                }
            }

            var configString  = Loader.GetString(config);
            var data = Encoding.UTF8.GetBytes(configString);

            serializableDataManager.SaveData(UsedPrefabsId, data);

            SingletonMod<Mod>.Logger.Debug($"{networks.Count} networks, {props.Count} props, {trees.Count} trees are used");
        }
        protected override void OnSaveFailed(string config)
        {
            Loader.SaveToFile(Loader.MarkingName, config, out _);
            serializableDataManager.EraseData(UsedPrefabsId);
        }

        public void SetUsedPrefabs(HashSet<string> networks, HashSet<string> props, HashSet<string> trees)
        {
            if (serializableDataManager.LoadData(UsedPrefabsId) is byte[] data)
            {
                SingletonMod<Mod>.Logger.Debug($"Start setting used prefabs to LSM");

                var configString = Encoding.UTF8.GetString(data);
                var config = XmlExtension.Parse(configString);

                var networkCount = 0;
                var propCount = 0;
                var treeCount = 0;
                foreach(var item in config.Elements())
                {
                    if(item.Name == "N")
                    {
                        if (item.TryGetAttrValue<string>("Id", out var id))
                        {
                            networks.Add(id);
                            networkCount += 1;
                        }
                    }
                    else if(item.Name == "P")
                    {
                        if (item.TryGetAttrValue<string>("Id", out var id))
                        {
                            props.Add(id);
                            propCount += 1;
                        }
                    }
                    else if (item.Name == "T")
                    {
                        if (item.TryGetAttrValue<string>("Id", out var id))
                        {
                            trees.Add(id);
                            treeCount += 1;
                        }
                    }
                }
                SingletonMod<Mod>.Logger.Debug($"{networkCount} networks, {propCount} props, {treeCount} trees are used");
            }
            else
            {
                SingletonMod<Mod>.Logger.Debug($"Used prefab data is not found");
            }
        }
    }
}
