﻿using IMT.Manager;
using IMT.Tools;
using IMT.Utilities;
using ModsCommon;
using ModsCommon.UI;
using ModsCommon.Utilities;
using System;
using UnityEngine;

namespace IMT.UI.Editors
{
    public abstract class OptionsHeaderPanel : BaseDeletableHeaderPanel<HeaderContent> { }
    public class StyleHeaderPanel : OptionsHeaderPanel
    {
        public event Action OnSaveTemplate;
        public event Action OnCopy;
        public event Action OnPaste;
        public event Action OnReset;
        public event Action OnApplySameStyle;
        public event Action OnApplySameType;

        protected IPropertyEditor Editor { get; private set; }
        private Style.StyleType StyleGroup { get; set; }
        private HeaderButtonInfo<HeaderButton> PasteButton { get; set; }
        private HeaderButtonInfo<ApplyTemplateHeaderButton> ApplyTemplate { get; }
        private HeaderButtonInfo<HeaderButton> ApplySameStyle { get; set; }
        private HeaderButtonInfo<HeaderButton> ApplySameType { get; set; }

        public StyleHeaderPanel()
        {
            Content.AddButton(new HeaderButtonInfo<HeaderButton>(HeaderButtonState.Main, IMTTextures.Atlas, IMTTextures.AddTemplateHeaderButton, IMT.Localize.HeaderPanel_SaveAsTemplate, SaveTemplateClick));

            ApplyTemplate = new HeaderButtonInfo<ApplyTemplateHeaderButton>(HeaderButtonState.Main, IMTTextures.Atlas, IMTTextures.ApplyTemplateHeaderButton, IMT.Localize.HeaderPanel_ApplyTemplate);
            Content.AddButton(ApplyTemplate);

            Content.AddButton(new HeaderButtonInfo<HeaderButton>(HeaderButtonState.Main, IMTTextures.Atlas, IMTTextures.CopyHeaderButton, IMT.Localize.HeaderPanel_StyleCopy, CopyClick));

            PasteButton = new HeaderButtonInfo<HeaderButton>(HeaderButtonState.Main, IMTTextures.Atlas, IMTTextures.PasteHeaderButton, IMT.Localize.HeaderPanel_StylePaste, PasteClick);
            Content.AddButton(PasteButton);

            Content.AddButton(new HeaderButtonInfo<HeaderButton>(HeaderButtonState.Main, IMTTextures.Atlas, IMTTextures.ResetHeaderButton, IMT.Localize.HeaderPanel_StyleReset, ResetClick));

            ApplySameStyle = new HeaderButtonInfo<HeaderButton>(HeaderButtonState.Additional, IMTTextures.Atlas, IMTTextures.CopyToSameHeaderButton, string.Empty, ApplySameStyleClick);
            Content.AddButton(ApplySameStyle);

            ApplySameType = new HeaderButtonInfo<HeaderButton>(HeaderButtonState.Additional, IMTTextures.Atlas, IMTTextures.CopyToAllHeaderButton, string.Empty, ApplySameTypeClick);
            Content.AddButton(ApplySameType);
        }

        public void Init(IPropertyEditor editor, Style.StyleType styleGroup, Action<StyleTemplate> onSelectTemplate, bool isDeletable = true)
        {
            Editor = editor;
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
            OnReset = null;
            OnApplySameStyle = null;
            OnApplySameType = null;

            SingletonTool<IntersectionMarkingTool>.Instance.OnStyleToBuffer -= StyleToBuffer;
        }

        public override void Refresh()
        {
            switch (Editor.EditObject)
            {
                case MarkingLineRawRule editRule:
                    {
                        ApplySameStyle.Text = string.Format(IMT.Localize.HeaderPanel_ApplyRegularType, editRule.Style.Value.Type.Description());
                        ApplySameType.Text = IMT.Localize.HeaderPanel_ApplyRegularAll;
                    }
                    break;
                case MarkingCrosswalk editCrosswalk:
                    {
                        ApplySameStyle.Text = string.Format(IMT.Localize.HeaderPanel_ApplyCrosswalkType, editCrosswalk.Style.Value.Type.Description());
                        ApplySameType.Text = IMT.Localize.HeaderPanel_ApplyCrosswalkAll;
                    }
                    break;
                case MarkingFiller editFiller:
                    {
                        ApplySameStyle.Text = string.Format(IMT.Localize.HeaderPanel_ApplyFillerType, editFiller.Style.Value.Type.Description());
                        ApplySameType.Text = IMT.Localize.HeaderPanel_ApplyFillerAll;
                    }
                    break;
            }

            base.Refresh();
        }

        private void SaveTemplateClick() => OnSaveTemplate?.Invoke();
        private void CopyClick() => OnCopy?.Invoke();
        private void PasteClick() => OnPaste?.Invoke();
        private void ResetClick() => OnReset?.Invoke();
        private void ApplySameStyleClick() => OnApplySameStyle?.Invoke();
        private void ApplySameTypeClick() => OnApplySameType?.Invoke();
    }
    public class RuleHeaderPanel : StyleHeaderPanel
    {
        public event Action OnApplyAllRules;
        public event Action OnExpand;

