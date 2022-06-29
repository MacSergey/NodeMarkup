using ColossalFramework.UI;
using ModsCommon;
using ModsCommon.UI;
using NodeMarkup.Manager;
using NodeMarkup.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using UnityEngine;

namespace NodeMarkup.UI.Editors
{
    public class IntersectionTemplateEditor : BaseTemplateEditor<IntersectionTemplateItemsPanel, IntersectionTemplate, IntersectionTemplateHeaderPanel, EditIntersectionTemplateMode>
    {
        public override string Name => NodeMarkup.Localize.PresetEditor_Presets;
        public override string EmptyMessage => string.Format(NodeMarkup.Localize.PresetEditor_EmptyMessage, NodeMarkup.Localize.Panel_SaveAsPreset);
        public override Markup.SupportType Support { get; } = Markup.SupportType.IntersectionTemplates;
        protected override string IsAssetMessage => NodeMarkup.Localize.PresetEditor_PresetIsAsset;
        protected override string RewriteCaption => NodeMarkup.Localize.PresetEditor_RewriteCaption;
        protected override string RewriteMessage => NodeMarkup.Localize.PresetEditor_RewriteMessage;
        protected override string SaveChangesMessage => NodeMarkup.Localize.PresetEditor_SaveChangesMessage;
        protected override string NameExistMessage => NodeMarkup.Localize.PresetEditor_NameExistMessage;
        protected override string IsAssetWarningMessage => NodeMarkup.Localize.PresetEditor_IsAssetWarningMessage;
        protected override string IsWorkshopWarningMessage => NodeMarkup.Localize.PresetEditor_IsWorkshopWarningMessage;

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
        }
        private void Apply() => Tool.ApplyIntersectionTemplate(EditObject);
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
            if (editObject.Enters.Length != Tool.Markup.EntersCount)
                return IntersectionTemplateFit.Poor;
            else if (PointsMatch(editObject, Tool.Markup))
                return IntersectionTemplateFit.Close;
            else if (SimilarWidth(editObject, Tool.Markup))
                return IntersectionTemplateFit.Possible;
            else
                return IntersectionTemplateFit.Poor;
        }
        private bool PointsMatch(IntersectionTemplate template, Markup markup)
        {
            var templatePoints = template.Enters.Select(e => e.Points).ToArray();
            var markupPoints = markup.Enters.Select(e => e.PointCount).ToArray();
            if (markupPoints.Length == templatePoints.Length)
            {
                for (int i = 0; i < 2; i += 1)
                {
                    for (int start = 0; start < templatePoints.Length; start += 1)
                    {
                        if (templatePoints.Skip(start).Concat(templatePoints.Take(start)).SequenceEqual(markupPoints))
                            return true;
                    }
                    templatePoints = templatePoints.Reverse().ToArray();
                }
            }
            return false;
        }

        private bool SimilarWidth(IntersectionTemplate template, Markup markup)
        {
            var templatePoints = template.Enters.Select(e => e.Points).ToArray();
            var markupPoints = markup.Enters.Select(e => e.PointCount).ToArray();
            if (markupPoints.Length == templatePoints.Length)
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
                            var diff = tp - markupPoints[index++];
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
