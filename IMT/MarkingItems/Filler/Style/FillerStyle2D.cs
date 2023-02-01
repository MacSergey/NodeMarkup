using ColossalFramework.Math;
using ColossalFramework.UI;
using IMT.API;
using IMT.Utilities;
using IMT.Utilities.API;
using ModsCommon;
using ModsCommon.UI;
using ModsCommon.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Xml.Linq;
using UnityEngine;

namespace IMT.Manager
{
    public interface IPeriodicFiller : IFillerStyle
    {
        PropertyValue<float> Step { get; }
    }
    public interface IOffsetFiller : IFillerStyle
    {
        PropertyValue<float> Offset { get; }
    }
    public interface IRotateFiller : IFillerStyle
    {
        PropertyValue<float> Angle { get; }
    }
    public interface ITextureFiller : IFillerStyle
    {
        public PropertyStructValue<float> ScratchDensity { get; }
        public PropertyStructValue<float> ScratchTiling { get; }
        public PropertyStructValue<float> VoidDensity { get; }
        public PropertyStructValue<float> VoidTiling { get; }
    }
    public interface IGuideFiller : IFillerStyle
    {
        PropertyValue<int> LeftGuideA { get; }
        PropertyValue<int> LeftGuideB { get; }
        PropertyValue<int> RightGuideA { get; }
        PropertyValue<int> RightGuideB { get; }
    }
    public interface IFollowGuideFiller : IGuideFiller
    {
        PropertyValue<bool> FollowGuides { get; }
    }

    public abstract class Filler2DStyle : FillerStyle, ITextureFiller
    {
        public PropertyStructValue<float> ScratchDensity { get; set; }
        public PropertyStructValue<float> ScratchTiling { get; set; }
        public PropertyStructValue<float> VoidDensity { get; set; }
        public PropertyStructValue<float> VoidTiling { get; set; }

        public Filler2DStyle(Color32 color, float width, float lineOffset, float medianOffset) : base(color, width, lineOffset, medianOffset)
        {
            ScratchDensity = new PropertyStructValue<float>(StyleChanged, 0);
            ScratchTiling = new PropertyStructValue<float>(StyleChanged, 1f);
            VoidDensity = new PropertyStructValue<float>(StyleChanged, 0);
            VoidTiling = new PropertyStructValue<float>(StyleChanged, 1f);
        }

        public override void CopyTo(FillerStyle target)
        {
            base.CopyTo(target);

            if (target is Filler2DStyle target2D)
            {
                target2D.ScratchDensity.Value = ScratchDensity;
                target2D.ScratchTiling.Value = ScratchTiling;
                target2D.VoidDensity.Value = VoidDensity;
                target2D.VoidTiling.Value = VoidTiling;
            }
        }

        public override void GetUIComponents(MarkingFiller filler, List<EditorItem> components, UIComponent parent, bool isTemplate = false)
        {
            base.GetUIComponents(filler, components, parent, isTemplate);

            components.Add(GetScratch(parent));
            components.Add(GetVoid(parent));
        }

        protected Vector2PropertyPanel GetScratch(UIComponent parent)
        {
            var scratchProperty = ComponentPool.Get<Vector2PropertyPanel>(parent, nameof(ScratchDensity));
            scratchProperty.Text = "Scratch";
            scratchProperty.SetLabels("Density", "Scale");
            scratchProperty.Format = Localize.NumberFormat_Percent;
            scratchProperty.FieldsWidth = 50f;
            scratchProperty.CanCollapse = false;
            scratchProperty.CheckMax = true;
            scratchProperty.CheckMin = true;
            scratchProperty.MinValue = new Vector2(0f, 10f);
            scratchProperty.MaxValue = new Vector2(100f, 1000f);
            scratchProperty.WheelStep = new Vector2(10f, 10f);
            scratchProperty.UseWheel = true;
            scratchProperty.Init(0, 1);
            scratchProperty.Value = new Vector2(ScratchDensity, ScratchTiling) * 100f;
            scratchProperty.OnValueChanged += (Vector2 value) =>
            {
                ScratchDensity.Value = value.x * 0.01f;
                ScratchTiling.Value = value.y * 0.01f;
            };
            return scratchProperty;
        }
        protected Vector2PropertyPanel GetVoid(UIComponent parent)
        {
            var voidProperty = ComponentPool.Get<Vector2PropertyPanel>(parent, nameof(ScratchDensity));
            voidProperty.Text = "Void";
            voidProperty.SetLabels("Density", "Scale");
            voidProperty.Format = Localize.NumberFormat_Percent;
            voidProperty.FieldsWidth = 50f;
            voidProperty.CanCollapse = false;
            voidProperty.CheckMax = true;
            voidProperty.CheckMin = true;
            voidProperty.MinValue = new Vector2(0f, 10f);
            voidProperty.MaxValue = new Vector2(100f, 1000f);
            voidProperty.WheelStep = new Vector2(10f, 10f);
            voidProperty.UseWheel = true;
            voidProperty.Init(0, 1);
            voidProperty.Value = new Vector2(VoidDensity, VoidTiling) * 100f;
            voidProperty.OnValueChanged += (Vector2 value) =>
            {
                VoidDensity.Value = value.x * 0.01f;
                VoidTiling.Value = value.y * 0.01f;
            };
            return voidProperty;
        }
    }

