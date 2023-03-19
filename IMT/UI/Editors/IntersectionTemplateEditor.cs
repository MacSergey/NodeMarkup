using ColossalFramework.UI;
using IMT.Manager;
using IMT.Utilities;
using ModsCommon;
using ModsCommon.UI;
using ModsCommon.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using UnityEngine;

namespace IMT.UI.Editors
{
    public class IntersectionTemplateEditor : BaseTemplateEditor<IntersectionTemplateItemsPanel, IntersectionTemplate, IntersectionTemplateHeaderPanel, EditIntersectionTemplateMode>
    {
        public override string Name => IMT.Localize.PresetEditor_Presets;
        public override string EmptyMessage => string.Format(IMT.Localize.PresetEditor_EmptyMessage, IMT.Localize.Panel_SaveAsPreset);
        public override Marking.SupportType Support => Marking.SupportType.IntersectionTemplates;
        protected override string IsAssetMessage => IMT.Localize.PresetEditor_PresetIsAsset;
        protected override string RewriteCaption => IMT.Localize.PresetEditor_RewriteCaption;
        protected override string RewriteMessage => IMT.Localize.PresetEditor_RewriteMessage;
        protected override string SaveChangesMessage => IMT.Localize.PresetEditor_SaveChangesMessage;
        protected override string NameExistMessage => IMT.Localize.PresetEditor_NameExistMessage;
        protected override string IsAssetWarningMessage => IMT.Localize.PresetEditor_IsAssetWarningMessage;
        protected override string IsWorkshopWarningMessage => IMT.Localize.PresetEditor_IsWorkshopWarningMessage;

        private PropertyGroupPanel InfoGroup { get; set; }

        protected override IEnumerable<IntersectionTemplate> GetObjects() => SingletonManager<IntersectionTemplateManager>.Instance.Templates;
        protected override void OnObjectSelect(IntersectionTemplate editObject)
        {
            base.OnObjectSelect(editObject);

            ItemsPanel.RemovePreview();

            InfoGroup = ComponentPool.Get<PropertyGroupPanel>(ContentPanel, nameof(InfoGroup));
            InfoGroup.Init();
            var info = ComponentPool.Get<IntersectionTemplateInfoProperty>(InfoGroup, "Info");
            info.Init(EditObject);
        }
        protected override void OnClear()
        {
            base.OnClear();
            InfoGroup = null;
        }
        protected override void OnObjectDelete(IntersectionTemplate template)
        {
            base.OnObjectDelete(template);
            ItemsPanel.RemovePreview();
        }
        protected override void AddHeader()
        {
            base.AddHeader();
            HeaderPanel.OnApply += Apply;
            HeaderPanel.OnApplyAll += ApplyAll;
            HeaderPanel.OnLink += Link;
        }
        private void Apply() => Tool.ApplyIntersectionTemplate(EditObject);
        private void ApplyAll() => Tool.ApplyAllIntersectionTemplate(EditObject);
        private void Link()
        {
            if (Marking.Type == MarkingType.Segment)
            {
                var roadName = Marking.Id.GetSegment().Info.name;
                if (SingletonManager<RoadTemplateManager>.Instance.TryGetPreset(roadName, out var preset) && preset == EditObject.Id)
                    SingletonManager<RoadTemplateManager>.Instance.RevertPreset(roadName);
                else
                    Tool.LinkPreset(EditObject, roadName);
            }

            ItemsPanel.RefreshItems();
            HeaderPanel.Refresh();
        }
        protected override bool SaveAsset(IntersectionTemplate template) => SingletonManager<IntersectionTemplateManager>.Instance.MakeAsset(template);
    }

    public class IntersectionTemplateItemsPanel : ItemsGroupPanel<IntersectionTemplateItem, IntersectionTemplate, IntersectionTemplateGroup, IntersectionTemplateFit>
    {
        private PreviewPanel Preview { get; set; }

        public override bool GroupingEnable => Settings.GroupPresets.value;

        public override int Compare(IntersectionTemplate x, IntersectionTemplate y)
        {
            var result = 0;

            if (Settings.SortPresetsType == 0)
            {
                if ((result = SortByRoads(x, y)) == 0)
                    result = SortByName(x, y);
            }
            else if (Settings.SortPresetsType == 1)
                result = SortByName(x, y);

            return result;

            static int SortByRoads(IntersectionTemplate x, IntersectionTemplate y) => x.Roads.CompareTo(y.Roads);
            static int SortByName(IntersectionTemplate x, IntersectionTemplate y) => x.Name.CompareTo(y.Name);
        }

