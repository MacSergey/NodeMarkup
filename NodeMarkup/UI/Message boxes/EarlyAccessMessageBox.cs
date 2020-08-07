using ColossalFramework.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NodeMarkup.UI
{
    public class EarlyAccessMessageBox : TwoButtonMessageBox
    {
        public string Key { get; private set; }
        public EarlyAccessMessageBox()
        {
            CaprionText = NodeMarkup.Localize.EarlyAccess_KeyActivationCaption;
            MessageText = NodeMarkup.Localize.EarlyAccess_KeyActivationMessage;
            Button1Text = NodeMarkup.Localize.EarlyAccess_KeyActivateButton;
            Button2Text = NodeMarkup.Localize.EarlyAccess_KeyCancelButton;
        }
        protected override void FillContent()
        {
            base.FillContent();

            var helper = new UIHelper(ScrollableContent);
            AddAccessId(helper);
            AddAccessKey(helper);
        }

        private void AddAccessId(UIHelper group)
        {
            var accessIdField = default(UITextField);
            var process = false;
            accessIdField = group.AddTextfield(NodeMarkup.Localize.EarlyAccess_ID, EarlyAccess.Id, Set, Set) as UITextField;
            accessIdField.width = 400;

            void Set(string text)
            {
                if (!process)
                {
                    process = true;
                    accessIdField.text = EarlyAccess.Id;
                    process = false;
                }
            }
        }
        private void AddAccessKey(UIHelper group)
        {
            var accessKeyField = group.AddTextfield(NodeMarkup.Localize.EarlyAccess_Key, string.Empty, OnChanged, OnChanged) as UITextField;
            accessKeyField.width = 400;

            void OnChanged(string key) =>  Key = key;
        }
    }
}
