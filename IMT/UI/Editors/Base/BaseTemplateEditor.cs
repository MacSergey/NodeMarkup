using IMT.Manager;
using IMT.Tools;
using ModsCommon;
using ModsCommon.UI;
using System.Collections.Generic;
using System.Linq;

namespace IMT.UI.Editors
{
    public interface ITemplateEditor<ItemType> : IEditor<ItemType>
        where ItemType : Template
    {
        void Cancel();
        void EditName();
    }
    public abstract class BaseTemplateEditor<ItemsPanelType, TemplateType, HeaderPanelType, EditToolMode> : SimpleEditor<ItemsPanelType, TemplateType>, ITemplateEditor<TemplateType>
        where ItemsPanelType : CustomUIScrollablePanel, IItemPanel<TemplateType>
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

        private EditToolMode ToolMode { get; }

        #endregion

        #region BASIC

        public BaseTemplateEditor()
        {
            ToolMode = Tool.CreateToolMode<EditToolMode>();
            ToolMode.Init(this);
        }
        protected sealed override void OnFillPropertiesPanel(TemplateType template)
        {
            FillProperties();
            AddAditionalProperties();

            SetEditable(Editors.EditMode.Default);
        }
        protected virtual void FillProperties()
        {
            AddHeader();
            AddWarning();
            AddAuthor();
            AddTemplateName();
        }
        protected override void OnClear()
        {
            base.OnClear();

            HeaderPanel = null;
            Warning = null;
            NameProperty = null;
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
                SetEditable(Editors.EditMode.Default);
        }

        protected virtual void AddHeader()
        {
            HeaderPanel = ComponentPool.Get<HeaderPanelType>(PropertiesPanel, nameof(HeaderPanel));
            HeaderPanel.ContentStyle = UIStyle.Default.HeaderContent;
            HeaderPanel.Init(this, EditObject);
            HeaderPanel.OnSaveAsset += SaveAsset;
            HeaderPanel.OnEdit += StartEditTemplate;
            HeaderPanel.OnSave += SaveChanges;
            HeaderPanel.OnNotSave += NotSaveChanges;
            HeaderPanel.OnDiscard += DiscardTemplate;
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
                authorProperty.SetStyle(UIStyle.Default);
                authorProperty.Label = IMT.Localize.TemplateEditor_Author;
                authorProperty.FieldWidth = 230;
                authorProperty.EnableControl = false;
                authorProperty.Init();
                authorProperty.FieldRef.Value = EditObject.Asset.Author;
            }
        }
        private void AddTemplateName()
        {
            NameProperty = ComponentPool.Get<StringPropertyPanel>(PropertiesPanel, "Name");
            NameProperty.SetStyle(UIStyle.Default);
            NameProperty.Label = IMT.Localize.TemplateEditor_Name;
            NameProperty.FieldWidth = 230;
            NameProperty.FieldRef.SubmitOnFocusLost = true;
            NameProperty.Init();
            NameProperty.FieldRef.Value = EditObject.Name;
            NameProperty.OnValueChanged += (name) => OnChanged();
        }

        protected virtual void AddAditionalProperties() { }
        protected virtual void RefreshAdditionalProperties() { }
        protected virtual void ClearAdditionalProperties() { }

        #endregion

        #region HANDLERS

        private void SaveAsset()
        {
            if (SaveAsset(EditObject))
            {
                RefreshSelectedItem();
                OnItemSelect(EditObject);
            }
        }
        protected abstract bool SaveAsset(TemplateType template);

        private void StartEditTemplate() => StartEditTemplate(Editors.EditMode.Edit);
        private void StartEditTemplate(EditMode mode)
        {
            EditMode = true;
            HasChanges = false;
            SetEditable(mode);
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

        protected virtual void SetEditable(EditMode mode)
        {
            Panel.Available = AvailableItems = mode == Editors.EditMode.Default;
            NameProperty.EnableControl = mode != Editors.EditMode.Default;
            HeaderPanel.EditMode = mode;
            Warning.isVisible = Settings.ShowPanelTip && EditObject.IsAsset && mode == Editors.EditMode.Default;
        }

        public void EditName()
        {
            StartEditTemplate(Editors.EditMode.Create);
            NameProperty.Edit();
        }

        protected void OnChanged() => HasChanges = true;
        private void EndEditTemplate()
        {
            EditMode = false;
            HasChanges = false;
            SetEditable(Editors.EditMode.Default);
            Tool.SetDefaultMode();
        }

        private void SaveChanges()
        {
            var name = NameProperty.FieldRef.Value;
            if (!string.IsNullOrEmpty(name) && name != EditObject.Name && (EditObject.Manager as TemplateManager<TemplateType>).ContainsName(name, EditObject))
            {
                var messageBox = MessageBox.Show<YesNoMessageBox>();
                messageBox.CaptionText = IMT.Localize.TemplateEditor_NameExistCaption;
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
                    var messageBox = MessageBox.Show<YesNoMessageBox>();
                    messageBox.CaptionText = RewriteCaption;
                    messageBox.MessageText = $"{IsAssetMessage} {RewriteMessage}";
                    messageBox.OnButton1Click = Save;
                    messageBox.OnButton2Click = EditName;
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
        protected virtual void OnApplyChanges() => EditObject.Name = NameProperty.FieldRef.Value;

        private void NotSaveChanges()
        {
            OnNotApplyChanges();
            AddAditionalProperties();
            EndEditTemplate();
        }
        protected virtual void OnNotApplyChanges() => NameProperty.FieldRef.Value = EditObject.Name;

        private void DiscardTemplate()
        {
            EndEditTemplate();
            OnObjectDelete(EditObject);
            Panel.SelectPrevEditor();
        }

        public void Cancel()
        {
            if (HasChanges)
            {
                var messageBox = MessageBox.Show<ThreeButtonMessageBox>();
                messageBox.CaptionText = IMT.Localize.TemplateEditor_SaveChanges;
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
    public enum EditMode
    {
        Default = 1,
        Edit = 2,
        Create = 4,
    }

    public abstract class EditTemplateMode<TemplateType> : IntersectionMarkingToolMode
        where TemplateType : Template
    {
        public override ToolModeType Type => ToolModeType.PanelAction;

        private ITemplateEditor<TemplateType> Editor { get; set; }

        public void Init(ITemplateEditor<TemplateType> editor) => Editor = editor;
        public override void OnSecondaryMouseClicked() => Editor?.Cancel();
    }
}