        protected CustomUIButton ExpandButton { get; set; }
        HeaderButtonInfo<HeaderButton> ApplyAllRules { get; }

        public bool IsExpand { set => ExpandButton.normalBgSprite = value ? IMTTextures.ListItemCollapse : IMTTextures.ListItemExpand; }

        public RuleHeaderPanel()
        {
            ExpandButton = AddUIComponent<CustomUIButton>();
            ExpandButton.tooltip = string.Format(IMT.Localize.Header_ExpandTooltip, LocalizeExtension.Shift);
            ExpandButton.atlas = IMTTextures.Atlas;
            ExpandButton.size = new Vector2(30, 30);
            ExpandButton.zOrder = 0;
            ExpandButton.eventClick += (_,_) => OnExpand?.Invoke();

            ApplyAllRules = new HeaderButtonInfo<HeaderButton>(HeaderButtonState.Main, IMTTextures.Atlas, IMTTextures.ApplyStyleHeaderButton, IMT.Localize.HeaderPanel_ApplyAllRules, ApplyAllRulesClick);
            Content.AddButton(ApplyAllRules);
        }
        public override void DeInit()
        {
            base.DeInit();
            IsExpand = false;
            OnApplyAllRules = null;
            OnExpand = null;
        }
        protected override void SetSize()
        {
            base.SetSize();
            ExpandButton.relativePosition = new Vector3(ItemsPadding, (height - ExpandButton.height) * 0.5f);
            Content.width -= ExpandButton.width + ItemsPadding;
            Content.relativePosition = Content.relativePosition + new Vector3(ExpandButton.width + ItemsPadding, 0f);
        }
        public override void Refresh()
        {
            ApplyAllRules.Visible = Editor.EditObject is MarkingLineRawRule editRule && editRule.Line.IsSupportRules;
            base.Refresh();
        }
        private void ApplyAllRulesClick() => OnApplyAllRules?.Invoke();
    }
    public class CrosswalkHeaderPanel : StyleHeaderPanel
    {
        public event Action OnCut;

