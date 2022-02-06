using ModsCommon;
using ModsCommon.UI;
using ModsCommon.Utilities;
using NodeMarkup.Manager;
using NodeMarkup.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NodeMarkup.UI.Editors
{
    public class PointsEditor : SimpleEditor<PointsItemsPanel, MarkupEnterPoint>
    {
        public override string Name => NodeMarkup.Localize.PointEditor_Points;
        public override string EmptyMessage => string.Empty;
        public override Type SupportType { get; } = typeof(ISupportPoints);

        protected PropertyGroupPanel TemplatePanel { get; private set; }

        private FloatPropertyPanel Offset { get; set; }
        private BoolListPropertyPanel Split { get; set; }
        private FloatPropertyPanel Shift { get; set; }

        protected override IEnumerable<MarkupEnterPoint> GetObjects() => Markup.Enters.SelectMany(e => e.Points);
        protected override void OnObjectSelect(MarkupEnterPoint point)
        {
            base.OnObjectSelect(point);

            TemplatePanel = ComponentPool.Get<PropertyGroupPanel>(ContentPanel.Content);
            TemplatePanel.StopLayout();
            FillTemplatePanel(point);
            TemplatePanel.StartLayout();
            TemplatePanel.Init();
        }


        protected override void OnFillPropertiesPanel(MarkupEnterPoint point)
        {
            AddOffset(point);
            AddSplit(point);
            AddShift(point);
        }
        private void FillTemplatePanel(MarkupEnterPoint point)
        {
            AddRoad(point);
            AddTemplate(point);
        }
        private void AddOffset(MarkupEnterPoint point)
        {
            Offset = ComponentPool.Get<FloatPropertyPanel>(PropertiesPanel, nameof(Offset));
            Offset.Text = NodeMarkup.Localize.PointEditor_Offset;
            Offset.UseWheel = true;
            Offset.WheelStep = 0.1f;
            Offset.WheelTip = Settings.ShowToolTip;
            Offset.Init();
            Offset.Value = point.Offset;
            Offset.OnValueChanged += OffsetChanged;
        }
        private void AddSplit(MarkupEnterPoint point)
        {
            Split = ComponentPool.Get<BoolListPropertyPanel>(PropertiesPanel, nameof(Split));
            Split.Text = NodeMarkup.Localize.PointEditor_SplitIntoTwo;
            Split.Init(NodeMarkup.Localize.StyleOption_No, NodeMarkup.Localize.StyleOption_Yes);
            Split.SelectedObject = point.Split;
            Split.OnSelectObjectChanged += SplitChanged;
        }
        private void AddShift(MarkupEnterPoint point)
        {
            Shift = ComponentPool.Get<FloatPropertyPanel>(PropertiesPanel, nameof(Shift));
            Shift.Text = NodeMarkup.Localize.PointEditor_SplitOffset;
            Shift.UseWheel = true;
            Shift.WheelStep = 0.1f;
            Shift.WheelTip = Settings.ShowToolTip;
            Shift.CheckMin = true;
            Shift.MinValue = 0f;
            Shift.Init();
            Shift.Value = point.SplitOffset;
            Shift.OnValueChanged += (value) => point.SplitOffset.Value = value;
            Shift.isVisible = point.Split;
        }
        private void AddRoad(MarkupEnterPoint point)
        {
            var roadNameProperty = ComponentPool.Get<StringPropertyPanel>(TemplatePanel, "Road");
            roadNameProperty.Text = NodeMarkup.Localize.PointEditor_RoadName;
            roadNameProperty.FieldWidth = 230;
            roadNameProperty.EnableControl = false;
            roadNameProperty.Init();
            roadNameProperty.Value = point.Enter.GetSegment().Info.name;
        }
        private void AddTemplate(MarkupEnterPoint point)
        {
            var buttonsPanel = ComponentPool.Get<ButtonsPanel>(TemplatePanel, "Buttons");
            var saveIndex = buttonsPanel.AddButton(NodeMarkup.Localize.PointEditor_SaveOffsets);
            var revertIndex = buttonsPanel.AddButton(NodeMarkup.Localize.PointEditor_RevertOffsets);
            buttonsPanel.Init();

            SetEnable();

            buttonsPanel.OnButtonClick += OnButtonClick;

            void OnButtonClick(int index)
            {
                if (index == saveIndex)
                {
                    var invert = point.Enter.IsLaneInvert;
                    var offsets = point.Enter.Points.Select(p => invert ? -p.Offset : p.Offset);
                    if (invert)
                        offsets = offsets.Reverse();

                    SingletonManager<RoadTemplateManager>.Instance.SaveOffsets(point.Enter.RoadName, offsets.ToArray());
                    SetEnable();
                }
                else if (index == revertIndex)
                {
                    SingletonManager<RoadTemplateManager>.Instance.RevertOffsets(point.Enter.RoadName);
                    SetEnable();
                }
            }
            void SetEnable()
            {
                buttonsPanel[revertIndex].isEnabled = SingletonManager<RoadTemplateManager>.Instance.Contains(point.Enter.RoadName);
            }
        }

        protected override void OnClear()
        {
            base.OnClear();
            Offset = null;
            Split = null;
            Shift = null;

            TemplatePanel = null;
        }
        protected override void OnObjectUpdate(MarkupEnterPoint editObject)
        {
            Offset.OnValueChanged -= OffsetChanged;
            Offset.Value = EditObject.Offset;
            Offset.OnValueChanged += OffsetChanged;
        }

        private void OffsetChanged(float value) => EditObject.Offset.Value = value;

        private void SplitChanged(bool value)
        {
            EditObject.Split.Value = value;
            Shift.isVisible = value;
        }

        public override void Render(RenderManager.CameraInfo cameraInfo)
        {
            ItemsPanel.HoverGroupObject?.Render(new OverlayData(cameraInfo) { Color = Colors.White, Width = 2f });
            ItemsPanel.HoverObject?.Render(new OverlayData(cameraInfo) { Color = Colors.White, Width = 2f });
        }
    }
    public class PointsItemsPanel : ItemsGroupPanel<PointItem, MarkupEnterPoint, PointGroup, Enter>
    {
        public override bool GroupingEnable => Settings.GroupPoints.value;

        public override int Compare(MarkupEnterPoint x, MarkupEnterPoint y) => 0;

        public override int Compare(Enter x, Enter y) => 0;

        protected override string GroupName(Enter group) => group.ToString();

        protected override Enter SelectGroup(MarkupEnterPoint point) => point.Enter;
    }
    public class PointItem : EditItem<MarkupEnterPoint, ColorIcon>
    {
        public override bool ShowDelete => false;
        public override void Init(MarkupEnterPoint editObject)
        {
            base.Init(editObject);
            Icon.InnerColor = Object.Color;
        }
    }
    public class PointGroup : EditGroup<Enter, PointItem, MarkupEnterPoint> { }
}
