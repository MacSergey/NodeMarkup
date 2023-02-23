using ColossalFramework;
using ColossalFramework.Math;
using ColossalFramework.UI;
using IMT.UI.Editors;
using IMT.Utilities;
using ModsCommon;
using ModsCommon.UI;
using ModsCommon.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using UnityEngine;

namespace IMT.Manager
{
    public abstract class PeriodicFillerStyle : BaseFillerStyle, IPeriodicFiller
    {
        protected abstract float DefaultStep { get; }
        public PropertyValue<float> Step { get; }
#if DEBUG
        public PropertyBoolValue Debug { get; }
        public PropertyValue<int> RenderOnly { get; }
        public PropertyBoolValue Start { get; }
        public PropertyBoolValue End { get; }
        public PropertyBoolValue StartBorder { get; }
        public PropertyBoolValue EndBorder { get; }
#endif

        public PeriodicFillerStyle(Color32 color, float width, Vector2 cracks, Vector2 voids, float texture, float step, Vector2 offset) : base(color, width, cracks, voids, texture, offset)
        {
            Step = GetStepProperty(step);
#if DEBUG
            Debug = new PropertyBoolValue(StyleChanged, false);
            RenderOnly = new PropertyStructValue<int>(StyleChanged, -1);
            Start = new PropertyBoolValue(StyleChanged, true);
            End = new PropertyBoolValue(StyleChanged, true);
            StartBorder = new PropertyBoolValue(StyleChanged, true);
            EndBorder = new PropertyBoolValue(StyleChanged, true);
#endif
        }

        public override void CopyTo(BaseFillerStyle target)
        {
            base.CopyTo(target);

            if (target is IPeriodicFiller periodicTarget)
                periodicTarget.Step.Value = Step;
        }

        protected override void GetUIComponents(MarkingFiller filler, EditorProvider provider)
        {
            base.GetUIComponents(filler, provider);

            provider.AddProperty(new PropertyInfo<FloatPropertyPanel>(this, nameof(Step), MainCategory, AddStepProperty));
#if DEBUG
            if (!provider.isTemplate && Settings.ShowDebugProperties)
            {
                provider.AddProperty(new PropertyInfo<BoolListPropertyPanel>(this, nameof(Debug), DebugCategory, GetDebug));
                provider.AddProperty(new PropertyInfo<IntPropertyPanel>(this, nameof(RenderOnly), DebugCategory, GetRenderOnlyProperty));
                provider.AddProperty(new PropertyInfo<BoolListPropertyPanel>(this, nameof(Start), DebugCategory, AddStartProperty));
                provider.AddProperty(new PropertyInfo<BoolListPropertyPanel>(this, nameof(End), DebugCategory, AddEndProperty));
                provider.AddProperty(new PropertyInfo<BoolListPropertyPanel>(this, nameof(StartBorder), DebugCategory, AddStartBorderProperty));
                provider.AddProperty(new PropertyInfo<BoolListPropertyPanel>(this, nameof(EndBorder), DebugCategory, AddEndBorderProperty));
            }
#endif
        }
#if DEBUG
        private void GetDebug(BoolListPropertyPanel debugProperty, EditorProvider provider)
        {
            debugProperty.Text = "Debug";
            debugProperty.Init(Localize.StyleOption_No, Localize.StyleOption_Yes);
            debugProperty.SelectedObject = Debug;
            debugProperty.OnSelectObjectChanged += (value) => Debug.Value = value;
        }
        private void GetRenderOnlyProperty(IntPropertyPanel property, EditorProvider provider)
        {
            property.Text = "Render only";
            property.UseWheel = true;
            property.WheelStep = 1;
            property.WheelTip = Settings.ShowToolTip;
            property.CheckMin = true;
            property.MinValue = -1;
            property.Init();
            property.Value = RenderOnly;
            property.OnValueChanged += (int value) => RenderOnly.Value = value;
        }
        protected void AddStartProperty(BoolListPropertyPanel property, EditorProvider provider)
        {
            property.Text = "Start";
            property.Init(Localize.StyleOption_No, Localize.StyleOption_Yes);
            property.SelectedObject = Start;
            property.OnSelectObjectChanged += (value) => Start.Value = value;
        }
        protected void AddEndProperty(BoolListPropertyPanel property, EditorProvider provider)
        {
            property.Text = "End";
            property.Init(Localize.StyleOption_No, Localize.StyleOption_Yes);
            property.SelectedObject = End;
            property.OnSelectObjectChanged += (value) => End.Value = value;
        }
        protected void AddStartBorderProperty(BoolListPropertyPanel property, EditorProvider provider)
        {
            property.Text = "Start border";
            property.Init(Localize.StyleOption_No, Localize.StyleOption_Yes);
            property.SelectedObject = StartBorder;
            property.OnSelectObjectChanged += (value) => StartBorder.Value = value;
        }
        protected void AddEndBorderProperty(BoolListPropertyPanel property, EditorProvider provider)
        {
            property.Text = "End border";
            property.Init(Localize.StyleOption_No, Localize.StyleOption_Yes);
            property.SelectedObject = EndBorder;
            property.OnSelectObjectChanged += (value) => EndBorder.Value = value;
        }
#endif
        protected void AddStepProperty(FloatPropertyPanel stepProperty, EditorProvider provider)
        {
            stepProperty.Text = Localize.StyleOption_Step;
            stepProperty.UseWheel = true;
            stepProperty.WheelStep = 0.1f;
            stepProperty.WheelTip = Settings.ShowToolTip;
            stepProperty.CheckMin = true;
            stepProperty.MinValue = 1.5f;
            stepProperty.Init();
            stepProperty.Value = Step;
            stepProperty.OnValueChanged += (float value) => Step.Value = value;
        }

