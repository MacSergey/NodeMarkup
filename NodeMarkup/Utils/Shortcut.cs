using ColossalFramework;
using NodeMarkup.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NodeMarkup.Utils
{
    public class Shortcut
    {
        private string LabelKey { get; }
        public string Label => Localize.ResourceManager.GetString(LabelKey, Localize.Culture);
        public SavedInputKey InputKey { get; }
        public ToolModeType ModeType { get; }
        private Action Action { get; }
        public Shortcut(string name, string labelKey, InputKey key, Action action = null, ToolModeType modeType = ToolModeType.MakeItem)
        {
            LabelKey = labelKey;
            InputKey = new SavedInputKey(name, UI.Settings.SettingsFile, key, true);
            ModeType = modeType;
            Action = action;
        }

        public bool IsPressed(Event e)
        {
            if (InputKey.IsPressed(e) && (NodeMarkupTool.Instance.ModeType & ModeType) != ToolModeType.None)
            {
                Press();
                return true;
            }
            else
                return false;
        }
        public void Press() => Action?.Invoke();

        public override string ToString() => InputKey.ToLocalizedString("KEYNAME");
    }
}
