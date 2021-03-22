using ColossalFramework.UI;
using ModsCommon.UI;
using NodeMarkup.Manager;
using NodeMarkup.Tools;
using NodeMarkup.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NodeMarkup.UI.Editors
{
    public class PointsEditor : SimpleEditor<PointsItemsPanel, MarkupEnterPoint>
    {
        public override string Name => NodeMarkup.Localize.PointEditor_Points;
        public override string EmptyMessage => string.Empty;
        public override Type SupportType { get; } = typeof(ISupportPoints);

        private FloatPropertyPanel Offset { get; set; }
        private BoolListPropertyPanel Split { get; set; }
        private FloatPropertyPanel Shift { get; set; }

        protected override IEnumerable<MarkupEnterPoint> GetObjects() => Markup.Enters.SelectMany(e => e.Points);

        protected override void OnFillPropertiesPanel(MarkupEnterPoint point)
        {
            Offset = ComponentPool.Get<FloatPropertyPanel>(PropertiesPanel);
            Offset.Text = NodeMarkup.Localize.PointEditor_Offset;
            Offset.UseWheel = true;
            Offset.WheelStep = 0.1f;
            Offset.WheelTip = WheelTip;
            Offset.Init();
            Offset.Value = point.Offset;
            Offset.OnValueChanged += OffsetChanged;

            Split = ComponentPool.Get<BoolListPropertyPanel>(PropertiesPanel);
            Split.Text = "Split";
            Split.Init(NodeMarkup.Localize.StyleOption_No, NodeMarkup.Localize.StyleOption_Yes);
            Split.SelectedObject = point.Split;
            Split.OnSelectObjectChanged += SplitChanged;

            Shift = ComponentPool.Get<FloatPropertyPanel>(PropertiesPanel);
            Shift.Text = "Split shift";
            Shift.UseWheel = true;
            Shift.WheelStep = 0.1f;
            Shift.WheelTip = WheelTip;
            Shift.CheckMin = true;
            Shift.MinValue = 0f;
            Shift.Init();
            Shift.Value = point.Shift;
            Shift.OnValueChanged += (value) => point.Shift.Value = value;
            Shift.isVisible = point.Split;

            void SplitChanged(bool value)
            {
                point.Split.Value = value;
                Shift.isVisible = value;
            }
        }
        protected override void OnClear()
        {
            base.OnClear();
            Offset = null;
            Split = null;
            Shift = null;
        }
        protected override void OnObjectUpdate(MarkupEnterPoint editObject)
        {
            Offset.OnValueChanged -= OffsetChanged;
            Offset.Value = EditObject.Offset;
            Offset.OnValueChanged += OffsetChanged;
        }

        private void OffsetChanged(float value) => EditObject.Offset.Value = value;

        public override void Render(RenderManager.CameraInfo cameraInfo) => ItemsPanel.HoverObject?.Render(cameraInfo, Colors.White, 2f);
    }
    public class PointsItemsPanel : ItemsPanel<PointItem, MarkupEnterPoint>
    {
        public override int Compare(MarkupEnterPoint x, MarkupEnterPoint y) => 0;
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
}
