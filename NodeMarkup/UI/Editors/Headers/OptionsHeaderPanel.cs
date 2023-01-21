using ColossalFramework.UI;
using ModsCommon;
using ModsCommon.UI;
using ModsCommon.Utilities;
using NodeMarkup.Manager;
using NodeMarkup.Tools;
using NodeMarkup.UI.Panel;
using NodeMarkup.Utilities;
using System;

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
            Content.AddButton(new HeaderButtonInfo<HeaderButton>(HeaderButtonState.Main, IntersectionMarkingToolTextures.Atlas, IntersectionMarkingToolTextures.AddTemplateHeaderButton, NodeMarkup.Localize.HeaderPanel_SaveAsTemplate, SaveTemplateClick));

            ApplyTemplate = new HeaderButtonInfo<ApplyTemplateHeaderButton>(HeaderButtonState.Main, IntersectionMarkingToolTextures.Atlas, IntersectionMarkingToolTextures.ApplyTemplateHeaderButton, NodeMarkup.Localize.HeaderPanel_ApplyTemplate);
            Content.AddButton(ApplyTemplate);

            Content.AddButton(new HeaderButtonInfo<HeaderButton>(HeaderButtonState.Main, IntersectionMarkingToolTextures.Atlas, IntersectionMarkingToolTextures.CopyHeaderButton, NodeMarkup.Localize.HeaderPanel_StyleCopy, CopyClick));

            PasteButton = new HeaderButtonInfo<HeaderButton>(HeaderButtonState.Main, IntersectionMarkingToolTextures.Atlas, IntersectionMarkingToolTextures.PasteHeaderButton, NodeMarkup.Localize.HeaderPanel_StylePaste, PasteClick);
            Content.AddButton(PasteButton);
        }

        public void Init(Style.StyleType styleGroup, Action<StyleTemplate> onSelectTemplate, bool isDeletable = true)
        {
            StyleGroup = styleGroup.GetGroup();
            ApplyTemplate.Button.Init(StyleGroup, onSelectTemplate);

            SetPasteEnabled();
            SingletonTool<IntersectionMarkingTool>.Instance.OnStyleToBuffer += StyleToBuffer;

            base.Init(isDeletable: isDeletable);
        }

        private void StyleToBuffer(Style.StyleType group)
        {
            if (group == StyleGroup)
                SetPasteEnabled();
        }
        private void SetPasteEnabled() => PasteButton.Enable = SingletonTool<IntersectionMarkingTool>.Instance.IsStyleInBuffer(StyleGroup);

        public override void DeInit()
        {
            base.DeInit();

            OnSaveTemplate = null;
            OnCopy = null;
            OnPaste = null;

            SingletonTool<IntersectionMarkingTool>.Instance.OnStyleToBuffer -= StyleToBuffer;
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
            Content.AddButton(new HeaderButtonInfo<HeaderButton>(HeaderButtonState.Main, IntersectionMarkingToolTextures.Atlas, IntersectionMarkingToolTextures.CutHeaderButton, NodeMarkup.Localize.HeaderPanel_CutLinesByCrosswalk, CutClick));
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
        public event Action OnDiscard;

        private HeaderButtonInfo<HeaderButton> SaveAsAsset { get; set; }
        private HeaderButtonInfo<HeaderButton> Edit { get; set; }
        private HeaderButtonInfo<HeaderButton> Save { get; set; }
        private HeaderButtonInfo<HeaderButton> NotSave { get; set; }
        private HeaderButtonInfo<HeaderButton> Discard { get; set; }

        protected TemplateType Template { get; private set; }
        protected Editor Editor { get; private set; }
        private bool IsAsset => Template.IsAsset;
        private bool CanEdit => !IsAsset || Template.Asset.CanEdit;

        private EditMode _editMode;
        public EditMode EditMode
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
            Edit = new HeaderButtonInfo<HeaderButton>(HeaderButtonState.Main, IntersectionMarkingToolTextures.Atlas, IntersectionMarkingToolTextures.EditHeaderButton, NodeMarkup.Localize.HeaderPanel_Edit, EditClick);
            Content.AddButton(Edit);

            SaveAsAsset = new HeaderButtonInfo<HeaderButton>(HeaderButtonState.Main, IntersectionMarkingToolTextures.Atlas, IntersectionMarkingToolTextures.PackageHeaderButton, NodeMarkup.Localize.HeaderPanel_SaveAsAsset, SaveAssetClick);
            Content.AddButton(SaveAsAsset);

            Save = new HeaderButtonInfo<HeaderButton>(HeaderButtonState.Main, IntersectionMarkingToolTextures.Atlas, IntersectionMarkingToolTextures.SaveHeaderButton, NodeMarkup.Localize.HeaderPanel_Save, SaveClick);
            Content.AddButton(Save);

            NotSave = new HeaderButtonInfo<HeaderButton>(HeaderButtonState.Main, IntersectionMarkingToolTextures.Atlas, IntersectionMarkingToolTextures.NotSaveHeaderButton, NodeMarkup.Localize.HeaderPanel_NotSave, NotSaveClick);
            Content.AddButton(NotSave);

            Discard = new HeaderButtonInfo<HeaderButton>(HeaderButtonState.Main, IntersectionMarkingToolTextures.Atlas, IntersectionMarkingToolTextures.ClearHeaderButton, NodeMarkup.Localize.HeaderPanel_Discard, DiscardClick);
            Content.AddButton(Discard);
        }

        public virtual void Init(Editor editor, TemplateType template)
        {
            Editor = editor;
            Template = template;
            base.Init(isDeletable: false);
        }
        public override void DeInit()
        {
            base.DeInit();
            Editor = null;
            Template = null;
            _editMode = EditMode.Default;
            OnSaveAsset = null;
            OnEdit = null;
            OnSave = null;
            OnNotSave = null;
            OnDiscard = null;
        }

        public override void Refresh()
        {
            SaveAsAsset.Visible = !IsAsset && EditMode == EditMode.Default;
            Edit.Visible = (!IsAsset || CanEdit) && EditMode == EditMode.Default;
            Save.Visible = EditMode != EditMode.Default;
            NotSave.Visible = EditMode == EditMode.Edit;
            Discard.Visible = EditMode == EditMode.Create;

            base.Refresh();
        }

        private void SaveAssetClick() => OnSaveAsset?.Invoke();
        private void EditClick() => OnEdit?.Invoke();
        private void SaveClick() => OnSave?.Invoke();
        private void NotSaveClick() => OnNotSave?.Invoke();
        private void DiscardClick() => OnDiscard?.Invoke();
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
            SetAsDefaultButton = new HeaderButtonInfo<HeaderButton>(HeaderButtonState.Main, IntersectionMarkingToolTextures.Atlas, IntersectionMarkingToolTextures.SetDefaultHeaderButton, NodeMarkup.Localize.HeaderPanel_SetAsDefault, SetAsDefaultClick);
            Content.AddButton(SetAsDefaultButton);

            UnsetAsDefaultButton = new HeaderButtonInfo<HeaderButton>(HeaderButtonState.Main, IntersectionMarkingToolTextures.Atlas, IntersectionMarkingToolTextures.UnsetDefaultHeaderButton, NodeMarkup.Localize.HeaderPanel_UnsetAsDefault, SetAsDefaultClick);
            Content.AddButton(UnsetAsDefaultButton);

            Duplicate = new HeaderButtonInfo<HeaderButton>(HeaderButtonState.Main, IntersectionMarkingToolTextures.Atlas, IntersectionMarkingToolTextures.DuplicateHeaderButton, NodeMarkup.Localize.HeaderPanel_Duplicate, DuplicateClick);
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
            SetAsDefaultButton.Visible = !IsDefault && EditMode == EditMode.Default;
            UnsetAsDefaultButton.Visible = IsDefault && EditMode == EditMode.Default;
            Duplicate.Visible = EditMode == EditMode.Default;
            base.Refresh();
        }

        private void SetAsDefaultClick() => OnSetAsDefault?.Invoke();
        private void DuplicateClick() => OnDuplicate?.Invoke();
    }
    public class IntersectionTemplateHeaderPanel : TemplateHeaderPanel<IntersectionTemplate>
    {
        public event Action OnApply;
        public event Action OnLink;

        private HeaderButtonInfo<HeaderButton> Apply { get; set; }
        private HeaderButtonInfo<HeaderButton> Link { get; set; }
        private HeaderButtonInfo<HeaderButton> Unlink { get; set; }

        protected override void AddButtons()
        {
            Apply = new HeaderButtonInfo<HeaderButton>(HeaderButtonState.Main, IntersectionMarkingToolTextures.Atlas, IntersectionMarkingToolTextures.ApplyHeaderButton, NodeMarkup.Localize.PresetEditor_ApplyPreset, ApplyClick);
            Content.AddButton(Apply);

            Link = new HeaderButtonInfo<HeaderButton>(HeaderButtonState.Main, IntersectionMarkingToolTextures.Atlas, IntersectionMarkingToolTextures.LinkHeaderButton, NodeMarkup.Localize.PresetEditor_LinkPreset, LinkClick);
            Content.AddButton(Link);

            Unlink = new HeaderButtonInfo<HeaderButton>(HeaderButtonState.Main, IntersectionMarkingToolTextures.Atlas, IntersectionMarkingToolTextures.UnlinkHeaderButton, NodeMarkup.Localize.PresetEditor_UnlinkPreset, LinkClick);
            Content.AddButton(Unlink);

            base.AddButtons();
        }
        public override void DeInit()
        {
            base.DeInit();
            OnApply = null;
            OnLink = null;
        }

        public override void Refresh()
        {
            Apply.Visible = EditMode == EditMode.Default;
            var canLink = false;
            var canUnlink = false;
            if(EditMode == EditMode.Default && Template.Enters.Length == 2 && Editor.Marking.Type == MarkingType.Segment)
            {
                SingletonManager<RoadTemplateManager>.Instance.TryGetPreset(Editor.Marking.Id.GetSegment().Info.name, out var presetId);

                if (presetId == Template.Id)
                    canUnlink = true;
                else
                    canLink = true;
            }
            Link.Visible = canLink;
            Unlink.Visible = canUnlink;
            base.Refresh();
        }

        private void ApplyClick() => OnApply?.Invoke();
        private void LinkClick() => OnLink?.Invoke();
    }
}