        protected sealed override void CalculateImpl(MarkingFiller filler, ContourGroup contours, MarkingLOD lod, Action<IStyleData> addData)
        {
            if ((SupportLOD & lod) != 0)
            {
                var guides = GetGuides(filler, contours);
#if DEBUG_PERIODIC_FILLER
                var dashes = new List<MarkingPartData>();
#endif
                foreach (var guide in guides)
                {
#if DEBUG_PERIODIC_FILLER
                    var parts = GetParts(guide, contours, lod, addData);
#else
                    var parts = GetParts(guide, contours, lod);
#endif
                    for (var i = 0; i < parts.Count; i += 1)
                    {
#if DEBUG
                        bool renderOnly = RenderOnly != -1 && i != RenderOnly;
                        if (renderOnly)
                            continue;
#endif
                        var part = parts[i];
                        var cutContours = new Queue<Contour>(contours);

                        if (!part.isBothDir)
                        {
#if DEBUG_PERIODIC_FILLER
                            if (RenderOnly == i)
                                new Border(part.trajectory, Border.Type.Forward, Intersection.Side.Left).Draw(dashes, UnityEngine.Color.cyan, 0.3f);
#endif
                            cutContours.Process(part.trajectory, Intersection.Side.Left);
                            if (cutContours.Count == 0)
                                continue;
                        }

#if DEBUG
                        if (Start)
#endif
                        {
#if DEBUG_PERIODIC_FILLER
                            if (RenderOnly == i)
                                new Border(part.start, Border.Type.Forward, Intersection.Side.Left).Draw(dashes, UnityEngine.Color.magenta, 0.3f);
#endif
                            cutContours.Process(part.start, Intersection.Side.Left);
                            if (cutContours.Count == 0)
                                continue;
                        }

#if DEBUG
                        if (End)
#endif
                        {
#if DEBUG_PERIODIC_FILLER
                            if (RenderOnly == i)
                                new Border(part.end, Border.Type.Forward, Intersection.Side.Left).Draw(dashes, UnityEngine.Color.yellow, 0.3f);
#endif
                            cutContours.Process(part.end, Intersection.Side.Left);
                            if (cutContours.Count == 0)
                                continue;
                        }

#if DEBUG
                        if (StartBorder)
#endif
                            if (part.startBorder != null)
                            {
                                cutContours.Process(part.startBorder.Value.trajectory, part.startBorder.Value.side);
                                if (cutContours.Count == 0)
                                    continue;
                            }

#if DEBUG
                        if (EndBorder)
#endif
                            if (part.endBorder != null)
                            {
                                cutContours.Process(part.endBorder.Value.trajectory, part.endBorder.Value.side);
                                if (cutContours.Count == 0)
                                    continue;
                            }
                        foreach (var contour in cutContours)
                        {
                            var trajectories = contour.Select(e => e.trajectory).ToArray();
                            var datas = DecalData.GetData(DecalData.DecalType.Filler, lod, trajectories, SplitParams, Color, DecalData.TextureData.Default, new DecalData.EffectData(this as IEffectStyle)
#if DEBUG
                                , Debug
#endif
                                );
                            foreach (var data in datas)
                                addData(data);
                        }
                    }
                }
#if DEBUG_PERIODIC_FILLER
                //var points = contours.Points;
                //for (int i = 0; i < points.Length; i += 1)
                //{
                //    dashes.Add(new MarkingPartData(points[i], points[(i + 1) % points.Length], 0.3f, UnityEngine.Color.black, RenderHelper.MaterialLib[MaterialType.RectangleLines]));
                //}

                addData(new MarkingPartGroupData(lod, dashes));
#endif
            }
        }

