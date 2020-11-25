using ColossalFramework;
using ModsCommon.Utils;
using IMT.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace IMT.Utils
{
    public class NodeMarkupShortcut : Shortcut
    {
        public override string Label => Localize.ResourceManager.GetString(LabelKey, Localize.Culture);
        public ToolModeType ModeType { get; }
        public NodeMarkupShortcut(string name, string labelKey, InputKey key, Action action = null, ToolModeType modeType = ToolModeType.MakeItem) : base(Settings.SettingsFile, name, labelKey, key, action)
        {
            ModeType = modeType;
        }
        public override bool IsPressed(Event e) => (NodeMarkupTool.Instance.ModeType & ModeType) != ToolModeType.None ? base.IsPressed(e) : false;
        public override string ToString() => InputKey.ToLocalizedString("KEYNAME");
    }
}
