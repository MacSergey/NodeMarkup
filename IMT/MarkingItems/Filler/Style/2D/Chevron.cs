using ColossalFramework.DataBinding;
using ColossalFramework.Math;
using ColossalFramework.UI;
using IMT.API;
using IMT.UI.Editors;
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
using static IMT.Manager.StyleHelper;

namespace IMT.Manager
{
    public class ChevronFillerStyle : GuideFillerStyle, IWidthStyle, IColorStyle, IEffectStyle
    {
        public override StyleType Type => StyleType.FillerChevron;
        public override MarkingLOD SupportLOD => MarkingLOD.NoLOD;
        protected override float DefaultStep => DefaultStepStripe;

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
                yield return nameof(Texture);
                yield return nameof(Cracks);
                yield return nameof(Voids);
#if DEBUG
                yield return nameof(Debug);
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
                yield return new StylePropertyDataProvider<float>(nameof(Texture), Texture);
                yield return new StylePropertyDataProvider<Vector2>(nameof(Cracks), Cracks);
                yield return new StylePropertyDataProvider<Vector2>(nameof(Voids), Voids);
            }
        }

        public ChevronFillerStyle(Color32 color, float width, Vector2 cracks, Vector2 voids, float texture, float lineOffset, float medianOffset, float angleBetween, float step) : base(color, width, cracks, voids, texture, step, lineOffset, medianOffset)
        {
            AngleBetween = GetAngleBetweenProperty(angleBetween);
            Invert = GetInvertProperty(false);

            Output = GetOutputProperty(0);
            StartingFrom = GetStartingFromProperty(From.Vertex);
        }

        public override FillerStyle CopyStyle() => new ChevronFillerStyle(Color, Width, Cracks, Voids, Texture, LineOffset, DefaultOffset, AngleBetween, Step);
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

        protected override void GetUIComponents(MarkingFiller filler, EditorProvider provider)
        {
            base.GetUIComponents(filler, provider);
            provider.AddProperty(new PropertyInfo<FloatPropertyPanel>(this, nameof(AngleBetween), MainCategory, AddAngleBetweenProperty));
            if (!provider.isTemplate)
            {
                provider.AddProperty(new PropertyInfo<ButtonPanel>(this, nameof(Invert), MainCategory, AddInvertProperty));
            }
        }

        protected void AddAngleBetweenProperty(FloatPropertyPanel angleProperty, EditorProvider provider)
        {
            angleProperty.Text = Localize.StyleOption_AngleBetween;
            angleProperty.Format = Localize.NumberFormat_Degree;
            angleProperty.UseWheel = true;
            angleProperty.WheelStep = 1f;
            angleProperty.WheelTip = Settings.ShowToolTip;
            angleProperty.CheckMin = true;
            angleProperty.MinValue = 30;
            angleProperty.CheckMax = true;
            angleProperty.MaxValue = 150;
            angleProperty.Init();
            angleProperty.Value = AngleBetween;
            angleProperty.OnValueChanged += (float value) => AngleBetween.Value = value;
        }
        new protected void AddInvertProperty(ButtonPanel buttonsPanel, EditorProvider provider)
        {
            buttonsPanel.Text = Localize.StyleOption_Invert;
            buttonsPanel.Init();
            buttonsPanel.OnButtonClick += OnButtonClick;

            void OnButtonClick() => Invert.Value = !Invert;
        }

#if DEBUG_PERIODIC_FILLER
        protected override List<Part> GetParts(ITrajectory guide, EdgeSetGroup contours, MarkingLOD lod, Action<IStyleData> addData)
#else
        protected override List<Part> GetParts(ITrajectory guide, ContourGroup contours, MarkingLOD lod)
#endif
        {
            var halfAngle = (Invert ? 360 - AngleBetween : AngleBetween) * 0.5f;
            var ratio = Math.Max(Mathf.Sin(Mathf.Abs(halfAngle) * Mathf.Deg2Rad), 0.01f);
            var width = Width.Value / ratio;

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
        protected override float GetAngle() => (Invert ? 360 - AngleBetween : AngleBetween) * 0.5f;

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
}
