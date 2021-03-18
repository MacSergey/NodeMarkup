using ColossalFramework.UI;
using ModsCommon.UI;
using NodeMarkup.Manager;
using NodeMarkup.Tools;
using NodeMarkup.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NodeMarkup.UI.Editors
{
    public class PointsEditor : SimpleEditor<PointsItemsPanel, MarkupPoint>
    {
        public override string Name => NodeMarkup.Localize.PointEditor_Points;
        public override string EmptyMessage => string.Empty;
        public override Type SupportType { get; } = typeof(ISupportPoints);

        private FloatPropertyPanel Offset { get; set; }

        protected override IEnumerable<MarkupPoint> GetObjects() => Markup.Enters.SelectMany(e => e.Points.Cast<MarkupPoint>());

        protected override void OnFillPropertiesPanel(MarkupPoint point)
        {
            Offset = ComponentPool.Get<FloatPropertyPanel>(PropertiesPanel);
            Offset.Text = NodeMarkup.Localize.PointEditor_Offset;
            Offset.UseWheel = true;
            Offset.WheelStep = 0.1f;
            Offset.WheelTip = WheelTip;
            Offset.Init();
            Offset.Value = EditObject.Offset;
            Offset.OnValueChanged += OffsetChanged;
        }
        protected override void OnClear()
        {
            base.OnClear();
            Offset = null;
        }
        protected override void OnObjectUpdate(MarkupPoint editObject)
        {
            Offset.OnValueChanged -= OffsetChanged;
            Offset.Value = EditObject.Offset;
            Offset.OnValueChanged += OffsetChanged;
        }

        private void OffsetChanged(float value) => EditObject.Offset = value;

        public override void Render(RenderManager.CameraInfo cameraInfo) => ItemsPanel.HoverObject?.Render(cameraInfo, Colors.White, 2f);
    }
    public class PointsItemsPanel : ItemsPanel<PointItem, MarkupPoint>
    {
        public override int Compare(MarkupPoint x, MarkupPoint y) => 0;
    }
    public class PointItem : EditItem<MarkupPoint, ColorIcon>
    {
        public override bool ShowDelete => false;
        public override void Init(MarkupPoint editableObject)
        {
            base.Init(editableObject);
            Icon.InnerColor = Object.Color;
        }
    }
}
