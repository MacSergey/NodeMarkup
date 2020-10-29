using ColossalFramework.Math;
using NodeMarkup.Manager;
using NodeMarkup.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NodeMarkup.Utils
{
    public class BezierBounds : IRender
    {
        private static float Coef { get; } = Mathf.Sin(45 * Mathf.Deg2Rad);
        public Bezier3 Bezier { get; }
        public float Size { get; }
        private List<Bounds> BoundsList { get; } = new List<Bounds>();
        public IEnumerable<Bounds> Bounds => BoundsList;


        public BezierBounds(Bezier3 bezier, float size)
        {
            Bezier = bezier;
            Size = size;
            CalculateBounds();
        }
        private void CalculateBounds()
        {
            var size = Size * Coef;
            var t = 0f; 
            while(t < 1f)
            {
                t = Bezier.Travel(t, size / 2);
                var bounds = new Bounds(Bezier.Position(t), Vector3.one * size);
                BoundsList.Add(bounds);
            }
        }
        public bool IntersectRay(Ray ray) => BoundsList.Any(b => b.IntersectRay(ray));
        public bool Intersects(Bounds bounds) => BoundsList.Any(b => b.Intersects(bounds));

        public void Render(RenderManager.CameraInfo cameraInfo, Color? color = null, float? width = null, bool? alphaBlend = true, bool? cut = null) 
            => NodeMarkupTool.RenderBezier(cameraInfo, Bezier, color, width ?? Size, alphaBlend, cut);
    }
}