        protected override void ItemHover(IntersectionTemplateItem item)
        {
            if (Editor.AvailableItems)
                AddPreview(item);
        }
        protected override void ItemLeave(IntersectionTemplateItem item) => RemovePreview();

        private void AddPreview(IntersectionTemplateItem item)
        {
            if (item == SelectItem || Preview != null)
                return;

            Editor.AvailableContent = false;

            var root = GetRootContainer();

            Preview = ComponentPool.Get<PreviewPanel>(root, nameof(Preview));
            Preview.width = 400f;
            Preview.Item = item;

            var info = ComponentPool.Get<PreviewIntersectionTemplateInfo>(Preview, "Info");
            info.Init(item.EditObject);

            item.eventSizeChanged += OnItemSizeChanged;
            item.eventPositionChanged += OnItemSizeChanged;
            SetPreviewPosition();
        }

        public void RemovePreview()
        {
            if (Preview == null)
                return;

            if (Preview.Item != null)
            {
                Preview.Item.eventSizeChanged -= OnItemSizeChanged;
                Preview.Item.eventPositionChanged -= OnItemSizeChanged;
            }

            Editor.AvailableContent = true;
            ComponentPool.Free(Preview);
            Preview = null;
        }
        private void OnItemSizeChanged(UIComponent item, Vector2 size) => SetPreviewPosition();
        private void SetPreviewPosition()
        {
            if(Preview != null)
            {
                var x = Preview.Item.absolutePosition.x + Preview.Item.width;
                var y = Mathf.Min(Preview.Item.absolutePosition.y, Preview.parent.absolutePosition.y + Preview.parent.height - Preview.height);
                Preview.absolutePosition = new Vector2(x, y);
            }
        }

        public override void RefreshItems()
        {
            base.RefreshItems();
            RemovePreview();
        }

        protected override IntersectionTemplateFit SelectGroup(IntersectionTemplate editObject)
        {
            if (Editor.Marking.Type == MarkingType.Segment && SingletonManager<RoadTemplateManager>.Instance.TryGetPreset(Editor.Marking.Id.GetSegment().Info.name, out var preset) && preset == editObject.Id)
                return IntersectionTemplateFit.Link;
            if (editObject.Enters.Length != Tool.Marking.EntersCount)
                return IntersectionTemplateFit.Poor;
            else if (PointsMatch(editObject, Tool.Marking))
                return IntersectionTemplateFit.Close;
            else if (SimilarWidth(editObject, Tool.Marking))
                return IntersectionTemplateFit.Possible;
            else
                return IntersectionTemplateFit.Poor;
        }
        private bool PointsMatch(IntersectionTemplate template, Marking marking)
        {
            var templatePoints = template.Enters.Select(e => e.PointCount).ToArray();
            var markingPoints = marking.Enters.Select(e => e.PointCount).ToArray();
            if (markingPoints.Length == templatePoints.Length)
            {
                for (int i = 0; i < 2; i += 1)
                {
                    for (int start = 0; start < templatePoints.Length; start += 1)
                    {
                        if (templatePoints.Skip(start).Concat(templatePoints.Take(start)).SequenceEqual(markingPoints))
                            return true;
                    }
                    templatePoints = templatePoints.Reverse().ToArray();
                }
            }
            return false;
        }

        private bool SimilarWidth(IntersectionTemplate template, Marking marking)
        {
            var templatePoints = template.Enters.Select(e => e.PointCount).ToArray();
            var markingPoints = marking.Enters.Select(e => e.PointCount).ToArray();
            if (markingPoints.Length == templatePoints.Length)
            {
                for (int i = 0; i < 2; i += 1)
                {
                    for (int start = 0; start < templatePoints.Length; start += 1)
                    {
                        var templateRotated = templatePoints.Skip(start).Concat(templatePoints.Take(start));

                        int maxDiff = 0;
                        bool hasGreater = false;
                        bool hasLesser = false;
                        int index = 0;

                        foreach (var tp in templateRotated)
                        {
                            var diff = tp - markingPoints[index++];
                            hasGreater |= diff > 0;
                            hasLesser |= diff < 0;
                            maxDiff = Math.Max(Math.Abs(diff), maxDiff);
                            if (maxDiff > 1 || (hasGreater && hasLesser))
                                break;
                        }
                        if (maxDiff <= 1 && !(hasGreater && hasLesser))
                            return true;
                    }
                    templatePoints = templatePoints.Reverse().ToArray();
                }
            }
            return false;
        }

        protected override string GroupName(IntersectionTemplateFit group) => group.Description();

