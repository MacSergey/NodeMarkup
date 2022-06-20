using ColossalFramework.Globalization;
using ColossalFramework.PlatformServices;
using ModsCommon;
using ModsCommon.Utilities;
using NodeMarkup.Manager;
using NodeMarkup.UI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using UnityEngine;
using Alignment = NodeMarkup.Manager.Alignment;

namespace NodeMarkup.Utilities
{
    public static class Utilities
    {
        public static string Description<T>(this T value) where T : Enum => value.Description<T, Mod>();
        public static string Description(this StyleModifier modifier)
        {
            if (modifier.GetAttr<DescriptionAttribute, StyleModifier>() is DescriptionAttribute description)
                return Localize.ResourceManager.GetString(description.Description, Localize.Culture);
            else if (modifier.GetAttr<InputKeyAttribute, StyleModifier>() is InputKeyAttribute inputKey)
                return LocalizeExtension.GetModifiers(inputKey.Control, inputKey.Alt, inputKey.Shift);
            else
                return modifier.ToString();
        }
        public static NetworkType GetNetworkType<T>(this T value) where T : Enum => value.GetAttr<NetworkTypeAttribute, T>()?.Type ?? NetworkType.All;

        public static Alignment Invert(this Alignment alignment) => (Alignment)(1 - alignment.Sign());
        public static int Sign(this Alignment alignment) => (int)alignment - 1;

        public static LinkedListNode<T> GetPrevious<T>(this LinkedListNode<T> item) => item.Previous ?? item.List.Last;
        public static LinkedListNode<T> GetNext<T>(this LinkedListNode<T> item) => item.Next ?? item.List.First;

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
        public EnterType Type { get; }
        public ushort Id { get; }

        public NotExistEnterException(EnterType type, ushort id) : base(string.Empty)
        {
            Type = type;
            Id = id;
        }
    }
    public class NotExistItemException : Exception
    {
        public MarkupType Type { get; }
        public ushort Id { get; }

        public NotExistItemException(MarkupType type, ushort id) : base(string.Empty)
        {
            Type = type;
            Id = id;
        }
    }
}
