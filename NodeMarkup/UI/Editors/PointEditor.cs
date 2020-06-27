using ColossalFramework.UI;
using NodeMarkup.Manager;
using NodeMarkup.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NodeMarkup.UI.Editors
{
    public class PointsEditorPanel : Editor<PointItem>
    {
        public override string PanelName { get; } = "Points";

        MarkupPoint Point { get; set; }

        FloatPropertyPanel Offset { get; set; }

        public PointsEditorPanel() : base(nameof(PointsEditorPanel))
        {
            SettingsPanel.eventSizeChanged += SettingsPanelSizeChanged;

            Offset = SettingsPanel.AddUIComponent<FloatPropertyPanel>();
            Offset.Text = "Offset";
            Offset.OnValueChanged += OffsetChanged;
        }
        private void OffsetChanged(float value) => Point.Offset = value;

        public override void SetMarkup(Markup markup)
        {
            base.SetMarkup(markup);

            foreach (var enter in markup.Enters)
            {
                foreach (var point in enter.Points)
                {
                    var item = AddItem(point.ToString());
                    item.Point = point;
                    item.Icon.Color = point.Color;
                }
            }
        }
        protected override void ItemClick(PointItem item)
        {
            Point = item.Point;

            SetPoint();
        }
        private void SetPoint()
        {
            Offset.Value = Point.Offset;
        }
        private void SettingsPanelSizeChanged(UIComponent component, Vector2 value)
        {
            foreach (var item in component.components)
            {
                item.width = value.x;
            }
        }
    }
    public class PointItem : EditableItem<ColorIcon>
    {
        public MarkupPoint Point { get; set; }
    }
}