        public CrosswalkHeaderPanel()
        {
            Content.AddButton(new HeaderButtonInfo<HeaderButton>(HeaderButtonState.Main, IMTTextures.Atlas, IMTTextures.CutHeaderButton, IMT.Localize.HeaderPanel_CutLinesByCrosswalk, CutClick));
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
            Edit = new HeaderButtonInfo<HeaderButton>(HeaderButtonState.Main, IMTTextures.Atlas, IMTTextures.EditHeaderButton, IMT.Localize.HeaderPanel_Edit, EditClick);
            Content.AddButton(Edit);

            SaveAsAsset = new HeaderButtonInfo<HeaderButton>(HeaderButtonState.Main, IMTTextures.Atlas, IMTTextures.PackageHeaderButton, IMT.Localize.HeaderPanel_SaveAsAsset, SaveAssetClick);
            Content.AddButton(SaveAsAsset);

            Save = new HeaderButtonInfo<HeaderButton>(HeaderButtonState.Main, IMTTextures.Atlas, IMTTextures.SaveHeaderButton, IMT.Localize.HeaderPanel_Save, SaveClick);
            Content.AddButton(Save);

            NotSave = new HeaderButtonInfo<HeaderButton>(HeaderButtonState.Main, IMTTextures.Atlas, IMTTextures.NotSaveHeaderButton, IMT.Localize.HeaderPanel_NotSave, NotSaveClick);
            Content.AddButton(NotSave);

            Discard = new HeaderButtonInfo<HeaderButton>(HeaderButtonState.Main, IMTTextures.Atlas, IMTTextures.ClearHeaderButton, IMT.Localize.HeaderPanel_Discard, DiscardClick);
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
        public event Action OnApplySameStyle;
        public event Action OnApplySameType;

        private HeaderButtonInfo<HeaderButton> SetAsDefaultButton { get; set; }
        private HeaderButtonInfo<HeaderButton> UnsetAsDefaultButton { get; set; }
        private HeaderButtonInfo<HeaderButton> Duplicate { get; set; }
        private HeaderButtonInfo<HeaderButton> ApplySameStyle { get; set; }
        private HeaderButtonInfo<HeaderButton> ApplySameType { get; set; }

        private bool IsDefault => Template.IsDefault;

        protected override void AddButtons()
        {
            SetAsDefaultButton = new HeaderButtonInfo<HeaderButton>(HeaderButtonState.Main, IMTTextures.Atlas, IMTTextures.SetDefaultHeaderButton, IMT.Localize.HeaderPanel_SetAsDefault, SetAsDefaultClick);
            Content.AddButton(SetAsDefaultButton);

            UnsetAsDefaultButton = new HeaderButtonInfo<HeaderButton>(HeaderButtonState.Main, IMTTextures.Atlas, IMTTextures.UnsetDefaultHeaderButton, IMT.Localize.HeaderPanel_UnsetAsDefault, SetAsDefaultClick);
            Content.AddButton(UnsetAsDefaultButton);

            Duplicate = new HeaderButtonInfo<HeaderButton>(HeaderButtonState.Main, IMTTextures.Atlas, IMTTextures.DuplicateHeaderButton, IMT.Localize.HeaderPanel_Duplicate, DuplicateClick);
            Content.AddButton(Duplicate);

            ApplySameStyle = new HeaderButtonInfo<HeaderButton>(HeaderButtonState.Main, IMTTextures.Atlas, IMTTextures.CopyToSameHeaderButton, string.Empty, ApplySameStyleClick);
            Content.AddButton(ApplySameStyle);

            ApplySameType = new HeaderButtonInfo<HeaderButton>(HeaderButtonState.Main, IMTTextures.Atlas, IMTTextures.CopyToAllHeaderButton, string.Empty, ApplySameTypeClick);
            Content.AddButton(ApplySameType);

            base.AddButtons();
        }
        public override void DeInit()
        {
            base.DeInit();

            OnSetAsDefault = null;
            OnDuplicate = null;
            OnApplySameStyle = null;
            OnApplySameType = null;
        }

        public override void Refresh()
        {
            SetAsDefaultButton.Visible = !IsDefault && EditMode == EditMode.Default;
            UnsetAsDefaultButton.Visible = IsDefault && EditMode == EditMode.Default;
            Duplicate.Visible = EditMode == EditMode.Default;
            ApplySameStyle.Visible = EditMode == EditMode.Default;
            ApplySameType.Visible = EditMode == EditMode.Default;

            switch (Template.Style)
            {
                case RegularLineStyle:
                    ApplySameStyle.Text = string.Format(IMT.Localize.HeaderPanel_ApplyRegularType, Template.Style.Type.Description());
                    ApplySameType.Text = IMT.Localize.HeaderPanel_ApplyRegularAll;
                    break;
                case StopLineStyle:
                    ApplySameStyle.Text = string.Format(IMT.Localize.HeaderPanel_ApplyStopType, Template.Style.Type.Description());
                    ApplySameType.Text = IMT.Localize.HeaderPanel_ApplyStopAll;
                    break;
                case CrosswalkStyle:
                    ApplySameStyle.Text = string.Format(IMT.Localize.HeaderPanel_ApplyCrosswalkType, Template.Style.Type.Description());
                    ApplySameType.Text = IMT.Localize.HeaderPanel_ApplyCrosswalkAll;
                    break;
                case FillerStyle:
                    ApplySameStyle.Text = string.Format(IMT.Localize.HeaderPanel_ApplyFillerType, Template.Style.Type.Description());
                    ApplySameType.Text = IMT.Localize.HeaderPanel_ApplyFillerAll;
                    break;
            }

            base.Refresh();
        }

        private void SetAsDefaultClick() => OnSetAsDefault?.Invoke();
        private void DuplicateClick() => OnDuplicate?.Invoke();
        private void ApplySameStyleClick() => OnApplySameStyle?.Invoke();
        private void ApplySameTypeClick() => OnApplySameType?.Invoke();
    }
    public class IntersectionTemplateHeaderPanel : TemplateHeaderPanel<IntersectionTemplate>
    {
        public event Action OnApply;
        public event Action OnApplyAll;
        public event Action OnLink;

        private HeaderButtonInfo<HeaderButton> Apply { get; set; }
        private HeaderButtonInfo<HeaderButton> ApplyAll { get; set; }
        private HeaderButtonInfo<HeaderButton> Link { get; set; }
        private HeaderButtonInfo<HeaderButton> Unlink { get; set; }

        protected override void AddButtons()
        {
            Apply = new HeaderButtonInfo<HeaderButton>(HeaderButtonState.Main, IMTTextures.Atlas, IMTTextures.ApplyHeaderButton, IMT.Localize.PresetEditor_ApplyPreset, ApplyClick);
            Content.AddButton(Apply);

            ApplyAll = new HeaderButtonInfo<HeaderButton>(HeaderButtonState.Main, IMTTextures.Atlas, IMTTextures.ApplyAllHeaderButton, IMT.Localize.PresetEditor_ApplyAllPreset, ApplyAllClick);
            Content.AddButton(ApplyAll);

            Link = new HeaderButtonInfo<HeaderButton>(HeaderButtonState.Main, IMTTextures.Atlas, IMTTextures.LinkHeaderButton, IMT.Localize.PresetEditor_LinkPreset, LinkClick);
            Content.AddButton(Link);

            Unlink = new HeaderButtonInfo<HeaderButton>(HeaderButtonState.Main, IMTTextures.Atlas, IMTTextures.UnlinkHeaderButton, IMT.Localize.PresetEditor_UnlinkPreset, LinkClick);
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
            ApplyAll.Visible = EditMode == EditMode.Default && Editor.Marking.Type == MarkingType.Segment;
            var canLink = false;
            var canUnlink = false;
            if (EditMode == EditMode.Default && Template.Enters.Length == 2 && Editor.Marking.Type == MarkingType.Segment)
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
        private void ApplyAllClick() => OnApplyAll?.Invoke();
        private void LinkClick() => OnLink?.Invoke();
    }
}