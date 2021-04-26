using ColossalFramework;
using ColossalFramework.UI;
using HarmonyLib;
using ICities;
using ModsCommon.Utilities;
using NodeMarkup.Manager;
using NodeMarkup.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;

namespace NodeMarkup
{
    public class AssetDataExtension : BaseIntersectionAssetDataExtension<Mod, AssetDataExtension, Utilities.ObjectsMap>
    {
        protected override string DataId { get; } = $"{Loader.Id}.Data";
        protected override string MapId { get; } = $"{Loader.Id}.Map";

        protected override Utilities.ObjectsMap CreateMap(bool isSimple) => new Utilities.ObjectsMap(isSimple: isSimple);
        protected override XElement GetConfig() => MarkupManager.ToXml();

        protected override void PlaceAsset(XElement config, Utilities.ObjectsMap map) => MarkupManager.FromXml(config, map, false);
    }
}
