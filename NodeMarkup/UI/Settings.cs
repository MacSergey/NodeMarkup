using ColossalFramework;
using ColossalFramework.PlatformServices;
using ColossalFramework.UI;
using ICities;
using NodeMarkup.Manager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NodeMarkup.UI
{
    public static class Settings
    {
        public static string SettingsFile => $"{Mod.StaticName}{nameof(SettingsFile)}";

        public static SavedFloat RenderDistance { get; } = new SavedFloat(nameof(RenderDistance), SettingsFile, 300f, true);
        public static SavedBool ShowToolTip { get; } = new SavedBool(nameof(ShowToolTip), SettingsFile, true, true);
        public static SavedString Templates { get; } = new SavedString(nameof(Templates), SettingsFile, string.Empty, true);

        static Settings()
        {
            if (GameSettings.FindSettingsFileByName(SettingsFile) == null)
                GameSettings.AddSettingsFile(new SettingsFile[] { new SettingsFile() { fileName = SettingsFile } });
        }

        public static void OnSettingsUI(UIHelperBase helper)
        {
            AddKeyMapping(helper);
            AddGeneral(helper);
            AddOther(helper);
        }
        private static void AddKeyMapping(UIHelperBase helper)
        {
            UIHelper group = helper.AddGroup("Shortcuts") as UIHelper;
            UIPanel panel = group.self as UIPanel;

            var keymappings = panel.gameObject.AddComponent<KeymappingsPanel>();
            keymappings.AddKeymapping("Activate tool", NodeMarkupTool.ActivationShortcut);
            keymappings.AddKeymapping("Delete all node lines", NodeMarkupTool.DeleteAllShortcut);
        }
        private static void AddGeneral(UIHelperBase helper)
        {
            UIHelper group = helper.AddGroup("General") as UIHelper;

            AddDistanceSetting(group);
            AddShowToolTipsSetting(group);
        }
        private static void AddDistanceSetting(UIHelper group)
        {
            UITextField distanceField = null;
            distanceField = group.AddTextfield("Render distance", RenderDistance.ToString(), OnDistanceChanged, OnDistanceSubmitted) as UITextField;

            void OnDistanceChanged(string distance) { }
            void OnDistanceSubmitted(string text)
            {
                if (float.TryParse(text, out float distance))
                {
                    if (distance < 0)
                        distance = 300;

                    RenderDistance.value = distance;
                    distanceField.text = distance.ToString();
                }
                else
                {
                    distanceField.text = RenderDistance.ToString();
                }
            }
        }
        private static void AddShowToolTipsSetting(UIHelper group)
        {
            var showCheckBox = group.AddCheckbox("Show tooltips", true, OnShowToolTipsChanged) as UICheckBox;

            void OnShowToolTipsChanged(bool show) => ShowToolTip.value = show;
        }
        private static void AddOther(UIHelperBase helper)
        {
            UIHelper group = helper.AddGroup("Other") as UIHelper;

            AddDeleteAll(group);
        }
        private static void AddDeleteAll(UIHelper group)
        {
            var button = group.AddButton("Delete marking from all intersections", Click) as UIButton;
            button.textColor = Color.red;

            void Click()
            {
                MarkupManager.DeleteAll();
            }
        }
    }
}
