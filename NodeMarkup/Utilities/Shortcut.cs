using ColossalFramework;
using ModsCommon;
using ModsCommon.Utilities;
using NodeMarkup.Tools;
using System;
using UnityEngine;

namespace NodeMarkup.Utilities
{
    public class NodeMarkupShortcut : Shortcut
    {
        public override string Label => Localize.ResourceManager.GetString(LabelKey, Localize.Culture);
        public ToolModeType ModeType { get; }
        public NodeMarkupShortcut(string name, string labelKey, InputKey key, Action action = null, ToolModeType modeType = ToolModeType.MakeItem) : base(Settings.SettingsFile, name, labelKey, key, action)
        {
            ModeType = modeType;
        }
        public override bool Press(Event e) => (SingletonTool<NodeMarkupTool>.Instance.CurrentMode & ModeType) != ToolModeType.None && base.Press(e);
        public override string ToString() => InputKey.ToLocalizedString("KEYNAME");
    }
}