    public class StripeFillerStyle : GuideFillerStyle, IFollowGuideFiller, IRotateFiller, IWidthStyle, IColorStyle
    {
        public override StyleType Type => StyleType.FillerStripe;
        public override MarkingLOD SupportLOD => MarkingLOD.NoLOD;

        public PropertyValue<float> Angle { get; }
        public PropertyValue<bool> FollowGuides { get; }

        private static Dictionary<string, int> PropertyIndicesDic { get; } = CreatePropertyIndices(PropertyIndicesList);
        private static IEnumerable<string> PropertyIndicesList
        {
            get
            {
                yield return nameof(Color);
                yield return nameof(Width);
                yield return nameof(Step);
                yield return nameof(Angle);
                yield return nameof(Offset);
                yield return nameof(Guide);
                yield return nameof(ScratchDensity);
                yield return nameof(ScratchTiling);
                yield return nameof(VoidDensity);
                yield return nameof(VoidTiling);
#if DEBUG
                yield return nameof(RenderOnly);
                yield return nameof(Start);
                yield return nameof(End);
                yield return nameof(StartBorder);
                yield return nameof(EndBorder);
#endif
            }
        }
        public override Dictionary<string, int> PropertyIndices => PropertyIndicesDic;
        public override IEnumerable<IStylePropertyData> Properties
        {
            get
            {
                yield return new StylePropertyDataProvider<Color32>(nameof(Color), Color);
                yield return new StylePropertyDataProvider<float>(nameof(Width), Width);
                yield return new StylePropertyDataProvider<float>(nameof(Step), Step);
                yield return new StylePropertyDataProvider<float>(nameof(Angle), Angle);
                yield return new StylePropertyDataProvider<float>(nameof(LineOffset), LineOffset);
                yield return new StylePropertyDataProvider<float>(nameof(MedianOffset), MedianOffset);
                yield return new StylePropertyDataProvider<int>(nameof(LeftGuideA), LeftGuideA);
                yield return new StylePropertyDataProvider<int>(nameof(LeftGuideB), LeftGuideB);
                yield return new StylePropertyDataProvider<int>(nameof(RightGuideA), RightGuideA);
                yield return new StylePropertyDataProvider<int>(nameof(RightGuideB), RightGuideB);
                yield return new StylePropertyDataProvider<bool>(nameof(FollowGuides), FollowGuides);
            }
        }

        public StripeFillerStyle(Color32 color, float width, float lineOffset, float medianOffset, float angle, float step, bool followGuides = false) : base(color, width, step, lineOffset, medianOffset)
        {
            Angle = GetAngleProperty(angle);
            FollowGuides = GetFollowGuidesProperty(followGuides);
        }
        public override FillerStyle CopyStyle() => new StripeFillerStyle(Color, Width, LineOffset, DefaultOffset, DefaultAngle, Step, FollowGuides);
        public override void CopyTo(FillerStyle target)
        {
            base.CopyTo(target);

            if (target is IRotateFiller rotateTarget)
                rotateTarget.Angle.Value = Angle;

            if (target is IFollowGuideFiller followGuideTarget)
                followGuideTarget.FollowGuides.Value = FollowGuides;
        }
        public override void GetUIComponents(MarkingFiller filler, List<EditorItem> components, UIComponent parent, bool isTemplate = false)
        {
            base.GetUIComponents(filler, components, parent, isTemplate);

            if (!isTemplate)
            {
                components.Add(AddAngleProperty(this, parent, false));
            }
        }

