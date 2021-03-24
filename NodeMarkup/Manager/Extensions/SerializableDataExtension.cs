using ICities;
using ModsCommon.Utilities;
using NodeMarkup.Manager;
using System;
using System.Diagnostics;

namespace NodeMarkup.Utilities
{
    public class SerializableDataExtension : SerializableDataExtensionBase
    {
        public override void OnLoadData()
        {
            Mod.Logger.Debug($"Start load map data");

            if (serializableDataManager.LoadData(Loader.Id) is byte[] data)
            {
                try
                {
                    var sw = Stopwatch.StartNew();

                    var decompress = Loader.Decompress(data);
#if DEBUG
                    Mod.Logger.Debug(decompress);
#endif
                    var config = XmlExtension.Parse(decompress);
                    MarkupManager.FromXml(config, new ObjectsMap());

                    sw.Stop();
                    Mod.Logger.Debug($"Map data was loaded in {sw.ElapsedMilliseconds}ms; Size = {data.Length} bytes");
                }
                catch (Exception error)
                {
                    Mod.Logger.Error("Could not load map data", error);
                    MarkupManager.SetFiled();
                }
            }
            else
                Mod.Logger.Debug($"Saved map data not founded");
        }
        public override void OnSaveData()
        {
            Mod.Logger.Debug($"Start save map data");

            string config = string.Empty;
            try
            {
                var sw = Stopwatch.StartNew();
                config = Loader.GetString(MarkupManager.ToXml());
#if DEBUG
                Mod.Logger.Debug(config);
#endif
                var compress = Loader.Compress(config);
                serializableDataManager.SaveData(Loader.Id, compress);

                sw.Stop();
                Mod.Logger.Debug($"Map data saved in {sw.ElapsedMilliseconds}ms; Size = {compress.Length} bytes");
            }
            catch (Exception error)
            {
                Mod.Logger.Error("Save map data failed", error);
                Loader.SaveToFile(Loader.MarkingName, config, out _);
                throw;
            }
        }
    }
}
