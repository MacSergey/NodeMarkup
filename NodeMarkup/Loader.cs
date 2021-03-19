using ColossalFramework.Globalization;
using ColossalFramework.Importers;
using ColossalFramework.IO;
using ColossalFramework.Packaging;
using HarmonyLib;
using ModsCommon.Utilities;
using NodeMarkup.Manager;
using NodeMarkup.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using UnityEngine;

namespace NodeMarkup
{
    public interface IXml
    {
        string XmlSection { get; }
    }
    public interface IToXml : IXml
    {
        XElement ToXml();
    }
    public interface IFromXml : IXml
    {
        void FromXml(XElement config);
    }
    public static class Loader
    {
        public static string Id { get; } = nameof(NodeMarkup);
        public static string GetString(XElement config) => config.ToString(SaveOptions.DisableFormatting);

        public static byte[] Compress(string xml)
        {
            var buffer = Encoding.UTF8.GetBytes(xml);

            using var outStream = new MemoryStream();
            using (var zipStream = new GZipStream(outStream, CompressionMode.Compress))
            {
                zipStream.Write(buffer, 0, buffer.Length);
            }
            var compresed = outStream.ToArray();
            return compresed;
        }

        public static string Decompress(byte[] data)
        {
            using var inStream = new MemoryStream(data);
            using var zipStream = new GZipStream(inStream, CompressionMode.Decompress);
            using var outStream = new MemoryStream();
            byte[] buffer = new byte[1000000];
            int readed;
            while ((readed = zipStream.Read(buffer, 0, buffer.Length)) > 0)
            {
                outStream.Write(buffer, 0, readed);
            }

            var decompressed = outStream.ToArray();
            var xml = Encoding.UTF8.GetString(decompressed);
            return xml;
        }

        public static string GetSaveName()
        {
            var lastSaveField = AccessTools.Field(typeof(SavePanel), "m_LastSaveName");
            var lastSave = lastSaveField.GetValue(null) as string;
            if (string.IsNullOrEmpty(lastSave))
                lastSave = Locale.Get("DEFAULTSAVENAMES", "NewSave");

            return lastSave;
        }
        public static string MarkingRecovery => nameof(MarkingRecovery);
        public static string TemplatesRecovery => nameof(TemplatesRecovery);
        public static string PresetsRecovery => nameof(PresetsRecovery);

        public static string MarkingName => $"{MarkingRecovery}.{GetSaveName()}";
        private static Regex MarkingRegex { get; } = new Regex(@$"{MarkingRecovery}\.(?<name>.+)\.(?<date>\d+)");
        private static Regex StyleTemplatesRegex { get; } = new Regex(@$"{TemplatesRecovery}\.(?<date>\d+)");
        private static Regex IntersectionTemplatesRegex { get; } = new Regex(@$"{PresetsRecovery}\.(?<date>\d+)");
        private static string RecoveryDirectory => Path.Combine(Directory.GetCurrentDirectory(), "IntersectionMarkingTool");
        private static string ScreenshotDirectory => Path.Combine(RecoveryDirectory, "TemplateScreenshots");

        public static Dictionary<string, string> GetMarkingRestoreList()
        {
            var files = GetRestoreList($"{MarkingRecovery}*.xml");
            var result = new Dictionary<string, string>();
            foreach (var file in files)
            {
                var match = MarkingRegex.Match(file);
                if (!match.Success)
                    continue;

                var date = new DateTime(long.Parse(match.Groups["date"].Value));
                result[file] = $"{match.Groups["name"].Value} {date}";
            }
            return result;
        }
        public static Dictionary<string, string> GetStyleTemplatesRestoreList() => GetTemplatesRestoreList(TemplatesRecovery, StyleTemplatesRegex);
        public static Dictionary<string, string> GetIntersectionTemplatesRestoreList() => GetTemplatesRestoreList(PresetsRecovery, IntersectionTemplatesRegex);

        private static Dictionary<string, string> GetTemplatesRestoreList(string name, Regex regex)
        {
            var files = GetRestoreList($"{name}*.xml");
            var result = new Dictionary<string, string>();
            foreach (var file in files)
            {
                var match = regex.Match(file);
                if (!match.Success)
                    continue;

                var date = new DateTime(long.Parse(match.Groups["date"].Value));
                result[file] = date.ToString();
            }
            return result;
        }
        private static string[] GetRestoreList(string pattern)
        {
            var files = Directory.GetFiles(RecoveryDirectory, pattern);
            return files;
        }

        public static bool ImportMarkingData(string file)
        {
            Mod.Logger.Debug($"Import marking data");
            return ImportData(file, (config) => MarkupManager.Import(config));
        }
        public static bool ImportStylesData(string file)
        {
            Mod.Logger.Debug($"Import styles data");
            return ImportTemplatesData(file, TemplateManager.StyleManager);
        }
        public static bool ImportIntersectionsData(string file)
        {
            Mod.Logger.Debug($"Import intersections data");
            return ImportTemplatesData(file, TemplateManager.IntersectionManager);
        }
        private static bool ImportTemplatesData(string file, TemplateManager manager)
        {
            return ImportData(file, (config) =>
            {
                manager.Saved.value = GetString(config);
                manager.Load();
            });
        }