        protected override ITrajectory[] GetGuides(MarkingFiller filler, ContourGroup contours)
        {
            if (FollowGuides)
                return base.GetGuides(filler, contours);
            else
                return new ITrajectory[] { GetGuide(contours.Limits, filler.Marking.Height, Angle) };
        }

#if DEBUG_PERIODIC_FILLER
        protected override List<Part> GetParts(ITrajectory guide, EdgeSetGroup contours, MarkingLOD lod, Action<IStyleData> addData)
#else
        protected override List<Part> GetParts(ITrajectory guide, ContourGroup contours, MarkingLOD lod)
#endif
        {
            var angle = FollowGuides ? 90f - Angle : 90f;
            var coef = Math.Max(Mathf.Sin(Mathf.Abs(angle) * Mathf.Deg2Rad), 0.01f);
            var width = Width.Value / coef;

            var limits = contours.GetLimits();
            var trajectories = GetPartTrajectories(guide, limits, width, width * (Step - 1));
            var parts = new List<Part>(trajectories.Count);

            if (!FollowGuides)
            {
                for (var i = 0; i < trajectories.Count; i += 1)
                {
                    var part = new Part(trajectories[i], null, null, angle, true);
                    if (part.CanIntersect(contours, true))
                        parts.Add(part);
                }
            }
            else
            {
                var borders = BorderPair.GetBorders(trajectories, contours, angle, true);
                for (var i = 0; i < borders.Count; i += 1)
                {
#if DEBUG
                    Border? startBorder = null;
                    Border? endBorder = null;
                    if (RenderOnly == -1 || i == RenderOnly)
                        borders[i].GetPartBorders(borders, i, out startBorder, out endBorder);
#else
                    borders[i].GetPartBorders(borders, i, out var startBorder, out var endBorder);
#endif
                    parts.Add(new Part(borders[i].trajectory, startBorder, endBorder, angle, true));
                }

#if DEBUG_PERIODIC_FILLER
                var dashes = new List<MarkingPartData>();

                for (var i = 0; i < borders.Count; i += 1)
                    borders[i].Draw(dashes);

                if (RenderOnly != -1 && parts.Count > RenderOnly)
                {
                    parts[RenderOnly].startBorder?.Draw(dashes, UnityEngine.Color.blue, 0.3f);
                    parts[RenderOnly].endBorder?.Draw(dashes, UnityEngine.Color.blue, 0.3f);
                }

                addData(new MarkingPartGroupData(lod, dashes));
#endif
            }

            return parts;
        }

        protected override float GetAngle() => 90f - Angle;

        public override void Render(MarkingFiller filler, OverlayData data)
        {
            if (FollowGuides)
                base.Render(filler, data);
        }

        public override XElement ToXml()
        {
            var config = base.ToXml();
            Angle.ToXml(config);
            FollowGuides.ToXml(config);
            return config;
        }
        public override void FromXml(XElement config, ObjectsMap map, bool invert, bool typeChanged)
        {
            base.FromXml(config, map, invert, typeChanged);
            Angle.FromXml(config, DefaultAngle);
            FollowGuides.FromXml(config, DefaultFollowGuides);
        }
    }
    public class ChevronFillerStyle : GuideFillerStyle, IWidthStyle, IColorStyle
    {
        public override StyleType Type => StyleType.FillerChevron;
        public override MarkingLOD SupportLOD => MarkingLOD.NoLOD;

        public PropertyValue<float> AngleBetween { get; }
        public PropertyBoolValue Invert { get; }
        public PropertyValue<int> Output { get; }
        public PropertyEnumValue<From> StartingFrom { get; }

