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

        private PropertyGroupPanel Screenshot { get; set; }

        protected override IEnumerable<IntersectionTemplate> GetObjects() => SingletonManager<IntersectionTemplateManager>.Instance.Templates;
        protected override void OnObjectSelect(IntersectionTemplate editObject)
        {
            base.OnObjectSelect(editObject);

            ItemsPanel.RemovePreview();

            Screenshot = ComponentPool.Get<PropertyGroupPanel>(ContentPanel.Content, nameof(Screenshot));
            var info = ComponentPool.Get<IntersectionTemplateInfoProperty>(Screenshot, "Info");
            info.Init(EditObject);
        }
        protected override void OnClear()
        {
            base.OnClear();
            Screenshot = null;
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
            Preview.Init(365f);

            var info = ComponentPool.Get<PreviewIntersectionTemplateInfo>(Preview, "Info");
            info.Init(item.Object);

            var x = item.absolutePosition.x + item.width;
            var y = Mathf.Min(item.absolutePosition.y, root.absolutePosition.y + root.height - Preview.height);
            Preview.absolutePosition = new Vector2(x, y);
        }

        public void RemovePreview()
        {
            if (Preview == null)
                return;

            Editor.AvailableContent = true;
            ComponentPool.Free(Preview);
            Preview = null;
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
        private bool IsLinked => Editor.Marking.Type == MarkingType.Segment && Editor.Marking.Id.GetSegment().Info is NetInfo info && SingletonManager<RoadTemplateManager>.Instance.TryGetPreset(info.name, out var preset) && preset == Object.Id;

        public override Color32 NormalColor => IsLinked ? new Color32(255, 197, 0, 255) : base.NormalColor;
        public override Color32 HoveredColor => IsLinked ? new Color32(255, 207, 51, 255) : base.HoveredColor;
        public override Color32 PressedColor => IsLinked ? new Color32(255, 218, 72, 255) : base.PressedColor;
        public override Color32 FocusColor => IsLinked ? new Color32(255, 228, 92, 255) : base.FocusColor;

        public override bool ShowDelete => !Object.IsAsset;

        public override void Refresh()
        {
            base.Refresh();
            Icon.Count = Object.Roads;
            Label.wordWrap = !Object.IsAsset;
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
        protected override Color32 Color => new Color32(201, 211, 216, 255);

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
