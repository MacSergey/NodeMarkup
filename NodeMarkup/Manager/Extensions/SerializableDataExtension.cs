using ICities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IMT.Manager;
using System.Xml.Linq;
using System.Diagnostics;
using System.IO;
using System.Xml;
using System.IO.Compression;
using ColossalFramework.IO;
using System.Linq.Expressions;
using IMT.UI;
using HarmonyLib;
using ColossalFramework.Globalization;

namespace IMT.Utils
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
                    var config = Loader.Parse(decompress);
                    MarkupManager.FromXml(config, new ObjectsMap());

                    sw.Stop();
                    Mod.Logger.Debug($"Map data was loaded in {sw.ElapsedMilliseconds}ms; Size = {data.Length} bytes");
                }
                catch(Exception error)
                {
                    Mod.Logger.Error("Could not load map data", error);
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
            catch(Exception error)
            {
                Mod.Logger.Error("Save map data failed", error);
                Loader.SaveToFile(Loader.MarkingName, config, out _);
                throw;
            }
        }
    }
}
