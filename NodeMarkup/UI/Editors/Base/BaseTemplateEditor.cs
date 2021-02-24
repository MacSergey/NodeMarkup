using ColossalFramework.UI;
using ModsCommon.UI;
using NodeMarkup.Manager;
using NodeMarkup.Tools;
using NodeMarkup.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NodeMarkup.UI.Editors
{
    public interface ITemplateEditor<ItemType> : IEditor<ItemType>
        where ItemType : Template
    {
        void Cancel();
        void EditName();
    }
    public abstract class BaseTemplateEditor<Item, TemplateType, Icon, Group, GroupType, HeaderPanelType, EditToolMode> : GroupedEditor<Item, TemplateType, Icon, Group, GroupType>, ITemplateEditor<TemplateType>
        where Item : EditableItem<TemplateType, Icon>
        where Icon : UIComponent
        where TemplateType : Template<TemplateType>
        where Group : EditableGroup<GroupType, Item, TemplateType, Icon>
        where HeaderPanelType : TemplateHeaderPanel<TemplateType>
        where EditToolMode : EditTemplateMode<TemplateType>
    {
        protected override bool UseGroupPanel => true;

        protected bool EditMode { get; private set; }
        protected bool HasChanges { get; set; }

        protected HeaderPanelType HeaderPanel { get; set; }
        private WarningTextProperty Warning { get; set; }
        protected StringPropertyPanel NameProperty { get; set; }

        protected abstract string IsAssetMessage { get; }
        protected abstract string RewriteCaption { get; }
        protected abstract string RewriteMessage { get; }
        protected abstract string SaveChangesMessage { get; }
        protected abstract string NameExistMessage { get; }
        protected abstract string IsAssetWarningMessage { get; }
        protected abstract string IsWorkshopWarningMessage { get; }

        private EditorItem[] Aditional { get; set; }

        public override bool Active
        {
            set
            {
                base.Active = value;
                EditMode = false;
                if(value && SelectItem is Item)
                    SetEditable();
            }
        }
        private EditToolMode ToolMode { get; }

        public BaseTemplateEditor()
        {
            ToolMode = Tool.CreateToolMode<EditToolMode>();
            ToolMode.Init(this);
        }

        protected abstract IEnumerable<TemplateType> GetTemplates();
        protected override void FillItems()
        {
            var templates = GetTemplates().ToArray();
            foreach (var template in templates)
                AddItem(template);
        }
        protected override void OnObjectSelect()
        {
            AddHeader();
            AddWarning();
            AddAuthor();
            AddTemplateName();

            ReloadAdditionalProperties();

            AddAdditional();

            SetEditable();
            SetEven();
        }
        private void ReloadAdditionalProperties()
        {
            if (Aditional != null)
            {
                foreach (var aditional in Aditional)
                    ComponentPool.Free(aditional);
            }

            Aditional = AddAditionalProperties().ToArray();
        }
        protected virtual void AddAdditional() { }
        protected override void OnClear()
        {
            HeaderPanel = null;
            Warning = null;
            NameProperty = null;
            Aditional = null;
        }
        protected virtual IEnumerable<EditorItem> AddAditionalProperties() { yield break; }

        protected override void OnObjectDelete(TemplateType template) => (template.Manager as TemplateManager<TemplateType>).DeleteTemplate(template);

        protected virtual void AddHeader()
        {
            HeaderPanel = ComponentPool.Get<HeaderPanelType>(PropertiesPanel);
            HeaderPanel.Init(EditObject);
            HeaderPanel.OnSaveAsset += SaveAsset;
            HeaderPanel.OnEdit += StartEditTemplate;
            HeaderPanel.OnSave += SaveChanges;
            HeaderPanel.OnNotSave += NotSaveChanges;
        }
        private void AddWarning()
        {
            Warning = ComponentPool.Get<WarningTextProperty>(PropertiesPanel);
            Warning.Text = $"{IsAssetMessage} {(EditObject.IsAsset && EditObject.Asset.CanEdit ? IsAssetWarningMessage : IsWorkshopWarningMessage)}";
            Warning.Init();
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
            NameProperty.SubmitOnFocusLost = true;
            NameProperty.Init();
            NameProperty.Value = EditObject.Name;
            NameProperty.OnValueChanged += (name) => OnChanged();
        }

        protected virtual void SetEditable()
        {
            Panel.Available = AvailableItems = !EditMode;
            HeaderPanel.EditMode = NameProperty.EnableControl = EditMode;
            Warning.isVisible = Settings.ShowPanelTip && EditObject.IsAsset && !EditMode;

            foreach (var aditional in Aditional)
                aditional.EnableControl = EditMode;
        }

        private void SaveAsset()
        {
            if (TemplateManager<TemplateType>.Instance.MakeAsset(EditObject))
            {
                SelectItem.Refresh();
                ItemClick(SelectItem);
            }
        }

        private void StartEditTemplate()
        {
            EditMode = true;
            HasChanges = false;
            SetEditable();
            Tool.SetMode(ToolMode);
        }
        public void EditName()
        {
            StartEditTemplate();
            NameProperty.Edit();
        }

        protected void OnChanged() => HasChanges = true;
        private void EndEditTemplate()
        {
            EditMode = false;
            HasChanges = false;
            SetEditable();
            Tool.SetDefaultMode();
        }

        private void SaveChanges()
        {
            var name = NameProperty.Value;
            var messageBox = default(YesNoMessageBox);
            if (!string.IsNullOrEmpty(name) && name != EditObject.Name && (EditObject.Manager as TemplateManager<TemplateType>).ContainsName(name, EditObject))
            {
                messageBox = MessageBoxBase.ShowModal<YesNoMessageBox>();
                messageBox.CaprionText = NodeMarkup.Localize.TemplateEditor_NameExistCaption;
                messageBox.MessageText = string.Format(NameExistMessage, name);
                messageBox.OnButton1Click = AgreeExistName;
                messageBox.OnButton2Click = EditName;
            }
            else
                AgreeExistName();


            bool AgreeExistName()
            {
                if (EditObject.IsAsset)
                {
                    messageBox ??= MessageBoxBase.ShowModal<YesNoMessageBox>();
                    messageBox.CaprionText = RewriteCaption;
                    messageBox.MessageText = $"{IsAssetMessage} {RewriteMessage}";
                    messageBox.OnButton1Click = Save;
                    return false;
                }
                else
                    return Save();
            }

            bool EditName()
            {
                NameProperty.Edit();
                return true;
            }

            bool Save()
            {
                OnApplyChanges();
                (EditObject.Manager as TemplateManager<TemplateType>).TemplateChanged(EditObject);
                EndEditTemplate();
                SelectItem.Refresh();
                return true;
            }
        }
        protected virtual void OnApplyChanges() => EditObject.Name = NameProperty.Value;

        private void NotSaveChanges()
        {
            OnNotApplyChanges();
            ReloadAdditionalProperties();
            EndEditTemplate();
        }
        protected virtual void OnNotApplyChanges() => NameProperty.Value = EditObject.Name;

        public void Cancel()
        {
            if (HasChanges)
            {
                var messageBox = MessageBoxBase.ShowModal<ThreeButtonMessageBox>();
                messageBox.CaprionText = NodeMarkup.Localize.TemplateEditor_SaveChanges;
                messageBox.MessageText = SaveChangesMessage;
                messageBox.Button1Text = NodeMarkupMessageBox.Yes;
                messageBox.Button2Text = NodeMarkupMessageBox.No;
                messageBox.Button3Text = NodeMarkupMessageBox.Cancel;
                messageBox.OnButton1Click = OnSave;
                messageBox.OnButton2Click = OnNotSave;
            }
            else
                OnNotSave();

            bool OnSave()
            {
                SaveChanges();
                return true;
            }
            bool OnNotSave()
            {
                NotSaveChanges();
                return true;
            }
        }
    }

    public abstract class EditTemplateMode<TemplateType> : BaseToolMode
        where TemplateType : Template
    {
        public override ToolModeType Type => ToolModeType.PanelAction;

        private ITemplateEditor<TemplateType> Editor { get; set; }

        public void Init(ITemplateEditor<TemplateType> editor) => Editor = editor;
        public override void OnSecondaryMouseClicked() => Editor?.Cancel();
    }
}