        private static Dictionary<string, int> PropertyIndicesDic { get; } = CreatePropertyIndices(PropertyIndicesList);
        private static IEnumerable<string> PropertyIndicesList
        {
            get
            {
                yield return nameof(Color);
                yield return nameof(Width);
                yield return nameof(Step);
                yield return nameof(AngleBetween);
                yield return nameof(Offset);
                yield return nameof(Guide);
                yield return nameof(Invert);
                yield return nameof(ScratchDensity);
                yield return nameof(ScratchTiling);
                yield return nameof(VoidDensity);
                yield return nameof(VoidTiling);
#if DEBUG
                yield return nameof(RenderOnly);
                yield return nameof(Start);
                yield return nameof(End);
                yield return nameof(StartBorder);
                yield return nameof(EndBorder);
#endif
            }
        }
        public override Dictionary<string, int> PropertyIndices => PropertyIndicesDic;
        public override IEnumerable<IStylePropertyData> Properties
        {
            get
            {
                yield return new StylePropertyDataProvider<Color32>(nameof(Color), Color);
                yield return new StylePropertyDataProvider<float>(nameof(Width), Width);
                yield return new StylePropertyDataProvider<float>(nameof(Step), Step);
                yield return new StylePropertyDataProvider<float>(nameof(AngleBetween), AngleBetween);
                yield return new StylePropertyDataProvider<float>(nameof(LineOffset), LineOffset);
                yield return new StylePropertyDataProvider<float>(nameof(MedianOffset), MedianOffset);
                yield return new StylePropertyDataProvider<int>(nameof(LeftGuideA), LeftGuideA);
                yield return new StylePropertyDataProvider<int>(nameof(LeftGuideB), LeftGuideB);
                yield return new StylePropertyDataProvider<int>(nameof(RightGuideA), RightGuideA);
                yield return new StylePropertyDataProvider<int>(nameof(RightGuideB), RightGuideB);
                yield return new StylePropertyDataProvider<bool>(nameof(Invert), Invert);
            }
        }

        public ChevronFillerStyle(Color32 color, float width, float lineOffset, float medianOffset, float angleBetween, float step) : base(color, width, step, lineOffset, medianOffset)
        {
            AngleBetween = GetAngleBetweenProperty(angleBetween);
            Invert = GetInvertProperty(false);

            Output = GetOutputProperty(0);
            StartingFrom = GetStartingFromProperty(From.Vertex);
        }

        public override FillerStyle CopyStyle() => new ChevronFillerStyle(Color, Width, LineOffset, DefaultOffset, AngleBetween, Step);
        public override void CopyTo(FillerStyle target)
        {
            base.CopyTo(target);

            if (target is ChevronFillerStyle chevronTarget)
            {
                chevronTarget.AngleBetween.Value = AngleBetween;
                chevronTarget.Step.Value = Step;
                chevronTarget.Invert.Value = Invert;
            }
            if (target is IFollowGuideFiller followGuideTarget)
                followGuideTarget.FollowGuides.Value = true;
        }
        public override void GetUIComponents(MarkingFiller filler, List<EditorItem> components, UIComponent parent, bool isTemplate = false)
        {
            base.GetUIComponents(filler, components, parent, isTemplate);
            components.Add(AddAngleBetweenProperty(parent, false));
            if (!isTemplate)
            {
                components.Add(AddInvertProperty(parent, false));
            }
        }

