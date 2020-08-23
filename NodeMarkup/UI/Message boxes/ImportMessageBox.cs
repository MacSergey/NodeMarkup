using ColossalFramework.UI;
using NodeMarkup.UI.Editors;
using NodeMarkup.Utils;
using System;
using System.Text.RegularExpressions;
using UnityEngine;

namespace NodeMarkup.UI
{
    public class ImportMessageBox : SimpleMessageBox
    {
        private static Regex Regex { get; } = new Regex(@"MarkingRecovery\.(?<name>.+)\.(?<date>\d+)");

        private UIButton ImportButton { get; set; }
        private UIButton CancelButton { get; set; }
        private FileDropDown DropDown { get; set; }
        public ImportMessageBox()
        {
            ImportButton = AddButton(1, 2, ImportClick);
            ImportButton.text = NodeMarkup.Localize.Settings_Import;
            ImportButton.Disable();
            CancelButton = AddButton(2, 2, CancelClick);
            CancelButton.text = NodeMarkup.Localize.Setting_Cancel;

            AddFileList();
        }
        private void AddFileList()
        {
            DropDown = ScrollableContent.AddUIComponent<FileDropDown>();

            DropDown.atlas = TextureUtil.InGameAtlas;
            DropDown.height = 38;
            DropDown.width = Width - 2 * Padding;
            DropDown.listBackground = "OptionsDropboxListbox";
            DropDown.itemHeight = 24;
            DropDown.itemHover = "ListItemHover";
            DropDown.itemHighlight = "ListItemHighlight";
            DropDown.normalBgSprite = "OptionsDropbox";
            DropDown.hoveredBgSprite = "OptionsDropboxHovered";
            DropDown.focusedBgSprite = "OptionsDropboxFocused";
            DropDown.listWidth = (int)DropDown.width;
            DropDown.listHeight = 200;
            DropDown.listPosition = UIDropDown.PopupListPosition.Below;
            DropDown.clampListToScreen = true;
            DropDown.foregroundSpriteMode = UIForegroundSpriteMode.Stretch;
            DropDown.popupTextColor = new Color32(170, 170, 170, 255);
            DropDown.textScale = 1.25f;
            DropDown.textFieldPadding = new RectOffset(14, 40, 7, 0);
            DropDown.verticalAlignment = UIVerticalAlignment.Middle;
            DropDown.horizontalAlignment = UIHorizontalAlignment.Center;
            DropDown.itemPadding = new RectOffset(14, 14, 0, 0);
            DropDown.verticalAlignment = UIVerticalAlignment.Middle;
            DropDown.eventSelectedIndexChanged += DropDownIndexChanged;

            DropDown.triggerButton = DropDown;

            AddData();
            DropDown.selectedIndex = 0;
        }

        private void DropDownIndexChanged(UIComponent component, int value)
        {
            if (DropDown.SelectedObject != null)
                ImportButton.Enable();
            else
                ImportButton.Disable();
        }

        private void AddData()
        {
            foreach (var file in Serializer.GetImportList())
            {
                var match = Regex.Match(file);
                if (!match.Success)
                    continue;
                var date = new DateTime(long.Parse(match.Groups["date"].Value));
                DropDown.AddItem(file, $"{match.Groups["name"].Value} {date}");
            }
        }

        protected virtual void ImportClick()
        {
            var result = Serializer.OnImportData(DropDown.SelectedObject);

            var resultMessageBox = ShowModal<OkMessageBox>();
            resultMessageBox.CaprionText = NodeMarkup.Localize.Settings_ImportMarkingCaption;
            resultMessageBox.MessageText = result ? NodeMarkup.Localize.Settings_ImportMarkingMessageSuccess : NodeMarkup.Localize.Settings_ImportMarkingMessageFailed;

            Cancel();
        }
        protected virtual void CancelClick()
        {
            Cancel();
        }

        class FileDropDown : CustomUIDropDown<string> { }
    }
}
