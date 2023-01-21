using ColossalFramework.Importers;
using ColossalFramework.IO;
using ColossalFramework.Packaging;
using IMT.Manager;
using ModsCommon;
using ModsCommon.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using UnityEngine;

namespace IMT
{
    public abstract class Loader : ModsCommon.Utilities.Loader
    {
        public static string Id { get; } = "NodeMarkup";

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
                result[file] = $"{match.Groups["name"].Value} {date.ToString(SingletonMod<Mod>.Instance.Culture)}";
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
                result[file] = date.ToString(SingletonMod<Mod>.Instance.Culture);
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
            SingletonMod<Mod>.Logger.Debug($"Import marking data");
            return ImportData(file, (config) => MarkingManager.Import(config));
        }
        public static bool ImportStylesData(string file)
        {
            SingletonMod<Mod>.Logger.Debug($"Import styles data");
            return ImportTemplatesData(file, SingletonManager<StyleTemplateManager>.Instance);
        }
        public static bool ImportIntersectionsData(string file)
        {
            SingletonMod<Mod>.Logger.Debug($"Import intersections data");
            return ImportTemplatesData(file, SingletonManager<IntersectionTemplateManager>.Instance);
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
            SingletonMod<Mod>.Logger.Debug($"Import data");

            try
            {
                using var fileStream = File.OpenRead(file);
                using var reader = new StreamReader(fileStream);
                var xml = reader.ReadToEnd();
                var config = XmlExtension.Parse(xml);

                processData(config);

                SingletonMod<Mod>.Logger.Debug($"Data was imported");

                return true;
            }
            catch (Exception error)
            {
                SingletonMod<Mod>.Logger.Error("Could not import data", error);
                return false;
            }
        }
        public static bool DumpMarkingData(out string path)
        {
            SingletonMod<Mod>.Logger.Debug($"Dump marking data");
            return DumpData(GetString(MarkingManager.ToXml()), MarkingName, out path);
        }
        public static bool DumpStyleTemplatesData(out string path)
        {
            SingletonMod<Mod>.Logger.Debug($"Dump style templates data");
            return DumpData(Settings.Templates, TemplatesRecovery, out path);
        }
        public static bool DumpIntersectionTemplatesData(out string path)
        {
            SingletonMod<Mod>.Logger.Debug($"Dump intersection templates data");
            return DumpData(Settings.Intersections, PresetsRecovery, out path);
        }

        private static bool DumpData(string data, string name, out string path)
        {
            SingletonMod<Mod>.Logger.Debug($"Dump data");

            try
            {
                return SaveToFile(name, data, out path);
            }
            catch (Exception error)
            {
                SingletonMod<Mod>.Logger.Error("Save dump failed", error);

                path = string.Empty;
                return false;
            }
        }

        public static bool SaveToFile(string name, string xml, out string file)
        {
            SingletonMod<Mod>.Logger.Debug($"Save to file");
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
                SingletonMod<Mod>.Logger.Debug($"Dump saved {file}");
                return true;
            }
            catch (Exception error)
            {
                SingletonMod<Mod>.Logger.Error("Save dump failed", error);

                file = string.Empty;
                return false;
            }
        }

        public static void LoadTemplateAsset(IMT.Manager.MarkingInfo markingInfo, Package.Asset asset)
        {
            SingletonMod<Mod>.Logger.Debug($"Start load template asset \"{asset.fullName}\" from {asset.package.packagePath}");
            try
            {
                var templateConfig = XmlExtension.Parse(markingInfo.data);
                if (TemplateAsset.FromPackage(templateConfig, asset, out TemplateAsset templateAsset))
                {
                    templateAsset.Template.Manager.AddTemplate(templateAsset.Template);
                    SingletonMod<Mod>.Logger.Debug($"Template asset loaded: {templateAsset} ({templateAsset.Flags})");
                }
                else
                    SingletonMod<Mod>.Logger.Error($"Could not load template asset");
            }
            catch (Exception error)
            {
                SingletonMod<Mod>.Logger.Error($"Could not load template asset", error);
            }
        }
        public static bool SaveTemplateAsset(TemplateAsset templateAsset)
        {
            SingletonMod<Mod>.Logger.Debug($"Start save template asset {templateAsset}");
            try
            {
                var meta = new CustomAssetMetaData()
                {
                    name = $"{templateAsset.Template.Name}_{Guid.NewGuid().Unique()}",
                    timeStamp = DateTime.Now,
                    type = CustomAssetMetaData.Type.Unknown,
                    steamTags = new string[] { "Marking" },
                    guid = templateAsset.Template.Id.ToString(),
                };

                var package = new Package(templateAsset.IsWorkshop ? templateAsset.WorkshopId.ToString() : meta.name)
                {
                    packageMainAsset = meta.name,
                    packageAuthor = $"steamid:{TemplateManager.UserId}",
                };

                var gameObject = new GameObject(typeof(IMT.Manager.MarkingInfo).Name);
                var markingInfo = gameObject.AddComponent<IMT.Manager.MarkingInfo>();
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

                SingletonMod<Mod>.Logger.Debug($"Template asset saved to {path}");

                return true;
            }
            catch (Exception error)
            {
                SingletonMod<Mod>.Logger.Error($"Could not save template asset", error);
                return false;
            }
        }
        public static bool SaveScreenshot(IntersectionTemplate template, Image image)
        {
            try
            {
                if (!Directory.Exists(ScreenshotDirectory))
                    Directory.CreateDirectory(ScreenshotDirectory);

                var data = image.GetFormattedImage(Image.BufferFileFormat.PNG);
                var path = Path.Combine(ScreenshotDirectory, $"{template.Id}.png");
                File.WriteAllBytes(path, data);
                return true;
            }
            catch (Exception error)
            {
                SingletonMod<Mod>.Logger.Error($"Could not save screenshot for template \"{template}\"", error);
                return false;
            }
        }
        public static bool LoadScreenshot(IntersectionTemplate template, out Image image)
        {
            try
            {
                var path = Path.Combine(ScreenshotDirectory, $"{template.Id}.png");

                if (File.Exists(path))
                {
                    var data = File.ReadAllBytes(path);
                    image = new Image(data);
                    return true;
                }
                else
                {
                    SingletonMod<Mod>.Logger.Debug($"Could not load screenshot for template \"{template}\", file {path} not exist");
                    image = null;
                    return false;
                }
            }
            catch (Exception error)
            {
                SingletonMod<Mod>.Logger.Error($"Could not load screenshot for template \"{template}\"", error);
                image = null;
                return false;
            }
        }
    }
}
