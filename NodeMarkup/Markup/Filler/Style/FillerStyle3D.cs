using ColossalFramework.UI;
using ModsCommon;
using ModsCommon.UI;
using ModsCommon.Utilities;
using NodeMarkup.UI.Editors;
using NodeMarkup.Utilities;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using UnityEngine;

namespace NodeMarkup.Manager
{
    public abstract class Filler3DStyle : FillerStyle
    {
        public Filler3DStyle(Color32 color, float width, float medianOffset) : base(color, width, medianOffset) { }
    }
    public abstract class TriangulationFillerStyle : Filler3DStyle
    {
        protected abstract MaterialType MaterialType { get; }

        public float MinAngle => 10f;
        public float MinLength => 2f;
        public float MaxLength => 10f;
        public PropertyValue<float> Elevation { get; }

        public TriangulationFillerStyle(Color32 color, float width, float medianOffset, float elevation) : base(color, width, medianOffset)
        {
            Elevation = GetElevationProperty(elevation);
        }

        public override void CopyTo(FillerStyle target)
        {
            base.CopyTo(target);
            if (target is TriangulationFillerStyle triangulationTarget)
                triangulationTarget.Elevation.Value = Elevation;
        }

        public override IStyleData Calculate(MarkupFiller filler, MarkupLOD lod)
        {
            var trajectories = GetTrajectories(filler);
            var parts = GetParts(trajectories, lod);

            var points = parts.SelectMany(p => p).Select(t => t.StartPosition).ToArray();
            if (points.Length < 3)
                return null;

            var triangles = Triangulator.Triangulate(points, PolygonDirection.ClockWise);
            if (triangles == null)
                return null;

            return new MarkupStylePolygonMesh(filler.Markup.Height, Elevation, parts.Select(g => g.Count).ToArray(), points, triangles, MaterialType);
        }
        private List<ITrajectory> GetTrajectories(MarkupFiller filler)
        {
            var contour = filler.IsMedian ? SetMedianOffset(filler).ToList() : filler.Contour.TrajectoriesProcessed.ToList();

            if (contour.Count > 3)
            {
                for (var i = 0; i < contour.Count; i += 1)
                {
                    for (var j = 2; j < contour.Count - 1; j += 1)
                    {
                        var x = i;
                        var y = (i + j) % contour.Count;
                        var intersect = Intersection.CalculateSingle(contour[x], contour[y]);
                        if (intersect.IsIntersect && (intersect.FirstT > 0.5f || intersect.SecondT < 0.5f))
                        {
                            contour[x] = contour[x].Cut(0f, intersect.FirstT);
                            contour[y] = contour[y].Cut(intersect.SecondT, 1f);

                            if (y > x)
                            {
                                var count = y - (x + 1);
                                contour.RemoveRange(x + 1, count);
                                j -= count;
                            }
                            else
                            {
                                contour.RemoveRange(x + 1, contour.Count - (x + 1));
                                contour.RemoveRange(0, y);
                                i -= y;
                            }
                        }
                    }
                }
            }

            if (GetDirection(contour) == PolygonDirection.CounterClockWise)
                contour = contour.Select(t => t.Invert()).Reverse().ToList();

            return contour;
        }
        private List<List<ITrajectory>> GetParts(List<ITrajectory> trajectories, MarkupLOD lod)
        {
            var parts = trajectories.Select(t => StyleHelper.CalculateSolid(t, lod, (tr) => tr, MinAngle, MinLength, MaxLength)).ToList();
            for (var i = 0; i < parts.Count; i += 1)
            {
                var xm = (i - 1 + parts.Count) % parts.Count;
                var x = i;
                var y = (i + 1) % parts.Count;
                var yp = (i + 2) % parts.Count;

                if (FindIntersects(parts[x], parts[y], true, 1))
                    continue;
                if (parts.Count > 3 && parts[y].Count == 1 && FindIntersects(parts[x], parts[yp], true, 0))
                {
                    parts.RemoveAt(y);
                    continue;
                }
                if (FindIntersects(parts[y], parts[x], false, 1))
                    continue;
                if (parts.Count > 3 && parts[x].Count == 1 && FindIntersects(parts[y], parts[xm], false, 0))
                {
                    parts.RemoveAt(x);
                    i -= 1;
                    continue;
                }
            }

            return parts;
        }
        private bool FindIntersects(List<ITrajectory> A, List<ITrajectory> B, bool isClockWise, int skip)
        {
            for (var x = isClockWise ? A.Count - 1 : 0; isClockWise ? x >= 0 : x < A.Count; x += isClockWise ? -1 : 1)
            {
                var xPart = new StraightTrajectory(A[x]);
                for (var y = isClockWise ? skip : B.Count - 1 - skip; isClockWise ? y < B.Count : y >= 0; y += isClockWise ? 1 : -1)
                {
                    var yPart = new StraightTrajectory(B[y]);
                    var intersect = Intersection.CalculateSingle(xPart, yPart);
                    if (intersect.IsIntersect)
                    {
                        if (isClockWise)
                        {
                            A[x] = xPart.Cut(0f, intersect.FirstT);
                            B[y] = yPart.Cut(intersect.SecondT, 1f);
                            B.RemoveRange(0, y);
                        }
                        else
                        {
                            A[x] = xPart.Cut(intersect.FirstT, 1f);
                            B[y] = yPart.Cut(0f, intersect.SecondT);
                            B.RemoveRange(y + 1, B.Count - (y + 1));
                        }

                        return true;
                    }
                }
            }

            return false;
        }

