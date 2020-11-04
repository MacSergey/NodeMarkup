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
        where TemplateType : Template<TemplateType>
        where Group : EditableGroup<GroupType, Item, TemplateType, Icon>
        where HeaderPanelType : TemplateHeaderPanel<TemplateType>
    {
        protected override bool UseGroupPanel => true;

        protected bool EditMode { get; private set; }

        protected StringPropertyPanel NameProperty { get; set; }
        protected HeaderPanelType HeaderPanel { get; set; }
        protected abstract string RewriteCaption { get; }
        protected abstract string RewriteMessage { get; }

        private EditorItem[] Aditional { get; set; }

        public override bool Active 
        {
            set
            {
                base.Active = value;
                EditMode = false;
                if (SelectItem is Item)
                    SetEditable();
            }
        }
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

            Aditional = AddAditional().ToArray();

            if (EditObject.IsAsset)
                SetEditable();
        }
        protected override void OnClear()
        {
            NameProperty = null;
            HeaderPanel = null;
            Aditional = null;
        }
        protected virtual IEnumerable<EditorItem> AddAditional() { yield break; }

        protected override void OnObjectDelete(TemplateType template) => (template.Manager as TemplateManager<TemplateType>).DeleteTemplate(template);

        protected virtual void AddHeader()
        {
            HeaderPanel = ComponentPool.Get<HeaderPanelType>(PropertiesPanel);
            HeaderPanel.Init(EditObject);
            HeaderPanel.OnSaveAsset += SaveAsset;
            HeaderPanel.OnEdit += EditAsset;
        }

        private void AddAuthor()
        {
            if (EditObject.IsAsset)
            {
                var authorProperty = ComponentPool.Get<StringPropertyPanel>(PropertiesPanel);
                authorProperty.Text = NodeMarkup.Localize.TemplateEditor_Author;
                authorProperty.FieldWidth = 230;
                authorProperty.EnableControl = false;
                authorProperty.Init();
                authorProperty.Value = EditObject.Asset.Author;
            }
        }
        private void AddTemplateName()
        {
            NameProperty = ComponentPool.Get<StringPropertyPanel>(PropertiesPanel);
            NameProperty.Text = NodeMarkup.Localize.TemplateEditor_Name;
            NameProperty.FieldWidth = 230;
            NameProperty.SubmitOnFocusLost = false;
            NameProperty.Init();
            NameProperty.Value = EditObject.Name;
            NameProperty.OnValueChanged += NameSubmitted;
        }


        private void NameSubmitted(string name)
        {
            if (name == EditObject.Name)
                return;

            var messageBox = default(YesNoMessageBox);
            if (!string.IsNullOrEmpty(name) && (EditObject.Manager as TemplateManager<TemplateType>).ContainsName(name, EditObject))
            {
                messageBox = MessageBoxBase.ShowModal<YesNoMessageBox>();
                messageBox.CaprionText = NodeMarkup.Localize.TemplateEditor_NameExistCaption;
                messageBox.MessageText = string.Format(NodeMarkup.Localize.TemplateEditor_NameExistMessage, name);
                messageBox.OnButton1Click = AgreeExistName;
                messageBox.OnButton2Click = NotSet;
            }
            else
                AgreeExistName();

            bool AgreeExistName()
            {
                if (EditObject.IsAsset)
                {
                    messageBox ??= MessageBoxBase.ShowModal<YesNoMessageBox>();
                    messageBox.CaprionText = RewriteCaption;
                    messageBox.MessageText = RewriteMessage;
                    messageBox.OnButton1Click = Set;
                    messageBox.OnButton2Click = NotSet;
                    return false;
                }
                else
                    return Set();
            }

            bool Set()
            {
                EditObject.Name = name;
                SelectItem.Refresh();
                return true;
            }

            bool NotSet()
            {
                NameProperty.Value = EditObject.Name;
                NameProperty.Edit();
                return true;
            }
        }

        protected virtual void SetEditable()
        {
            NameProperty.EnableControl = EditMode;

            foreach (var aditional in Aditional)
                aditional.EnableControl = EditMode;
        }

        private void SaveAsset()
        {
            if (TemplateManager<TemplateType>.Instance.MakeAsset(EditObject))
            {
                SelectItem.Init(EditObject);
                ItemClick(SelectItem);
            }
        }

        
        protected virtual void EditAsset()
        {
            EditMode = !EditMode;
            SetEditable();
            Panel.AvailableHeader = Panel.AvailableTabStrip = AvailableItems = !EditMode;
        }
    }
}
