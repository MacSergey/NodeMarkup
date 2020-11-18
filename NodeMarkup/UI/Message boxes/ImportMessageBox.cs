using ColossalFramework.UI;
using ModsCommon.UI;
using NodeMarkup.UI.Editors;
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
        protected abstract string Caption { get; }
        protected abstract string SuccessMessage { get; }
        protected abstract string FailedMessage { get; }

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
        private void ImportClick()
        {
            var result = Import(DropDown.SelectedObject);

            var resultMessageBox = ShowModal<OkMessageBox>();
            resultMessageBox.CaprionText = Caption;
            resultMessageBox.MessageText = result ? SuccessMessage : FailedMessage;

            Close();
        }
        protected abstract bool Import(string file);

        protected virtual void CancelClick() => Close();

        public class FileDropDown : UIDropDown<string> { }
    }
    public class ImportMarkingMessageBox : ImportMessageBox
    {
        protected override string Caption => NodeMarkup.Localize.Settings_RestoreMarkingCaption;
        protected override string SuccessMessage => NodeMarkup.Localize.Settings_RestoreMarkingMessageSuccess;
        protected override string FailedMessage => NodeMarkup.Localize.Settings_RestoreMarkingMessageFailed;
        protected override Dictionary<string, string> GetList() => Loader.GetMarkingRestoreList();
        protected override bool Import(string file) => Loader.ImportMarkingData(file);
    }
    public class ImportStyleTemplatesMessageBox : ImportMessageBox
    {
        protected override string Caption => NodeMarkup.Localize.Settings_RestoreTemplatesCaption;
        protected override string SuccessMessage => NodeMarkup.Localize.Settings_RestoreTemplatesMessageSuccess;
        protected override string FailedMessage => NodeMarkup.Localize.Settings_RestoreTemplatesMessageFailed;
        protected override Dictionary<string, string> GetList() => Loader.GetStyleTemplatesRestoreList();
        protected override bool Import(string file) =>  Loader.ImportStylesData(file);
    }
    public class ImportIntersectionTemplatesMessageBox : ImportMessageBox
    {
        protected override string Caption => NodeMarkup.Localize.Settings_RestorePresetsCaption;
        protected override string SuccessMessage => NodeMarkup.Localize.Settings_RestorePresetsMessageSuccess;
        protected override string FailedMessage => NodeMarkup.Localize.Settings_RestorePresetsMessageFailed;
        protected override Dictionary<string, string> GetList() => Loader.GetIntersectionTemplatesRestoreList();
        protected override bool Import(string file) => Loader.ImportIntersectionsData(file);

    }
}