        protected FloatPropertyPanel AddAngleBetweenProperty(UIComponent parent, bool canCollapse)
        {
            var angleProperty = ComponentPool.Get<FloatPropertyPanel>(parent, nameof(AngleBetween));
            angleProperty.Text = Localize.StyleOption_AngleBetween;
            angleProperty.Format = Localize.NumberFormat_Degree;
            angleProperty.UseWheel = true;
            angleProperty.WheelStep = 1f;
            angleProperty.WheelTip = Settings.ShowToolTip;
            angleProperty.CheckMin = true;
            angleProperty.MinValue = 30;
            angleProperty.CheckMax = true;
            angleProperty.MaxValue = 150;
            angleProperty.CanCollapse = canCollapse;
            angleProperty.Init();
            angleProperty.Value = AngleBetween;
            angleProperty.OnValueChanged += (float value) => AngleBetween.Value = value;

            return angleProperty;
        }
        protected ButtonPanel AddInvertProperty(UIComponent parent, bool canCollapse)
        {
            var buttonsPanel = ComponentPool.Get<ButtonPanel>(parent, nameof(Invert));
            buttonsPanel.Text = Localize.StyleOption_Invert;
            buttonsPanel.CanCollapse = canCollapse;
            buttonsPanel.Init();
            buttonsPanel.OnButtonClick += OnButtonClick;

            void OnButtonClick() => Invert.Value = !Invert;

            return buttonsPanel;
        }

#if DEBUG_PERIODIC_FILLER
        protected override List<Part> GetParts(ITrajectory guide, EdgeSetGroup contours, MarkingLOD lod, Action<IStyleData> addData)
#else
        protected override List<Part> GetParts(ITrajectory guide, ContourGroup contours, MarkingLOD lod)
#endif
        {
            var halfAngle = (Invert ? 360 - AngleBetween : AngleBetween) * 0.5f;
            var coef = Math.Max(Mathf.Sin(Mathf.Abs(halfAngle) * Mathf.Deg2Rad), 0.01f);
            var width = Width.Value / coef;

            var limits = contours.GetLimits();
            var trajectories = GetPartTrajectories(guide, limits, width, width * (Step - 1));
            var parts = new List<Part>(trajectories.Count * 2);

            var leftBorders = BorderPair.GetBorders(trajectories, contours, halfAngle - 180, false);
            for (var i = 0; i < leftBorders.Count; i += 1)
            {
                leftBorders[i].GetPartBorders(leftBorders, i, out var leftStartBorder, out var leftEndBorder);
                var part = new Part(leftBorders[i].trajectory, leftStartBorder, leftEndBorder, halfAngle, false);
                parts.Add(part);
            }

            var rightBorders = BorderPair.GetBorders(trajectories, contours, 180 - halfAngle, false);
            for (var i = 0; i < rightBorders.Count; i += 1)
            {
                rightBorders[i].GetPartBorders(rightBorders, i, out var rightStartBorder, out var rightEndBorder);
                var part = new Part(rightBorders[i].trajectory, rightStartBorder, rightEndBorder, -halfAngle, false);
                parts.Add(part);
            }

#if DEBUG_PERIODIC_FILLER
            var dashes = new List<MarkingPartData>();

            for (var i = 0; i < leftBorders.Count; i += 1)
                leftBorders[i].Draw(dashes);

            for (var i = 0; i < rightBorders.Count; i += 1)
                rightBorders[i].Draw(dashes);

            if (RenderOnly != -1 && parts.Count > RenderOnly)
            {
                parts[RenderOnly].startBorder?.Draw(dashes, UnityEngine.Color.blue, 0.3f);
                parts[RenderOnly].endBorder?.Draw(dashes, UnityEngine.Color.blue, 0.3f);
            }

            addData(new MarkingPartGroupData(lod, dashes));
#endif
            return parts;
        }
        protected override float GetAngle() => (Invert ? 360 - AngleBetween : AngleBetween) / 2;

        public override XElement ToXml()
        {
            var config = base.ToXml();
            AngleBetween.ToXml(config);
            Invert.ToXml(config);
            return config;
        }
        public override void FromXml(XElement config, ObjectsMap map, bool invert, bool typeChanged)
        {
            Output.FromXml(config, 0);
            StartingFrom.FromXml(config, From.Vertex);

            LeftGuideA.Value = Output;
            LeftGuideB.Value = Output + 1;

            if (StartingFrom == From.Vertex)
            {
                RightGuideA.Value = Output;
                RightGuideB.Value = Output - 1;
            }
            else if (StartingFrom == From.Edge)
            {
                RightGuideA.Value = Output - 1;
                RightGuideB.Value = Output - 2;
            }

            base.FromXml(config, map, invert, typeChanged);
            AngleBetween.FromXml(config, DefaultAngle);
            Invert.FromXml(config, false);
        }

        public enum From
        {
            [Description(nameof(Localize.StyleOption_Vertex))]
            Vertex = 0,

            [Description(nameof(Localize.StyleOption_Edge))]
            Edge = 1
        }
    }
    public class GridFillerStyle : PeriodicFillerStyle, IPeriodicFiller, IRotateFiller, IWidthStyle, IColorStyle
    {
        public override StyleType Type => StyleType.FillerGrid;
        public override MarkingLOD SupportLOD => MarkingLOD.NoLOD;

        public PropertyValue<float> Angle { get; }

        private static Dictionary<string, int> PropertyIndicesDic { get; } = CreatePropertyIndices(PropertyIndicesList);
        private static IEnumerable<string> PropertyIndicesList
        {
            get
            {
                yield return nameof(Color);
                yield return nameof(Width);
                yield return nameof(Step);
                yield return nameof(Angle);
                yield return nameof(Offset);
                yield return nameof(ScratchDensity);
                yield return nameof(ScratchTiling);
                yield return nameof(VoidDensity);
                yield return nameof(VoidTiling);
            }
        }
        public override Dictionary<string, int> PropertyIndices => PropertyIndicesDic;
        public override IEnumerable<IStylePropertyData> Properties
        {
            get
            {
                yield return new StylePropertyDataProvider<Color32>(nameof(Color), Color);
                yield return new StylePropertyDataProvider<float>(nameof(Width), Width);
                yield return new StylePropertyDataProvider<float>(nameof(Step), Step);
                yield return new StylePropertyDataProvider<float>(nameof(Angle), Angle);
                yield return new StylePropertyDataProvider<float>(nameof(LineOffset), LineOffset);
                yield return new StylePropertyDataProvider<float>(nameof(MedianOffset), MedianOffset);
            }
        }

