using ModsCommon;
using ModsCommon.UI;
using NodeMarkup.Manager;
using NodeMarkup.Tools;
using System.Collections.Generic;
using System.Linq;

namespace NodeMarkup.UI.Editors
{
    public interface ITemplateEditor<ItemType> : IEditor<ItemType>
        where ItemType : Template
    {
        void Cancel();
        void EditName();
    }
    public abstract class BaseTemplateEditor<ItemsPanelType, TemplateType, HeaderPanelType, EditToolMode> : SimpleEditor<ItemsPanelType, TemplateType>, ITemplateEditor<TemplateType>
        where ItemsPanelType : AdvancedScrollablePanel, IItemPanel<TemplateType>
        where TemplateType : Template<TemplateType>
        where HeaderPanelType : TemplateHeaderPanel<TemplateType>
        where EditToolMode : EditTemplateMode<TemplateType>
    {
        #region PROPERTIES

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
        private EditToolMode ToolMode { get; }

        #endregion

        #region BASIC

        public BaseTemplateEditor()
        {
            ToolMode = Tool.CreateToolMode<EditToolMode>();
            ToolMode.Init(this);
        }
        protected override void OnFillPropertiesPanel(TemplateType template)
        {
            AddHeader();
            AddWarning();
            AddAuthor();
            AddTemplateName();

            ReloadAdditionalProperties();

            SetEditable();
        }
        protected override void OnClear()
        {
            base.OnClear();

            HeaderPanel = null;
            Warning = null;
            NameProperty = null;
            Aditional = null;
        }
        protected override void OnObjectDelete(TemplateType template)
        {
            (template.Manager as TemplateManager<TemplateType>).DeleteTemplate(template);
            base.OnObjectDelete(template);
        }
        protected override void ActiveEditor()
        {
            base.ActiveEditor();

            EditMode = false;
            if (Active && EditObject is TemplateType)
                SetEditable();
        }

        protected virtual void AddHeader()
        {
            HeaderPanel = ComponentPool.Get<HeaderPanelType>(PropertiesPanel, nameof(HeaderPanel));
            HeaderPanel.Init(EditObject);
            HeaderPanel.OnSaveAsset += SaveAsset;
            HeaderPanel.OnEdit += StartEditTemplate;
            HeaderPanel.OnSave += SaveChanges;
            HeaderPanel.OnNotSave += NotSaveChanges;
        }
        private void AddWarning()
        {
            Warning = ComponentPool.Get<WarningTextProperty>(PropertiesPanel, nameof(Warning));
            Warning.Text = $"{IsAssetMessage} {(EditObject.IsAsset && EditObject.Asset.CanEdit ? IsAssetWarningMessage : IsWorkshopWarningMessage)}";
            Warning.Init();
        }
        private void AddAuthor()
        {
            if (EditObject.IsAsset)
            {
                var authorProperty = ComponentPool.Get<StringPropertyPanel>(PropertiesPanel, "Author");
                authorProperty.Text = NodeMarkup.Localize.TemplateEditor_Author;
                authorProperty.FieldWidth = 230;
                authorProperty.EnableControl = false;
                authorProperty.Init();
                authorProperty.Value = EditObject.Asset.Author;
            }
        }
        private void AddTemplateName()
        {
            NameProperty = ComponentPool.Get<StringPropertyPanel>(PropertiesPanel, "Name");
            NameProperty.Text = NodeMarkup.Localize.TemplateEditor_Name;
            NameProperty.FieldWidth = 230;
            NameProperty.SubmitOnFocusLost = true;
            NameProperty.Init();
            NameProperty.Value = EditObject.Name;
            NameProperty.OnValueChanged += (name) => OnChanged();
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
        protected virtual IEnumerable<EditorItem> AddAditionalProperties() { yield break; }

        #endregion

        #region HANDLERS

        private void SaveAsset()
        {
            if (TemplateManager<TemplateType>.Instance.MakeAsset(EditObject))
            {
                RefreshSelectedItem();
                OnObjectSelect(EditObject);
            }
        }
        private void StartEditTemplate()
        {
            EditMode = true;
            HasChanges = false;
            SetEditable();
            Tool.SetMode(ToolMode);
        }
        public override bool OnEscape()
        {
            if (EditMode)
            {
                Cancel();
                return true;
            }
            else
                return false;
        }

        #endregion

        #region EDIT TEMPLATE

        protected virtual void SetEditable()
        {
            Panel.Available = AvailableItems = !EditMode;
            HeaderPanel.EditMode = NameProperty.EnableControl = EditMode;
            Warning.isVisible = Settings.ShowPanelTip && EditObject.IsAsset && !EditMode;

            foreach (var aditional in Aditional)
                aditional.EnableControl = EditMode;
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
                messageBox = MessageBox.Show<YesNoMessageBox>();
                messageBox.CaptionText = NodeMarkup.Localize.TemplateEditor_NameExistCaption;
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
                    messageBox ??= MessageBox.Show<YesNoMessageBox>();
                    messageBox.CaptionText = RewriteCaption;
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
                RefreshSelectedItem();
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
                var messageBox = MessageBox.Show<ThreeButtonMessageBox>();
                messageBox.CaptionText = NodeMarkup.Localize.TemplateEditor_SaveChanges;
                messageBox.MessageText = SaveChangesMessage;
                messageBox.Button1Text = CommonLocalize.MessageBox_Yes;
                messageBox.Button2Text = CommonLocalize.MessageBox_No;
                messageBox.Button3Text = CommonLocalize.MessageBox_Cancel;
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

        #endregion
    }

    public abstract class EditTemplateMode<TemplateType> : NodeMarkupToolMode
        where TemplateType : Template
    {
        public override ToolModeType Type => ToolModeType.PanelAction;

        private ITemplateEditor<TemplateType> Editor { get; set; }

        public void Init(ITemplateEditor<TemplateType> editor) => Editor = editor;
        public override void OnSecondaryMouseClicked() => Editor?.Cancel();
    }
}
