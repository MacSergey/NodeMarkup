﻿using IMT.Tools;
using ModsCommon;
using ModsCommon.Utilities;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace IMT.UI
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
            if (SingletonTool<IntersectionMarkingTool>.Instance.MouseRayValid)
                HoverLine = Lines.FirstOrDefault(l => l.IntersectRay(SingletonTool<IntersectionMarkingTool>.Instance.MouseRay));
            else
                HoverLine = null;
        }
        public void Render(RenderManager.CameraInfo cameraInfo, bool renderHover = true)
        {
            foreach (var line in Lines)
                line.Render(new OverlayData(cameraInfo) { Color = Color, Width = LineSize });

            if (renderHover && IsHoverLine)
                HoverLine.Render(new OverlayData(cameraInfo) { Color = CommonColors.Hover, Width = LineSize + Space });
        }
    }
}
