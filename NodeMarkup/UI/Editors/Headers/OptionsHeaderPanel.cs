using ColossalFramework.UI;
using ModsCommon;
using ModsCommon.UI;
using ModsCommon.Utilities;
using NodeMarkup.Manager;
using NodeMarkup.Tools;
using NodeMarkup.UI.Panel;
using NodeMarkup.Utilities;
using System;
using UnityEngine;

namespace NodeMarkup.UI.Editors
{
    public abstract class OptionsHeaderPanel : BaseDeletableHeaderPanel<HeaderContent> { }
    public class StyleHeaderPanel : OptionsHeaderPanel
    {
        public event Action OnSaveTemplate;
        public event Action OnCopy;
        public event Action OnPaste;

        private Style.StyleType StyleGroup { get; set; }
        private HeaderButtonInfo<HeaderButton> PasteButton { get; set; }
        private HeaderButtonInfo<ApplyTemplateHeaderButton> ApplyTemplate { get; }

        public StyleHeaderPanel()
        {
            Content.AddButton(new HeaderButtonInfo<HeaderButton>(HeaderButtonState.Main, NodeMarkupTextures.Atlas, NodeMarkupTextures.AddTemplate, NodeMarkup.Localize.HeaderPanel_SaveAsTemplate, SaveTemplateClick));

            ApplyTemplate = new HeaderButtonInfo<ApplyTemplateHeaderButton>(HeaderButtonState.Main, NodeMarkupTextures.Atlas, NodeMarkupTextures.ApplyTemplate, NodeMarkup.Localize.HeaderPanel_ApplyTemplate);
            Content.AddButton(ApplyTemplate);

            Content.AddButton(new HeaderButtonInfo<HeaderButton>(HeaderButtonState.Main, NodeMarkupTextures.Atlas, NodeMarkupTextures.Copy, NodeMarkup.Localize.HeaderPanel_StyleCopy, CopyClick));

            PasteButton = new HeaderButtonInfo<HeaderButton>(HeaderButtonState.Main, NodeMarkupTextures.Atlas, NodeMarkupTextures.Paste, NodeMarkup.Localize.HeaderPanel_StylePaste, PasteClick);
            Content.AddButton(PasteButton);
        }

        public void Init(Style.StyleType styleGroup, Action<StyleTemplate> onSelectTemplate, bool isDeletable = true)
        {
            StyleGroup = styleGroup.GetGroup();
            ApplyTemplate.Button.Init(StyleGroup, onSelectTemplate);

            SetPasteEnabled();
            SingletonTool<NodeMarkupTool>.Instance.OnStyleToBuffer += StyleToBuffer;

            base.Init(isDeletable: isDeletable);
        }

        private void StyleToBuffer(Style.StyleType group)
        {
            if (group == StyleGroup)
                SetPasteEnabled();
        }
        private void SetPasteEnabled() => PasteButton.Enable = SingletonTool<NodeMarkupTool>.Instance.IsStyleInBuffer(StyleGroup);

        public override void DeInit()
        {
            base.DeInit();

            OnSaveTemplate = null;
            OnCopy = null;
            OnPaste = null;

            SingletonTool<NodeMarkupTool>.Instance.OnStyleToBuffer -= StyleToBuffer;
        }
        private void SaveTemplateClick() => OnSaveTemplate?.Invoke();
        private void CopyClick() => OnCopy?.Invoke();
        private void PasteClick() => OnPaste?.Invoke();
    }
    public class CrosswalkHeaderPanel : StyleHeaderPanel
    {
        public event Action OnCut;

        public CrosswalkHeaderPanel()
        {
            Content.AddButton(new HeaderButtonInfo<HeaderButton>(HeaderButtonState.Main, NodeMarkupTextures.Atlas, NodeMarkupTextures.Cut, NodeMarkup.Localize.HeaderPanel_CutLinesByCrosswalk, CutClick));
        }
        public override void DeInit()
        {
            base.DeInit();

            OnCut = null;
        }

        private void CutClick() => OnCut?.Invoke();
    }

