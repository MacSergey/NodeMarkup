using IMT.Manager;
using ModsCommon.Utilities;
using System.Xml.Linq;

namespace IMT.Utilities
{
    public class SerializableDataExtension : BaseSerializableDataExtension<SerializableDataExtension, Mod>
    {
        protected override string Id => Loader.Id;
        protected override XElement GetSaveData() => MarkingManager.ToXml();
        protected override void SetLoadData(XElement config) => MarkingManager.FromXml(config, new ObjectsMap(), false);
        protected override void OnLoadFailed() => MarkingManager.SetFailed();
        protected override void OnSaveFailed(string config) => Loader.SaveToFile(Loader.MarkingName, config, out _);
    }
}
