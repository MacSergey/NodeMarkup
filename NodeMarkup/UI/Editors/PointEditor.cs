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
    public class PointsEditor : Editor<PointItem, MarkupPoint, ColorIcon>
    {
        public override string Name { get; } = "Points";

        FloatPropertyPanel Offset { get; set; }

        public PointsEditor()
        {
            SettingsPanel.eventSizeChanged += SettingsPanelSizeChanged;

            Offset = SettingsPanel.AddUIComponent<FloatPropertyPanel>();
            Offset.Text = "Offset";
            Offset.OnValueChanged += OffsetChanged;
        }
        private void OffsetChanged(float value) => EditObject.Offset = value;

        protected override void FillItems()
        {
            foreach (var enter in Markup.Enters)
            {
                foreach (var point in enter.Points)
                {
                    AddItem(point);
                }
            }
        }
        protected override void OnObjectSelect()
        {
            Offset.Value = EditObject.Offset;
        }
        private void SettingsPanelSizeChanged(UIComponent component, Vector2 value)
        {
            foreach (var item in component.components)
            {
                item.width = value.x;
            }
        }
    }
    public class PointItem : EditableItem<MarkupPoint, ColorIcon> 
    {
        protected override void OnObjectSet()
        {
            Icon.Color = Object.Color;
        }
    }
}