    public abstract class TemplateHeaderPanel<TemplateType> : OptionsHeaderPanel
        where TemplateType : Template
    {
        public event Action OnSaveAsset;
        public event Action OnEdit;
        public event Action OnSave;
        public event Action OnNotSave;

        private HeaderButtonInfo<HeaderButton> SaveAsAsset { get; set; }
        private HeaderButtonInfo<HeaderButton> Edit { get; set; }
        private HeaderButtonInfo<HeaderButton> Save { get; set; }
        private HeaderButtonInfo<HeaderButton> NotSave { get; set; }

        protected TemplateType Template { get; private set; }
        private bool IsAsset => Template.IsAsset;
        private bool CanEdit => !IsAsset || Template.Asset.CanEdit;

        private bool _editMode;
        public bool EditMode
        {
            get => _editMode;
            set
            {
                if (value != _editMode)
                {
                    _editMode = value;
                    Refresh();
                }
            }
        }

        public TemplateHeaderPanel() => AddButtons();
        protected virtual void AddButtons()
        {
            Edit = new HeaderButtonInfo<HeaderButton>(HeaderButtonState.Main, NodeMarkupTextures.Atlas, NodeMarkupTextures.Edit, NodeMarkup.Localize.HeaderPanel_Edit, EditClick);
            Content.AddButton(Edit);

            SaveAsAsset = new HeaderButtonInfo<HeaderButton>(HeaderButtonState.Main, NodeMarkupTextures.Atlas, NodeMarkupTextures.Package, NodeMarkup.Localize.HeaderPanel_SaveAsAsset, SaveAssetClick);
            Content.AddButton(SaveAsAsset);

            Save = new HeaderButtonInfo<HeaderButton>(HeaderButtonState.Main, NodeMarkupTextures.Atlas, NodeMarkupTextures.Save, NodeMarkup.Localize.HeaderPanel_Save, SaveClick);
            Content.AddButton(Save);

            NotSave = new HeaderButtonInfo<HeaderButton>(HeaderButtonState.Main, NodeMarkupTextures.Atlas, NodeMarkupTextures.NotSave, NodeMarkup.Localize.HeaderPanel_NotSave, NotSaveClick);
            Content.AddButton(NotSave);
        }

        public virtual void Init(TemplateType template)
        {
            Template = template;
            base.Init(isDeletable: false);
        }
        public override void DeInit()
        {
            base.DeInit();
            _editMode = false;
            OnSaveAsset = null;
            OnEdit = null;
            OnSave = null;
            OnNotSave = null;
        }

        public override void Refresh()
        {
            SaveAsAsset.Visible = !IsAsset && !EditMode;
            Edit.Visible = (!IsAsset || CanEdit) && !EditMode;
            Save.Visible = EditMode;
            NotSave.Visible = EditMode;

            base.Refresh();
        }

        private void SaveAssetClick() => OnSaveAsset?.Invoke();
        private void EditClick() => OnEdit?.Invoke();
        private void SaveClick() => OnSave?.Invoke();
        private void NotSaveClick() => OnNotSave?.Invoke();
    }

    public class StyleTemplateHeaderPanel : TemplateHeaderPanel<StyleTemplate>
    {
        public event Action OnSetAsDefault;
        public event Action OnDuplicate;

        private HeaderButtonInfo<HeaderButton> SetAsDefaultButton { get; set; }
        private HeaderButtonInfo<HeaderButton> UnsetAsDefaultButton { get; set; }
        private HeaderButtonInfo<HeaderButton> Duplicate { get; set; }

        private bool IsDefault => Template.IsDefault;

        protected override void AddButtons()
        {
            SetAsDefaultButton = new HeaderButtonInfo<HeaderButton>(HeaderButtonState.Main, NodeMarkupTextures.Atlas, NodeMarkupTextures.SetDefault, NodeMarkup.Localize.HeaderPanel_SetAsDefault, SetAsDefaultClick);
            Content.AddButton(SetAsDefaultButton);

            UnsetAsDefaultButton = new HeaderButtonInfo<HeaderButton>(HeaderButtonState.Main, NodeMarkupTextures.Atlas, NodeMarkupTextures.UnsetDefault, NodeMarkup.Localize.HeaderPanel_UnsetAsDefault, SetAsDefaultClick);
            Content.AddButton(UnsetAsDefaultButton);

            Duplicate = new HeaderButtonInfo<HeaderButton>(HeaderButtonState.Main, NodeMarkupTextures.Atlas, NodeMarkupTextures.Duplicate, NodeMarkup.Localize.HeaderPanel_Duplicate, DuplicateClick);
            Content.AddButton(Duplicate);

            base.AddButtons();
        }
        public override void DeInit()
        {
            base.DeInit();

            OnSetAsDefault = null;
            OnDuplicate = null;
        }

        public override void Refresh()
        {
            SetAsDefaultButton.Visible = !IsDefault && !EditMode;
            UnsetAsDefaultButton.Visible = IsDefault && !EditMode;
            Duplicate.Visible = !EditMode;
            base.Refresh();
        }

        private void SetAsDefaultClick() => OnSetAsDefault?.Invoke();
        private void DuplicateClick() => OnDuplicate?.Invoke();
    }
    public class IntersectionTemplateHeaderPanel : TemplateHeaderPanel<IntersectionTemplate>
    {
        public event Action OnApply;

        private HeaderButtonInfo<HeaderButton> Apply { get; set; }

        protected override void AddButtons()
        {
            Apply = new HeaderButtonInfo<HeaderButton>(HeaderButtonState.Main, NodeMarkupTextures.Atlas, NodeMarkupTextures.Apply, NodeMarkup.Localize.PresetEditor_ApplyPreset, ApplyClick);
            Content.AddButton(Apply);

            base.AddButtons();
        }
        public override void DeInit()
        {
            base.DeInit();
            OnApply = null;
        }

        public override void Refresh()
        {
            Apply.Visible = !EditMode;
            base.Refresh();
        }

        private void ApplyClick() => OnApply?.Invoke();
    }
}