        public override int Compare(IntersectionTemplateFit x, IntersectionTemplateFit y) => x.CompareTo(y);
    }
    public enum IntersectionTemplateFit
    {
        [Description(nameof(Localize.PresetEditor_PresetFit_Linked))]
        Link,

        [Description(nameof(Localize.PresetEditor_PresetFit_Perfect))]
        Perfect,

        [Description(nameof(Localize.PresetEditor_PresetFit_Close))]
        Close,

        [Description(nameof(Localize.PresetEditor_PresetFit_Possible))]
        Possible,

        [Description(nameof(Localize.PresetEditor_PresetFit_Poor))]
        Poor,
    }

    public class IntersectionTemplateItem : EditItem<IntersectionTemplate, IntersectionTemplateIcon>
    {
        private bool IsLinked => Editor.Marking.Type == MarkingType.Segment && Editor.Marking.Id.GetSegment().Info is NetInfo info && SingletonManager<RoadTemplateManager>.Instance.TryGetPreset(info.name, out var preset) && preset == EditObject.Id;

        public override ModsCommon.UI.SpriteSet ForegroundSprites => !IsLinked ? base.ForegroundSprites : new ModsCommon.UI.SpriteSet()
        {
            normal = CommonTextures.BorderBig,
            hovered = CommonTextures.PanelSmall,
            pressed = CommonTextures.PanelSmall,
            focused = CommonTextures.BorderBig,
            disabled = CommonTextures.PanelSmall,
        };
        public override ModsCommon.UI.SpriteSet ForegroundSelectedSprites => !IsLinked ? base.ForegroundSelectedSprites : new ModsCommon.UI.SpriteSet(CommonTextures.PanelSmall);

        public override ColorSet ForegroundColors => !IsLinked ? base.ForegroundColors : new ColorSet()
        {
            normal = IMTColors.ItemFavoriteNormal,
            hovered = IMTColors.ItemFavoriteNormal,
            pressed = IMTColors.ItemFavoritePressed,
            focused = IMTColors.ItemFavoriteFocused,
            disabled = null,
        };
        public override ColorSet ForegroundSelectedColors => !IsLinked ? base.ForegroundSelectedColors : new ColorSet(IMTColors.ItemFavoriteFocused);

        public override ColorSet TextColor => !IsLinked ? base.TextColor : new ColorSet()
        {
            normal = Color.white,
            hovered = Color.black,
            pressed = Color.black,
            focused = Color.white,
            disabled = Color.white,
        };
        public override ColorSet TextSelectedColor => !IsLinked ? base.TextSelectedColor : new ColorSet(Color.white);

        public override bool ShowDelete => EditObject != null && !EditObject.IsAsset;

        public override void Refresh()
        {
            base.Refresh();
            Icon.Count = EditObject.Roads;
            wordWrap = !EditObject.IsAsset;
        }
    }
    public class IntersectionTemplateIcon : ColorIcon
    {
        protected CustomUILabel CountLabel { get; }
        public int Count { set => CountLabel.text = value.ToString(); }
        public IntersectionTemplateIcon()
        {
            CountLabel = AddUIComponent<CustomUILabel>();
            CountLabel.textColor = Color.white;
            CountLabel.textScale = 0.7f;
            CountLabel.relativePosition = new Vector3(0, 0);
            CountLabel.autoSize = false;
            CountLabel.textAlignment = UIHorizontalAlignment.Center;
            CountLabel.verticalAlignment = UIVerticalAlignment.Middle;
            CountLabel.padding = new RectOffset(0, 0, 5, 0);
        }
        protected override void OnSizeChanged()
        {
            base.OnSizeChanged();
            if (CountLabel != null)
                CountLabel.size = size;
        }
    }
    public class IntersectionTemplateGroup : EditGroup<IntersectionTemplateFit, IntersectionTemplateItem, IntersectionTemplate> { }
    public class EditIntersectionTemplateMode : EditTemplateMode<IntersectionTemplate> { }
    public class PreviewPanel : PropertyGroupPanel
    {
        public IntersectionTemplateItem Item { get; set; }
        protected override Color32 DefaultColor => IMTColors.ItemGroupBackground;

        protected override void OnTooltipEnter(UIMouseEventParameter p) { return; }
        protected override void OnTooltipHover(UIMouseEventParameter p) { return; }
        protected override void OnTooltipLeave(UIMouseEventParameter p) { return; }
    }
    public class PreviewIntersectionTemplateInfo : IntersectionTemplateInfoProperty
    {
        protected override void OnTooltipEnter(UIMouseEventParameter p) { return; }
        protected override void OnTooltipHover(UIMouseEventParameter p) { return; }
        protected override void OnTooltipLeave(UIMouseEventParameter p) { return; }
    }
}
