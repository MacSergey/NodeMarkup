using ColossalFramework.UI;
using ModsCommon;
using ModsCommon.UI;
using System.Collections.Generic;
using System.Linq;
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
        protected StringDropDown DropDown { get; set; }
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
            DropDown = Panel.Content.AddUIComponent<StringDropDown>();
            ComponentStyle.DropDownMessageBoxStyle(DropDown, new Vector2(DefaultWidth - 2 * Padding, 38));
            DropDown.EntityTextScale = 1f;

            DropDown.textScale = 1.25f;
            DropDown.OnSelectObject += DropDownValueChanged;

            var files = GetList();
            foreach (var file in files)
                DropDown.AddItem(file.Key, file.Value);

            DropDown.SelectedObject = files.FirstOrDefault().Key;
            DropDown.OnSetPopupStyle += SetPopupStyle;
            DropDown.OnSetEntityStyle += SetEntityStyle;

            DropDownValueChanged(DropDown.SelectedObject);
        }

        private void SetPopupStyle(StringDropDown.StringPopup popup, ref bool overridden)
        {
            popup.PopupSettingsStyle<DropDownItem<string>, StringDropDown.StringEntity, StringDropDown.StringPopup>();
            overridden = true;
        }
        private void SetEntityStyle(StringDropDown.StringEntity entity, ref bool overridden)
        {
            entity.EntitySettingsStyle<DropDownItem<string>, StringDropDown.StringEntity>();
            overridden = true;
        }

        private void DropDownValueChanged(string obj)
        {
            if (!string.IsNullOrEmpty(obj))
                ImportButton.Enable();
            else
                ImportButton.Disable();
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
