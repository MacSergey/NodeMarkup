using ColossalFramework.UI;
using NodeMarkup.Manager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NodeMarkup.UI.Editors
{
    public abstract class BaseTemplateEditor<Item, TemplateType, Icon, Group, GroupType, HeaderPanelType> : GroupedEditor<Item, TemplateType, Icon, Group, GroupType>
        where Item : EditableItem<TemplateType, Icon>
        where Icon : UIComponent
        where TemplateType : Template
        where Group : EditableGroup<GroupType, Item, TemplateType, Icon>
        where HeaderPanelType : TemplateHeaderPanel
    {
        protected StringPropertyPanel NameProperty { get; set; }
        protected HeaderPanelType HeaderPanel { get; set; }

        protected abstract IEnumerable<TemplateType> GetTemplates();

        protected override void FillItems()
        {
            foreach (var templates in GetTemplates())
                AddItem(templates);
        }

        protected override void OnObjectSelect()
        {
            AddHeader();
            AddAuthor();
            AddTemplateName();
        }
        protected override void OnObjectDelete(TemplateType template) => (template.Manager as TemplateManager<TemplateType>).DeleteTemplate(template);

        protected virtual void AddHeader()
        {
            HeaderPanel = ComponentPool.Get<HeaderPanelType>(SettingsPanel);
            HeaderPanel.Init(EditObject);
            HeaderPanel.OnSaveAsset += SaveAsset;
        }
        private void AddAuthor()
        {
            if (EditObject.IsAsset)
            {
                var authorProperty = ComponentPool.Get<StringPropertyPanel>(SettingsPanel);
                authorProperty.Text = "Author";
                authorProperty.FieldWidth = 230;
                authorProperty.UseWheel = false;
                authorProperty.Init();
                authorProperty.Value = EditObject.Asset.Author;
            }
        }
        private void AddTemplateName()
        {
            NameProperty = ComponentPool.Get<StringPropertyPanel>(SettingsPanel);
            NameProperty.Text = NodeMarkup.Localize.TemplateEditor_Name;
            NameProperty.FieldWidth = 230;
            NameProperty.UseWheel = false;
            NameProperty.Init();
            NameProperty.Value = EditObject.Name;
            NameProperty.OnValueChanged += NameSubmitted;
        }


        private void NameSubmitted(string name)
        {
            if (name == EditObject.Name)
                return;

            if (!string.IsNullOrEmpty(name) && (EditObject.Manager as TemplateManager<TemplateType>).ContainsName(name, EditObject))
            {
                var messageBox = MessageBoxBase.ShowModal<YesNoMessageBox>();
                messageBox.CaprionText = NodeMarkup.Localize.TemplateEditor_NameExistCaption;
                messageBox.MessageText = string.Format(NodeMarkup.Localize.TemplateEditor_NameExistMessage, name);
                messageBox.OnButton1Click = Set;
                messageBox.OnButton2Click = NotSet;
            }
            else
                Set();

            bool Set()
            {
                EditObject.Name = name;
                SelectItem.Refresh();
                return true;
            }
            bool NotSet()
            {
                NameProperty.Edit();
                return true;
            }
        }


        private void SaveAsset()
        {
            if (TemplateManager.MakeAsset(EditObject))
            {
                SelectItem.Init(EditObject);
                ItemClick(SelectItem);
            }
        }
    }
}
