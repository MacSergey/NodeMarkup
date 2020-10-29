using ColossalFramework.UI;
using NodeMarkup.UI.Editors;
using NodeMarkup.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace NodeMarkup.UI
{
    public abstract class ImportMessageBox : SimpleMessageBox
    {
        private UIButton ImportButton { get; set; }
        private UIButton CancelButton { get; set; }
        protected FileDropDown DropDown { get; set; }
        public ImportMessageBox()
        {
            ImportButton = AddButton(1, 2, ImportClick);
            ImportButton.text = NodeMarkup.Localize.Settings_Restore;
            ImportButton.Disable();
            CancelButton = AddButton(2, 2, CancelClick);
            CancelButton.text = NodeMarkup.Localize.Settings_Cancel;

            AddFileList();
        }
        private void AddFileList()
        {
            DropDown = ScrollableContent.AddUIComponent<FileDropDown>();
            DropDown.SetSettingsStyle(new Vector2(Width - 2 * Padding, 38));

            DropDown.listWidth = (int)DropDown.width;
            DropDown.listHeight = 200;
            DropDown.itemPadding = new RectOffset(14, 14, 0, 0);
            DropDown.textScale = 1.25f;
            DropDown.clampListToScreen = true;
            DropDown.eventSelectedIndexChanged += DropDownIndexChanged;

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
            foreach (var file in GetList())
                DropDown.AddItem(file.Key, file.Value);
        }
        protected abstract Dictionary<string, string> GetList();
        protected abstract void ImportClick();

        protected virtual void CancelClick() => Cancel();

        public class FileDropDown : UIDropDown<string> { }
    }
    public class ImportMarkingMessageBox : ImportMessageBox
    {
        protected override Dictionary<string, string> GetList() => Loader.GetMarkingRestoreList();
        protected override void ImportClick()
        {
            var result = Loader.ImportMarkingData(DropDown.SelectedObject);

            var resultMessageBox = ShowModal<OkMessageBox>();
            resultMessageBox.CaprionText = NodeMarkup.Localize.Settings_RestoreMarkingCaption;
            resultMessageBox.MessageText = result ? NodeMarkup.Localize.Settings_RestoreMarkingMessageSuccess : NodeMarkup.Localize.Settings_RestoreMarkingMessageFailed;

            Cancel();
        }
    }
    public class ImportTemplatesMessageBox : ImportMessageBox
    {
        protected override Dictionary<string, string> GetList() => Loader.GetTemplatesRestoreList();
        protected override void ImportClick()
        {
            var result = Loader.ImportStylesData(DropDown.SelectedObject);

            var resultMessageBox = ShowModal<OkMessageBox>();
            resultMessageBox.CaprionText = NodeMarkup.Localize.Settings_RestoreTemplatesCaption;
            resultMessageBox.MessageText = result ? NodeMarkup.Localize.Settings_RestoreTemplatesMessageSuccess : NodeMarkup.Localize.Settings_RestoreTemplatesMessageFailed;

            Cancel();
        }
    }
}