        private PolygonDirection GetDirection(List<ITrajectory> contour)
        {
            var isClockWise = 0;
            for (var i = 0; i < contour.Count; i += 1)
                isClockWise += (Vector3.Cross(-contour[i].Direction, contour[(i + 1) % contour.Count].Direction).y < 0) ? 1 : -1;

            return isClockWise >= 0 ? PolygonDirection.ClockWise : PolygonDirection.CounterClockWise;
        }

        public override void GetUIComponents(MarkupFiller filler, List<EditorItem> components, UIComponent parent, bool isTemplate = false)
        {
            base.GetUIComponents(filler, components, parent, isTemplate);
            components.Add(AddElevationProperty(this, parent));
        }
        private static FloatPropertyPanel AddElevationProperty(TriangulationFillerStyle triangulationStyle, UIComponent parent)
        {
            var elevationProperty = ComponentPool.Get<FloatPropertyPanel>(parent, nameof(Elevation));
            elevationProperty.Text = Localize.FillerStyle_Elevation;
            elevationProperty.UseWheel = true;
            elevationProperty.WheelStep = 0.1f;
            elevationProperty.WheelTip = Editor.WheelTip;
            elevationProperty.CheckMin = true;
            elevationProperty.MinValue = 0f;
            elevationProperty.CheckMax = true;
            elevationProperty.MaxValue = 10f;
            elevationProperty.Init();
            elevationProperty.Value = triangulationStyle.Elevation;
            elevationProperty.OnValueChanged += (float value) => triangulationStyle.Elevation.Value = value;

            return elevationProperty;
        }

        public override XElement ToXml()
        {
            var config = base.ToXml();
            Elevation.ToXml(config);
            return config;
        }
        public override void FromXml(XElement config, Utilities.ObjectsMap map, bool invert)
        {
            base.FromXml(config, map, invert);
            Elevation.FromXml(config, DefaultElevation);
        }
    }
    public class PavementFillerStyle : TriangulationFillerStyle
    {
        public override StyleType Type => StyleType.FillerPavement;
        protected override MaterialType MaterialType => MaterialType.Pavement;

        public PavementFillerStyle(Color32 color, float width, float medianOffset, float elevation) : base(color, width, medianOffset, elevation) { }

        public override FillerStyle CopyStyle() => new PavementFillerStyle(Color, Width, MedianOffset, Elevation);
    }
    public class GrassFillerStyle : TriangulationFillerStyle
    {
        public override StyleType Type => StyleType.FillerGrass;
        protected override MaterialType MaterialType => MaterialType.Grass;

        public GrassFillerStyle(Color32 color, float width, float medianOffset, float elevation) : base(color, width, medianOffset, elevation) { }

        public override FillerStyle CopyStyle() => new GrassFillerStyle(Color, Width, MedianOffset, Elevation);
    }
}