        public GridFillerStyle(Color32 color, float width, float angle, float step, float lineOffset, float medianOffset) : base(color, width, step, lineOffset, medianOffset)
        {
            Angle = GetAngleProperty(angle);
        }

        public override FillerStyle CopyStyle() => new GridFillerStyle(Color, Width, DefaultAngle, Step, LineOffset, DefaultOffset);
        public override void CopyTo(FillerStyle target)
        {
            base.CopyTo(target);

            if (target is IRotateFiller rotateTarget)
                rotateTarget.Angle.Value = Angle;

            if (target is IPeriodicFiller periodicTarget)
                periodicTarget.Step.Value = Step;
        }
        public override void GetUIComponents(MarkingFiller filler, List<EditorItem> components, UIComponent parent, bool isTemplate = false)
        {
            base.GetUIComponents(filler, components, parent, isTemplate);
            components.Add(AddStepProperty(this, parent, false));
            if (!isTemplate)
                components.Add(AddAngleProperty(this, parent, false));
        }

        protected override ITrajectory[] GetGuides(MarkingFiller filler, ContourGroup contours)
        {
            return new ITrajectory[]
            {
                GetGuide(contours.Limits, filler.Marking.Height, Angle),
                GetGuide(contours.Limits, filler.Marking.Height, Angle < 0 ? Angle + 90 : Angle - 90)
            };
        }

#if DEBUG_PERIODIC_FILLER
        protected override List<Part> GetParts(ITrajectory guide, EdgeSetGroup contours, MarkingLOD lod, Action<IStyleData> addData)
#else
        protected override List<Part> GetParts(ITrajectory guide, ContourGroup contours, MarkingLOD lod)
#endif
        {
            var width = Width.Value;
            var trajectories = GetPartTrajectories(guide, contours.Limits, width, width * (Step - 1));

            var parts = new List<Part>(trajectories.Count);
            for (var i = 0; i < trajectories.Count; i += 1)
            {
                var part = new Part(trajectories[i], null, null, 90f, true);
                if (part.CanIntersect(contours, true))
                    parts.Add(part);
            }
            return parts;
        }

        public override XElement ToXml()
        {
            var config = base.ToXml();
            Angle.ToXml(config);
            Step.ToXml(config);
            return config;
        }
        public override void FromXml(XElement config, ObjectsMap map, bool invert, bool typeChanged)
        {
            base.FromXml(config, map, invert, typeChanged);
            Angle.FromXml(config, DefaultAngle);
            Step.FromXml(config, DefaultStepGrid);
        }
    }
    public class SolidFillerStyle : Filler2DStyle, IColorStyle, ITextureFiller
    {
        public static float DefaultSolidWidth { get; } = 0.2f;

        public override StyleType Type => StyleType.FillerSolid;
        public override MarkingLOD SupportLOD => MarkingLOD.LOD0 | MarkingLOD.LOD1;

#if DEBUG
        public new PropertyValue<float> MinAngle { get; }
        public new PropertyValue<float> MinLength { get; }
        public new PropertyValue<float> MaxLength { get; }
#endif

        private static Dictionary<string, int> PropertyIndicesDic { get; } = CreatePropertyIndices(PropertyIndicesList);
        private static IEnumerable<string> PropertyIndicesList
        {
            get
            {
                yield return nameof(Color);
                yield return nameof(Offset);
                yield return nameof(ScratchDensity);
                yield return nameof(ScratchTiling);
                yield return nameof(VoidDensity);
                yield return nameof(VoidTiling);
#if DEBUG
                yield return nameof(MinAngle);
                yield return nameof(MinLength);
                yield return nameof(MaxLength);
#endif
            }
        }
        public override Dictionary<string, int> PropertyIndices => PropertyIndicesDic;
        public override IEnumerable<IStylePropertyData> Properties
        {
            get
            {
                yield return new StylePropertyDataProvider<Color32>(nameof(Color), Color);
                yield return new StylePropertyDataProvider<float>(nameof(LineOffset), LineOffset);
                yield return new StylePropertyDataProvider<float>(nameof(MedianOffset), MedianOffset);
            }
        }

