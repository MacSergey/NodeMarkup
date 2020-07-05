using ColossalFramework.UI;
using NodeMarkup.Manager;
using NodeMarkup.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            SettingsPanel.autoLayoutPadding = new RectOffset(10, 10, 0, 0);
        }
        protected override void FillItems()
        {
#if STOPWATCH
            var sw = Stopwatch.StartNew();
#endif
            foreach (var enter in Markup.Enters)
            {
                foreach (var point in enter.Points)
                {
                    AddItem(point);
                }
            }
#if STOPWATCH
            Logger.LogDebug($"{nameof(PointsEditor)}.{nameof(FillItems)}: {sw.ElapsedMilliseconds}ms");
#endif
        }
        protected override void OnObjectSelect()
        {
            var offset = SettingsPanel.AddUIComponent<FloatPropertyPanel>();
            offset.Text = "Offset";
            offset.UseWheel = true;
            offset.Step = 0.1f;
            offset.Init();
            offset.Value = EditObject.Offset;
            offset.OnValueChanged += OffsetChanged;
        }
        private void OffsetChanged(float value) => EditObject.Offset = value;

        public override void Render(RenderManager.CameraInfo cameraInfo)
        {
            if (HoverItem != null)
            {
                NodeMarkupTool.RenderManager.OverlayEffect.DrawCircle(cameraInfo, Color.white, HoverItem.Object.Position, 2f, -1f, 1280f, false, true);
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
