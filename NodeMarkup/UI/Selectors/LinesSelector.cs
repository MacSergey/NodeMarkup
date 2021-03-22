using NodeMarkup.Manager;
using NodeMarkup.Tools;
using NodeMarkup.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NodeMarkup.UI
{
    public class LinesSelector<LineType>
        where LineType : TrajectoryBound
    {
        private LineType[] Lines { get; }

        protected Color Color { get; }
        protected float LineSize { get; }
        private float Space => 0.5f;

        public LineType HoverLine { get; set; }
        public bool IsHoverLine => HoverLine != null;

        public LinesSelector(IEnumerable<LineType> lines, Color color, float lineSize = 0.2f)
        {
            Lines = lines.ToArray();
            Color = color;
            LineSize = lineSize;
        }

        public void OnUpdate()
        {
            if (NodeMarkupTool.MouseRayValid)
                HoverLine = Lines.FirstOrDefault(l => l.IntersectRay(NodeMarkupTool.MouseRay));
            else
                HoverLine = null;
        }
        public void Render(RenderManager.CameraInfo cameraInfo, bool renderHover = true)
        {
            foreach (var line in Lines)
                line.Render(new OverlayData(cameraInfo) { Color = Color, Width = LineSize });

            if (renderHover && IsHoverLine)
                HoverLine.Render(new OverlayData(cameraInfo) { Color = Colors.Hover, Width = LineSize + Space });
        }
    }
}
