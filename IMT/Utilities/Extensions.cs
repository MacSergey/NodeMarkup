using ColossalFramework.Globalization;
using IMT.Manager;
using IMT.UI;
using ModsCommon;
using ModsCommon.Utilities;
using System;
using System.ComponentModel;
using Alignment = IMT.Manager.Alignment;

namespace IMT.Utilities
{
    public static class Utilities
    {
        public static string Description<T>(this T value) where T : Enum => value.Description<T, Mod>();
        public static string Description(this StyleModifier modifier)
        {
            if (modifier.GetAttr<DescriptionAttribute, StyleModifier>() is DescriptionAttribute description)
                return Localize.LocaleManager.GetString(description.Description, Localize.Culture);
            else if (modifier.GetAttr<InputKeyAttribute, StyleModifier>() is InputKeyAttribute inputKey)
                return LocalizeExtension.GetModifiers(inputKey.Control, inputKey.Alt, inputKey.Shift);
            else
                return modifier.ToString();
        }
        public static NetworkType GetNetworkType<T>(this T value) where T : Enum => value.GetAttr<NetworkTypeAttribute, T>()?.Type ?? NetworkType.All;
        public static LineType GetLineType<T>(this T value) where T : Enum => value.GetAttr<LineTypeAttribute, T>()?.Type ?? LineType.All;

        public static Alignment Invert(this Alignment alignment) => (Alignment)(1 - alignment.Sign());
        public static int Sign(this Alignment alignment) => (int)alignment - 1;

        public static Style.StyleType GetGroup(this Style.StyleType type) => type & Style.StyleType.GroupMask;
        public static Style.StyleType GetItem(this Style.StyleType type) => type & Style.StyleType.ItemMask;

        public static string GetPrefabName(PropInfo prop)
        {
            if (prop == null)
                return string.Empty;
            else if (Locale.Exists("PROPS_TITLE", prop.name))
                return Locale.Get("PROPS_TITLE", prop.name);
            else
                return prop.name;
        }
        public static string GetPrefabName(TreeInfo tree)
        {
            if (tree == null)
                return string.Empty;
            else if (Locale.Exists("TREE_TITLE", tree.name))
                return Locale.Get("TREE_TITLE", tree.name);
            else
                return tree.name;
        }
        public static string GetPrefabName(NetInfo network)
        {
            if (network == null)
                return string.Empty;
            else if (Locale.Exists("NET_TITLE", network.name))
                return Locale.Get("NET_TITLE", network.name);
            else
                return network.name;
        }
    }
    public class NotExistEnterException : Exception
    {
        public EntranceType Type { get; }
        public ushort Id { get; }

        public NotExistEnterException(EntranceType type, ushort id) : base(string.Empty)
        {
            Type = type;
            Id = id;
        }
    }
    public class NotExistItemException : Exception
    {
        public MarkingType Type { get; }
        public ushort Id { get; }

        public NotExistItemException(MarkingType type, ushort id) : base(string.Empty)
        {
            Type = type;
            Id = id;
        }
    }
}
