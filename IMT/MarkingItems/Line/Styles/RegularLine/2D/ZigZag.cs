using ColossalFramework.UI;
using IMT.API;
using IMT.Utilities;
using IMT.Utilities.API;
using ModsCommon.UI;
using ModsCommon.Utilities;
using System;
using System.Collections.Generic;
using System.Xml.Linq;
using UnityEngine;

namespace IMT.Manager
{
    public class ZigZagLineStyle : RegularLineStyle, IRegularLine
    {
        public override StyleType Type => StyleType.LineZigZag;

        public override MarkingLOD SupportLOD => MarkingLOD.LOD0 | MarkingLOD.LOD1;

        private static Dictionary<string, int> PropertyIndicesDic { get; } = CreatePropertyIndices(PropertyIndicesList);
        private static IEnumerable<string> PropertyIndicesList
        {
            get
            {
                yield return nameof(Color);
                yield return nameof(Width);
                yield return nameof(Step);
                yield return nameof(Offset);
                yield return nameof(Side);
                yield return nameof(StartFrom);
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
                yield return new StylePropertyDataProvider<float>(nameof(Offset), Offset);
                yield return new StylePropertyDataProvider<bool>(nameof(Side), Side);
                yield return new StylePropertyDataProvider<bool>(nameof(StartFrom), StartFrom);
            }
        }

        public PropertyStructValue<float> Step { get; }
        public new PropertyStructValue<float> Offset { get; }
        public PropertyBoolValue Side { get; }
        public PropertyBoolValue StartFrom { get; }

        public ZigZagLineStyle(Color32 color, float width, float step, float offset, bool side, bool startFrom) : base(color, width)
        {
            Step = new PropertyStructValue<float>("S", StyleChanged, step);
            Offset = new PropertyStructValue<float>("O", StyleChanged, offset);
            Side = new PropertyBoolValue("SD", StyleChanged, side);
            StartFrom = new PropertyBoolValue("SF", StyleChanged, startFrom);
        }

        public override RegularLineStyle CopyLineStyle() => new ZigZagLineStyle(Color, Width, Step, Offset, Side, StartFrom);
        public override void CopyTo(LineStyle target)
        {
            base.CopyTo(target);

            if (target is ZigZagLineStyle zigzagTarget)
            {
                zigzagTarget.Step.Value = Step;
                zigzagTarget.Offset.Value = Offset;
                zigzagTarget.Side.Value = Side;
                zigzagTarget.StartFrom.Value = StartFrom;
            }
        }

        protected override void CalculateImpl(MarkingRegularLine line, ITrajectory trajectory, MarkingLOD lod, Action<IStyleData> addData)
        {
            var count = Mathf.FloorToInt(trajectory.Length / Step.Value);
            var startOffset = (trajectory.Length - Step.Value * count) * 0.5f;

            var dashes = new MarkingPartData[StartFrom ? count * 2 : count * 2 + 2];

            for (int i = 0; i < count; i += 1)
            {
                var startDistance = startOffset + Step.Value * i;
                var endDistance = startDistance + Step.Value;
                var middleDistance = (startDistance + endDistance) * 0.5f;
                var startT = trajectory.Travel(startDistance);
                var endT = trajectory.Travel(endDistance);
                var middleT = trajectory.Travel(middleDistance);

                if (StartFrom)
                {
                    var startPos = trajectory.Position(startT);
                    var middlePos = trajectory.Position(middleT) + trajectory.Tangent(middleT).MakeFlatNormalized().Turn90(!Side) * Offset;
                    var endPos = trajectory.Position(endT);

                    dashes[2 * i] = new MarkingPartData(startPos, middlePos, Width, Color, RenderHelper.MaterialLib[MaterialType.RectangleLines]);
                    dashes[2 * i + 1] = new MarkingPartData(middlePos, endPos, Width, Color, RenderHelper.MaterialLib[MaterialType.RectangleLines]);
                }
                else
                {
                    var startPos = trajectory.Position(startT) + trajectory.Tangent(startT).MakeFlatNormalized().Turn90(!Side) * Offset;
                    var middlePos = trajectory.Position(middleT);
                    var endPos = trajectory.Position(endT) + trajectory.Tangent(endT).MakeFlatNormalized().Turn90(!Side) * Offset;

                    dashes[2 * i] = new MarkingPartData(startPos, middlePos, Width, Color, RenderHelper.MaterialLib[MaterialType.RectangleLines]);
                    dashes[2 * i + 1] = new MarkingPartData(middlePos, endPos, Width, Color, RenderHelper.MaterialLib[MaterialType.RectangleLines]);
                }
            }

            if (!StartFrom)
            {
                var startT = trajectory.Travel(startOffset);
                var endT = trajectory.Travel(startOffset + Step.Value * count);

                var startPos = trajectory.Position(startT);
                var startDir = trajectory.Tangent(startT).MakeFlatNormalized().Turn90(!Side);
                var endPos = trajectory.Position(endT);
                var endDir = trajectory.Tangent(endT).MakeFlatNormalized().Turn90(!Side);

                dashes[count * 2] = new MarkingPartData(startPos, startPos + startDir * Offset, Width, Color, RenderHelper.MaterialLib[MaterialType.RectangleLines]);
                dashes[count * 2 + 1] = new MarkingPartData(endPos, endPos + endDir * Offset, Width, Color, RenderHelper.MaterialLib[MaterialType.RectangleLines]);
            }

            addData(new MarkingPartGroupData(lod, dashes));
        }