        private static bool ImportData(string file, Action<XElement> processData)
        {
            Mod.Logger.Debug($"Import data");

            try
            {
                using var fileStream = File.OpenRead(file);
                using var reader = new StreamReader(fileStream);
                var xml = reader.ReadToEnd();
                var config = XmlExtension.Parse(xml);

                processData(config);

                Mod.Logger.Debug($"Data was imported");

                return true;
            }
            catch (Exception error)
            {
                Mod.Logger.Error("Could not import data", error);
                return false;
            }
        }
        public static bool DumpMarkingData(out string path)
        {
            Mod.Logger.Debug($"Dump marking data");
            return DumpData(GetString(MarkupManager.ToXml()), MarkingName, out path);
        }
        public static bool DumpStyleTemplatesData(out string path)
        {
            Mod.Logger.Debug($"Dump style templates data");
            return DumpData(Settings.Templates, TemplatesRecovery, out path);
        }
        public static bool DumpIntersectionTemplatesData(out string path)
        {
            Mod.Logger.Debug($"Dump intersection templates data");
            return DumpData(Settings.Intersections, PresetsRecovery, out path);
        }

        private static bool DumpData(string data, string name, out string path)
        {
            Mod.Logger.Debug($"Dump data");

            try
            {
                return SaveToFile(name, data, out path);
            }
            catch (Exception error)
            {
                Mod.Logger.Error("Save dump failed", error);

                path = string.Empty;
                return false;
            }
        }

        public static bool SaveToFile(string name, string xml, out string file)
        {
            Mod.Logger.Debug($"Save to file");
            try
            {
                if (!Directory.Exists(RecoveryDirectory))
                    Directory.CreateDirectory(RecoveryDirectory);

                file = Path.Combine(RecoveryDirectory, $"{name}.{DateTime.Now.Ticks}.xml");
                using (var fileStream = File.Create(file))
                using (var writer = new StreamWriter(fileStream))
                {
                    writer.Write(xml);
                }
                Mod.Logger.Debug($"Dump saved {file}");
                return true;
            }
            catch (Exception error)
            {
                Mod.Logger.Error("Save dump failed", error);

                file = string.Empty;
                return false;
            }
        }

        public static void LoadTemplateAsset(GameObject gameObject, Package.Asset asset)
        {
            if (!(gameObject.GetComponent<MarkingInfo>() is MarkingInfo markingInfo))
                return;

            Mod.Logger.Debug($"Start load template asset \"{asset.fullName}\" from {asset.package.packagePath}");
            try
            {
                var templateConfig = XmlExtension.Parse(markingInfo.data);
                if (TemplateAsset.FromPackage(templateConfig, asset, out TemplateAsset templateAsset))
                {
                    templateAsset.Template.Manager.AddTemplate(templateAsset.Template);
                    Mod.Logger.Debug($"Template asset loaded: {templateAsset} ({templateAsset.Flags})");
                }
                else
                    Mod.Logger.Error($"Could not load template asset");
            }
            catch (Exception error)
            {
                Mod.Logger.Error($"Could not load template asset", error);
            }
        }
        public static bool SaveTemplateAsset(TemplateAsset templateAsset)
        {
            Mod.Logger.Debug($"Start save template asset {templateAsset}");
            try
            {
                var meta = new CustomAssetMetaData()
                {
                    name = $"{templateAsset.Template.Name}_{Guid.NewGuid().Unique()}",
                    timeStamp = DateTime.Now,
                    type = CustomAssetMetaData.Type.Unknown,
                    dlcMask = SteamHelper.DLC_BitMask.None,
                    steamTags = new string[] { "Marking" },
                    guid = templateAsset.Template.Id.ToString(),
                };

                var package = new Package(templateAsset.IsWorkshop ? templateAsset.WorkshopId.ToString() : meta.name)
                {
                    packageMainAsset = meta.name,
                    packageAuthor = $"steamid:{TemplateManager.UserId}",
                };

                var gameObject = new GameObject(typeof(MarkingInfo).Name);
                var markingInfo = gameObject.AddComponent<MarkingInfo>();
                markingInfo.data = GetString(templateAsset.Template.ToXml());
                meta.assetRef = package.AddAsset($"{meta.name}_Data", markingInfo.gameObject);

                if (templateAsset.Preview is Image image)
                    meta.imageRef = package.AddAsset(templateAsset.MetaPreview, image, false, Image.BufferFileFormat.PNG, false, false);

                if (templateAsset.SeparatePreview && templateAsset.SteamPreview is Image steamImage)
                    meta.steamPreviewRef = package.AddAsset(templateAsset.MetaSteamPreview, steamImage, false, Image.BufferFileFormat.PNG, false, false);
                else
                    meta.steamPreviewRef = meta.imageRef;

                package.AddAsset(meta.name, meta, UserAssetType.CustomAssetMetaData);

                var path = Path.Combine(DataLocation.assetsPath, PathUtils.AddExtension(PathEscaper.Escape(templateAsset.FileName), PackageManager.packageExtension));
                package.Save(path);

                Mod.Logger.Debug($"Template asset saved to {path}");

                return true;
            }
            catch (Exception error)
            {
                Mod.Logger.Error($"Could not save template asset", error);
                return false;
            }
        }
        public static bool SaveScreenshot(Image image, Guid id)
        {
            try
            {
                var data = image.GetFormattedImage(Image.BufferFileFormat.PNG);
                var path = Path.Combine(ScreenshotDirectory, $"{id}.png");
                File.WriteAllBytes(path, data);
                return true;
            }
            catch (Exception error)
            {
                Mod.Logger.Error($"Could not save screenshot {id}", error);
                return false;
            }
        }
        public static bool LoadScreenshot(Guid id, out Image image)
        {
            try
            {
                if (!Directory.Exists(ScreenshotDirectory))
                    Directory.CreateDirectory(ScreenshotDirectory);

                var path = Path.Combine(ScreenshotDirectory, $"{id}.png");
                var data = File.ReadAllBytes(path);
                image = new Image(data);
                return true;
            }
            catch (Exception error)
            {
                Mod.Logger.Error($"Could not load screenshot {id}", error);
                image = null;
                return false;
            }

        }
    }
}
