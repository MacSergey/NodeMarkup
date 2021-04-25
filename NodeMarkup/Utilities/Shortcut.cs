using ColossalFramework;
using ModsCommon;
using ModsCommon.Utilities;
using NodeMarkup.Tools;
using System;
using UnityEngine;

namespace NodeMarkup.Utilities
{
    public class NodeMarkupShortcut : BaseShortcut<Mod>
    {
        public ToolModeType ModeType { get; }
        public NodeMarkupShortcut(string name, string labelKey, InputKey key, Action action = null, ToolModeType modeType = ToolModeType.MakeItem) : base(name, labelKey, key, action)
        {
            ModeType = modeType;
        }
        public override bool Press(Event e) => (SingletonTool<NodeMarkupTool>.Instance.CurrentMode & ModeType) != ToolModeType.None && base.Press(e);
    }
}
