﻿using ColossalFramework.UI;
using ModsCommon;
using ModsCommon.UI;
using System.Collections.Generic;
using UnityEngine;

namespace IMT.UI
{
    public abstract class ImportMessageBox : SimpleMessageBox
    {
        protected abstract string Caption { get; }
        protected abstract string SuccessMessage { get; }
        protected abstract string FailedMessage { get; }

        private CustomUIButton ImportButton { get; set; }
        private CustomUIButton CancelButton { get; set; }
        protected FileDropDown DropDown { get; set; }
        public ImportMessageBox()
        {
            ImportButton = AddButton(ImportClick);
            ImportButton.text = IMT.Localize.Settings_Restore;
            ImportButton.Disable();
            CancelButton = AddButton(CancelClick);
            CancelButton.text = CommonLocalize.Settings_Cancel;

            AddFileList();
        }
        private void AddFileList()
        {
            DropDown = Panel.Content.AddUIComponent<FileDropDown>();
            ComponentStyle.CustomSettingsStyle(DropDown, new Vector2(DefaultWidth - 2 * Padding, 38));

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

            var resultMessageBox = MessageBox.Show<OkMessageBox>();
            resultMessageBox.CaptionText = Caption;
            resultMessageBox.MessageText = result ? SuccessMessage : FailedMessage;

            Close();
        }
        protected abstract bool Import(string file);

        protected virtual void CancelClick() => Close();

        public class FileDropDown : UIDropDown<string> { }
    }
    public class ImportMarkingMessageBox : ImportMessageBox
    {
        protected override string Caption => IMT.Localize.Settings_RestoreMarkingCaption;
        protected override string SuccessMessage => IMT.Localize.Settings_RestoreMarkingMessageSuccess;
        protected override string FailedMessage => IMT.Localize.Settings_RestoreMarkingMessageFailed;
        protected override Dictionary<string, string> GetList() => Loader.GetMarkingRestoreList();
        protected override bool Import(string file) => Loader.ImportMarkingData(file);
    }
    public class ImportStyleTemplatesMessageBox : ImportMessageBox
    {
        protected override string Caption => IMT.Localize.Settings_RestoreTemplatesCaption;
        protected override string SuccessMessage => IMT.Localize.Settings_RestoreTemplatesMessageSuccess;
        protected override string FailedMessage => IMT.Localize.Settings_RestoreTemplatesMessageFailed;
        protected override Dictionary<string, string> GetList() => Loader.GetStyleTemplatesRestoreList();
        protected override bool Import(string file) => Loader.ImportStylesData(file);
    }
    public class ImportIntersectionTemplatesMessageBox : ImportMessageBox
    {
        protected override string Caption => IMT.Localize.Settings_RestorePresetsCaption;
        protected override string SuccessMessage => IMT.Localize.Settings_RestorePresetsMessageSuccess;
        protected override string FailedMessage => IMT.Localize.Settings_RestorePresetsMessageFailed;
        protected override Dictionary<string, string> GetList() => Loader.GetIntersectionTemplatesRestoreList();
        protected override bool Import(string file) => Loader.ImportIntersectionsData(file);

    }
}