        public override void GetUIComponents(MarkingRegularLine line, List<EditorItem> components, UIComponent parent, bool isTemplate = false)
        {
            base.GetUIComponents(line, components, parent, isTemplate);
            components.Add(AddStepProperty(parent, false));
            components.Add(AddOffsetProperty(parent, false));
            components.Add(AddSideProperty(parent, false));
            components.Add(AddStartFromProperty(parent, false));
        }
        protected FloatPropertyPanel AddStepProperty(UIComponent parent, bool canCollapse)
        {
            var stepProperty = ComponentPool.Get<FloatPropertyPanel>(parent, nameof(Step));
            stepProperty.Text = Localize.StyleOption_ZigzagStep;
            stepProperty.Format = Localize.NumberFormat_Meter;
            stepProperty.UseWheel = true;
            stepProperty.WheelStep = 0.1f;
            stepProperty.WheelTip = Settings.ShowToolTip;
            stepProperty.CheckMin = true;
            stepProperty.MinValue = 0.3f;
            stepProperty.CanCollapse = canCollapse;
            stepProperty.Init();
            stepProperty.Value = Step;
            stepProperty.OnValueChanged += (value) => Step.Value = value;

            return stepProperty;
        }
        protected FloatPropertyPanel AddOffsetProperty(UIComponent parent, bool canCollapse)
        {
            var offsetProperty = ComponentPool.Get<FloatPropertyPanel>(parent, nameof(Offset));
            offsetProperty.Text = Localize.StyleOption_ZigzagOffset;
            offsetProperty.Format = Localize.NumberFormat_Meter;
            offsetProperty.UseWheel = true;
            offsetProperty.WheelStep = 0.1f;
            offsetProperty.WheelTip = Settings.ShowToolTip;
            offsetProperty.CheckMin = true;
            offsetProperty.MinValue = 0.1f;
            offsetProperty.CanCollapse = canCollapse;
            offsetProperty.Init();
            offsetProperty.Value = Offset;
            offsetProperty.OnValueChanged += (value) => Offset.Value = value;

            return offsetProperty;
        }
        protected BoolListPropertyPanel AddSideProperty(UIComponent parent, bool canCollapse)
        {
            var sideProperty = ComponentPool.Get<BoolListPropertyPanel>(parent, nameof(Side));
            sideProperty.Text = Localize.StyleOption_ZigzagSide;
            sideProperty.CanCollapse = canCollapse;
            sideProperty.Init(Localize.StyleOption_SideLeft, Localize.StyleOption_SideRight);
            sideProperty.SelectedObject = Side;
            sideProperty.OnSelectObjectChanged += (value) => Side.Value = value;
            return sideProperty;
        }
        protected BoolListPropertyPanel AddStartFromProperty(UIComponent parent, bool canCollapse)
        {
            var startFromProperty = ComponentPool.Get<BoolListPropertyPanel>(parent, nameof(Side));
            startFromProperty.Text = Localize.StyleOption_ZigzagStartFrom;
            startFromProperty.CanCollapse = canCollapse;
            startFromProperty.Init(Localize.StyleOption_ZigzagStartFromOutside, Localize.StyleOption_ZigzagStartFromLine);
            startFromProperty.SelectedObject = StartFrom;
            startFromProperty.OnSelectObjectChanged += (value) => StartFrom.Value = value;
            return startFromProperty;
        }

        public override void FromXml(XElement config, ObjectsMap map, bool invert, bool typeChanged)
        {
            base.FromXml(config, map, invert, typeChanged);
            Step.FromXml(config, 1f);
            Offset.FromXml(config, 1f);
            Side.FromXml(config, true);
            StartFrom.FromXml(config, true);

            if (typeChanged)
                Side.Value = !Side.Value;
        }
        public override XElement ToXml()
        {
            var config = base.ToXml();
            Step.ToXml(config);
            Offset.ToXml(config);
            Side.ToXml(config);
            return config;
        }
    }
}
