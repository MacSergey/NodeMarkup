using ICities;
using ModsCommon;
using ModsCommon.Utilities;
using NodeMarkup.Manager;
using System;
using System.Diagnostics;
using System.Xml.Linq;

namespace NodeMarkup.Utilities
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