        protected abstract ITrajectory[] GetGuides(MarkingFiller filler, ContourGroup contours);
        protected StraightTrajectory GetGuide(Rect limits, float height, float angle)
        {
            if (angle > 90)
                angle -= 180;
            else if (angle < -90)
                angle += 180;

            var absAngle = Mathf.Abs(angle) * Mathf.Deg2Rad;
            var guideLength = limits.width * Mathf.Sin(absAngle) + limits.height * Mathf.Cos(absAngle);
            var dx = guideLength * Mathf.Sin(absAngle);
            var dy = guideLength * Mathf.Cos(absAngle);

            if (angle == -90 || angle == 90)
                return new StraightTrajectory(new Vector3(limits.xMin, height, limits.yMax), new Vector3(limits.xMax, height, limits.yMax));
            else if (90 > angle && angle > 0)
                return new StraightTrajectory(new Vector3(limits.xMin, height, limits.yMax), new Vector3(limits.xMin + dx, height, limits.yMax - dy));
            else if (angle == 0)
                return new StraightTrajectory(new Vector3(limits.xMin, height, limits.yMax), new Vector3(limits.xMin, height, limits.yMin));
            else if (0 > angle && angle > -90)
                return new StraightTrajectory(new Vector3(limits.xMin, height, limits.yMin), new Vector3(limits.xMin + dx, height, limits.yMin + dy));
            else
                return default;
        }

#if DEBUG_PERIODIC_FILLER
        protected abstract List<Part> GetParts(ITrajectory guide, EdgeSetGroup contours, MarkingLOD lod, Action<IStyleData> addData);
#else
        protected abstract List<Part> GetParts(ITrajectory guide, ContourGroup contours, MarkingLOD lod);
#endif

        protected List<StraightTrajectory> GetPartTrajectories(ITrajectory guide, Rect limits, float dash, float space)
        {
            List<StraightTrajectory> trajectories = new List<StraightTrajectory>();
            foreach (var part in StyleHelper.   CalculateDashesBezierT(guide, dash, space, 1))
            {
                trajectories.Add(new StraightTrajectory(guide.Position(part.start), guide.Position(part.end)));
            }
            return trajectories;
        }

        public override XElement ToXml()
        {
            var config = base.ToXml();
            Step.ToXml(config);
            return config;
        }
        public override void FromXml(XElement config, ObjectsMap map, bool invert, bool typeChanged)
        {
            base.FromXml(config, map, invert, typeChanged);
            Step.FromXml(config, DefaultStep);
        }

