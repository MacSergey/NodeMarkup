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
        protected override XElement GetSaveData() => MarkupManager.ToXml();
        protected override void SetLoadData(XElement config) => MarkupManager.FromXml(config, new ObjectsMap());
        protected override void OnLoadFailed() => MarkupManager.SetFailed();
        protected override void OnSaveFailed(string config) => Loader.SaveToFile(Loader.MarkingName, config, out _);
    }
}