        public SolidFillerStyle(Color32 color, float lineOffset, float medianOffset) : base(color, DefaultSolidWidth, lineOffset, medianOffset)
        {
#if DEBUG
            MinAngle = new PropertyStructValue<float>(StyleChanged, FillerStyle.MinAngle);
            MinLength = new PropertyStructValue<float>(StyleChanged, FillerStyle.MinLength);
            MaxLength = new PropertyStructValue<float>(StyleChanged, FillerStyle.MaxLength);
#endif
        }

        public override FillerStyle CopyStyle() => new SolidFillerStyle(Color, LineOffset, DefaultOffset);

        protected override void CalculateImpl(MarkingFiller filler, ContourGroup contours, MarkingLOD lod, Action<IStyleData> addData)
        {
            if ((SupportLOD & lod) != 0)
            {
                foreach (var contour in contours)
                {
                    var trajectories = contour.Select(c => c.trajectory).ToArray();
                    foreach (var data in DecalData.GetData(lod, trajectories, MinAngle, MinLength, MaxLength, Color, Vector2.one, ScratchDensity, new Vector2(1f / ScratchTiling, 1f / ScratchTiling), VoidDensity, new Vector2(1f / VoidTiling, 1f / VoidTiling)))
                    {
                        addData(data);
                    }
                }
            }
        }

        public override void GetUIComponents(MarkingFiller filler, List<EditorItem> components, UIComponent parent, bool isTemplate = false)
        {
            base.GetUIComponents(filler, components, parent, isTemplate);
#if DEBUG
            components.Add(GetMinAngle(parent));
            components.Add(GetMinLength(parent));
            components.Add(GetMaxLength(parent));
#endif
        }
#if DEBUG
        private FloatPropertyPanel GetMinAngle(UIComponent parent)
        {
            var minAngleProperty = ComponentPool.Get<FloatPropertyPanel>(parent, nameof(MinAngle));
            minAngleProperty.Text = "Min angle";
            minAngleProperty.CanCollapse = true;
            minAngleProperty.CheckMax = true;
            minAngleProperty.CheckMin = true;
            minAngleProperty.MinValue = 1f;
            minAngleProperty.MaxValue = 90f;
            minAngleProperty.WheelStep = 1f;
            minAngleProperty.UseWheel = true;
            minAngleProperty.Init();
            minAngleProperty.Value = MinAngle;
            minAngleProperty.OnValueChanged += (float value) => MinAngle.Value = value;
            return minAngleProperty;
        }
        private FloatPropertyPanel GetMinLength(UIComponent parent)
        {
            var minLengthProperty = ComponentPool.Get<FloatPropertyPanel>(parent, nameof(MinLength));
            minLengthProperty.Text = "Min length";
            minLengthProperty.CanCollapse = true;
            minLengthProperty.CheckMax = true;
            minLengthProperty.CheckMin = true;
            minLengthProperty.MinValue = 0.1f;
            minLengthProperty.MaxValue = 10f;
            minLengthProperty.WheelStep = 0.1f;
            minLengthProperty.UseWheel = true;
            minLengthProperty.Init();
            minLengthProperty.Value = MinLength;
            minLengthProperty.OnValueChanged += (float value) => MinLength.Value = value;
            return minLengthProperty;
        }
        private FloatPropertyPanel GetMaxLength(UIComponent parent)
        {
            var maxLengthProperty = ComponentPool.Get<FloatPropertyPanel>(parent, nameof(MaxLength));
            maxLengthProperty.Text = "Max length";
            maxLengthProperty.CanCollapse = true;
            maxLengthProperty.CheckMax = true;
            maxLengthProperty.CheckMin = true;
            maxLengthProperty.MinValue = 1f;
            maxLengthProperty.MaxValue = 100f;
            maxLengthProperty.WheelStep = 0.1f;
            maxLengthProperty.UseWheel = true;
            maxLengthProperty.Init();
            maxLengthProperty.Value = MaxLength;
            maxLengthProperty.OnValueChanged += (float value) => MaxLength.Value = value;
            return maxLengthProperty;
        }
#endif
        public override XElement ToXml()
        {
            var config = base.ToXml();
            return config;
        }
        public override void FromXml(XElement config, ObjectsMap map, bool invert, bool typeChanged)
        {
            base.FromXml(config, map, invert, typeChanged);
        }
    }
}