        protected readonly struct Part
        {
            public readonly Vector3 direction;
            public readonly StraightTrajectory trajectory;
            public readonly StraightTrajectory start;
            public readonly StraightTrajectory end;
            public readonly bool isBothDir;
            public readonly Border? startBorder;
            public readonly Border? endBorder;

            public Vector3 StartPos => start.StartPosition;
            public Vector3 EndPos => end.StartPosition;

            public Part(StraightTrajectory trajectory, Border? startBorder, Border? endBorder, float angle, bool isBothDir)
            {
                direction = trajectory.Direction.MakeFlatNormalized().TurnDeg(angle, true);

                if (angle >= 0)
                    this.trajectory = new StraightTrajectory(trajectory.StartPosition, trajectory.EndPosition, false);
                else
                    this.trajectory = new StraightTrajectory(trajectory.EndPosition, trajectory.StartPosition, false);

                start = new StraightTrajectory(this.trajectory.StartPosition, this.trajectory.StartPosition + direction, false);
                end = new StraightTrajectory(this.trajectory.EndPosition, this.trajectory.EndPosition - direction, false);

                this.isBothDir = isBothDir;
                this.startBorder = startBorder;
                this.endBorder = endBorder;
            }

            public bool CanIntersect(ContourGroup contours, bool precise) => contours.CanIntersect(start, precise) || contours.CanIntersect(end, precise);

            public override string ToString() => $"{start} ÷ {end}";
        }
        protected readonly struct BorderPair
        {
            public readonly StraightTrajectory trajectory;
            public readonly Border startBorder;
            public readonly Border endBorder;

            public BorderPair(StraightTrajectory trajectory, Border startBorder, Border endBorder)
            {
                this.trajectory = trajectory;
                this.startBorder = startBorder;
                this.endBorder = endBorder;
            }

            public bool IsMain(float startT, float endT) => startBorder.IsMain(startT) || endBorder.IsMain(endT);

            public static List<BorderPair> GetBorders(List<StraightTrajectory> trajectories, ContourGroup contours, float angle, bool isBothDir)
            {
                var borders = new List<BorderPair>(trajectories.Count);
                var orientation = Mathf.Abs(angle) <= 90 ? Border.Type.Forward : Border.Type.Backward;
                var sign = angle >= 0;

                for (var i = 0; i < trajectories.Count; i += 1)
                {
                    var trajectory = trajectories[i];
                    var direction = trajectory.Direction.TurnDeg(angle, true);

                    var startLine = new StraightTrajectory(trajectory.StartPosition, trajectory.StartPosition + direction, !isBothDir, false);
                    var endLine = new StraightTrajectory(trajectory.EndPosition, trajectory.EndPosition + direction, !isBothDir, false);

                    if (contours.CanIntersect(startLine, true) || contours.CanIntersect(endLine, true))
                    {
                        var startBorder = Border.GetBorder(startLine, orientation | Border.Type.After, sign ? Intersection.Side.Right : Intersection.Side.Left, contours);
                        var endBorder = Border.GetBorder(endLine, orientation | Border.Type.Before, sign ? Intersection.Side.Left : Intersection.Side.Right, contours);

                        borders.Add(new BorderPair(trajectory, startBorder, endBorder));
                    }
                }

                return borders;
            }

            public void GetPartBorders(List<BorderPair> borders, int index, out Border? startBorder, out Border? endBorder)
            {
                startBorder = null;
                endBorder = null;

                var startInter = Intersection.NotIntersect;
                var endInter = Intersection.NotIntersect;

                for (var i = index - 1; i >= 0; i -= 1)
                {
                    var nextBorder = borders[i].endBorder;
                    var nextStartInter = Intersection.GetIntersection(this.startBorder.trajectory, in nextBorder.trajectory);
                    var nextEndInter = Intersection.GetIntersection(this.endBorder.trajectory, in nextBorder.trajectory);

                    if (nextStartInter.isIntersect || nextEndInter.isIntersect)
                        CheckBorders(ref startBorder, ref endBorder, ref startInter, ref endInter, in nextBorder, in nextStartInter, in nextEndInter);
                }
                for (var i = index + 1; i < borders.Count; i += 1)
                {
                    var nextBorder = borders[i].startBorder;
                    var nextStartInter = Intersection.GetIntersection(this.startBorder.trajectory, in nextBorder.trajectory);
                    var nextEndInter = Intersection.GetIntersection(this.endBorder.trajectory, in nextBorder.trajectory);

                    if (nextStartInter.isIntersect || nextEndInter.isIntersect)
                        CheckBorders(ref startBorder, ref endBorder, ref startInter, ref endInter, in nextBorder, in nextStartInter, in nextEndInter);
                }
                if (startBorder == endBorder)
                    endBorder = null;
            }

            private void CheckBorders(ref Border? startBorder, ref Border? endBorder, ref Intersection startInter, ref Intersection endInter, in Border nextBorder, in Intersection nextStartInter, in Intersection nextEndInter)
            {
                var isMain = (startBorder != null && this.startBorder.IsMain(startInter.firstT)) || (endBorder != null && this.endBorder.IsMain(endInter.firstT));
                var isNextMain = IsMain(nextStartInter.isIntersect ? nextStartInter.firstT : float.MaxValue, nextEndInter.isIntersect ? nextEndInter.firstT : float.MaxValue);

                if (nextStartInter.isIntersect && Math.Abs(nextStartInter.firstT) < 1000f && Math.Abs(nextStartInter.secondT) < 1000f)
                {
                    var isStartBorderMain = startBorder != null && startBorder.Value.IsMain(startInter.secondT);
                    var isNextStartBorderMain = nextBorder.IsMain(nextStartInter.secondT);
                    var isStartLess = startBorder == null || Mathf.Abs(nextStartInter.firstT) < Mathf.Abs(startInter.firstT);
                    CheckBorder(ref startBorder, ref startInter, in nextBorder, in nextStartInter, isStartLess, isMain, isNextMain, isStartBorderMain, isNextStartBorderMain);
                }

                if (nextEndInter.isIntersect && Math.Abs(nextEndInter.firstT) < 1000f && Math.Abs(nextEndInter.secondT) < 1000f)
                {
                    var isEndBorderMain = endBorder != null && endBorder.Value.IsMain(endInter.secondT);
                    var isNextEndBorderMain = nextBorder.IsMain(nextEndInter.secondT);
                    var isEndLess = endBorder == null || Mathf.Abs(nextEndInter.firstT) < Mathf.Abs(endInter.firstT);
                    CheckBorder(ref endBorder, ref endInter, in nextBorder, in nextEndInter, isEndLess, isMain, isNextMain, isEndBorderMain, isNextEndBorderMain);
                }
            }

            private void CheckBorder(ref Border? border, ref Intersection inter, in Border nextBorder, in Intersection nextInter, bool isLess, bool isMain, bool isNextMain, bool isBorderMain, bool isNextBorderMain)
            {
                if (nextInter.isIntersect && Math.Abs(nextInter.firstT) < 1000f && Math.Abs(nextInter.secondT) < 1000f)
                {
                    if (!isMain && !isNextMain && isLess)
                    {
                        border = nextBorder;
                        inter = nextInter;
                    }
                    else if (isNextBorderMain && isLess)
                    {
                        if (!isMain && !isNextMain)
                        {
                            border = nextBorder;
                            inter = nextInter;
                        }
                        else if (nextBorder.type switch
                        {
                            Border.Type.BeforeForward or Border.Type.AfterBackward => nextInter.firstT <= 0 && nextInter.secondT <= 0,
                            Border.Type.BeforeBackward or Border.Type.AfterForward => nextInter.firstT >= 0 && nextInter.secondT >= 0,
                        })
                        {
                            border = nextBorder;
                            inter = nextInter;
                        }
                    }
                    else if (isMain && !isBorderMain && !isLess && (!isNextMain || isNextBorderMain))
                    {
                        border = nextBorder;
                        inter = nextInter;
                    }
                }
            }

#if DEBUG_PERIODIC_FILLER
            public void Draw(List<MarkingPartData> dashes, float width = 0.05f)
            {
                startBorder.Draw(dashes, UnityEngine.Color.green, width);
                endBorder.Draw(dashes, UnityEngine.Color.red, width);
            }
#endif

            public override string ToString() => $"[{startBorder.mainStartT:0.###} ÷ {startBorder.mainEndT:0.###}] [{endBorder.mainStartT:0.###} ÷ {endBorder.mainEndT:0.###}]";
        }
        protected readonly struct Border
        {
            public readonly StraightTrajectory trajectory;
            public readonly Intersection.Side side;
            public readonly Type type;
            public readonly float? mainStartT;
            public readonly float? mainEndT;

            public Border(StraightTrajectory trajectory, Type type, Intersection.Side side, float? mainStartT = null, float? mainEndT = null)
            {
                this.trajectory = trajectory;
                this.type = type;
                this.side = side;
                this.mainStartT = mainStartT;
                this.mainEndT = mainEndT;
            }

            public bool IsMain(float t) => mainStartT != null && mainEndT != null && mainStartT <= t && t <= mainEndT;

            public static Border GetBorder(in StraightTrajectory trajectory, Type type, Intersection.Side side, ContourGroup contours)
            {
                var intersections = new HashSet<Intersection>();

                foreach (var contour in contours)
                {
                    var contourIntersection = contour.GetIntersections(trajectory);
                    intersections.AddRange(contourIntersection);
                }

                if (trajectory.StartLimited && intersections.Count % 2 == 1)
                    intersections.Add(new Intersection(0f, 0f));

                if (intersections.Count >= 2)
                {
                    var sortedIntersections = intersections.OrderBy(i => i, Intersection.FirstComparer).ToArray();
                    for (int i = 1; i < sortedIntersections.Length; i += 1)
                    {
                        var startT = sortedIntersections[i - 1].firstT;
                        var endT = sortedIntersections[i].firstT;

                        if (startT * endT <= 0)
                        {
                            return new Border(trajectory, type, side, startT, endT);
                        }
                    }
                }

                return new Border(trajectory, type, side, null, null);
            }

#if DEBUG_PERIODIC_FILLER
            public void Draw(List<MarkingPartData> dashes, Color color, float width = 0.05f)
            {
                var normal = trajectory.Direction.Turn90(side == Intersection.Side.Right);

                var start = trajectory.StartLimited ? 0f : -50f;
                var end = trajectory.EndLimited ? 1f : 50f;

                if (mainStartT != null)
                    start = Mathf.Min(start, mainStartT.Value);
                if (mainEndT != null)
                    end = Mathf.Max(end, mainEndT.Value);

                dashes.Add(new MarkingPartData(trajectory.Position(start), trajectory.Position(end), width, color, RenderHelper.MaterialLib[MaterialType.RectangleLines]));
                dashes.Add(new MarkingPartData(trajectory.Position(end - 0.2f - width * 0.5f) + normal * 0.25f, trajectory.Position(end + 0.2f + width * 0.5f) + normal * 0.25f, 0.45f + width, color, RenderHelper.MaterialLib[MaterialType.RectangleLines]));
            }
#endif
            public override bool Equals(object obj) => obj is Border border && border.trajectory == trajectory;
            public static bool operator ==(Border left, Border right) => left.trajectory == right.trajectory;
            public static bool operator !=(Border left, Border right) => left.trajectory != right.trajectory;

            public override string ToString() => $"{trajectory} ({mainStartT:0.###} ÷ {mainEndT:0.###})";

            [Flags]
            public enum Type
            {
                None = 0,
                Before = 1 << 0,
                After = 1 << 1,
                Forward = 1 << 2,
                Backward = 1 << 3,

                BeforeForward = Before | Forward,
                BeforeBackward = Before | Backward,
                AfterForward = After | Forward,
                AfterBackward = After | Backward,
            }
        }
    }
}
