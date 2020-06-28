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

        public PointsEditor()
        {

        }
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
            var offset = SettingsPanel.AddUIComponent<FloatPropertyPanel>();
            offset.Text = "Offset";
            offset.Init();
            offset.Value = EditObject.Offset;
            offset.OnValueChanged += OffsetChanged;
        }
        private void OffsetChanged(float value) => EditObject.Offset = value;

    }
    public class PointItem : EditableItem<MarkupPoint, ColorIcon> 
    {
        protected override void OnObjectSet()
        {
            Icon.Color = Object.Color;
        }
    }
}
