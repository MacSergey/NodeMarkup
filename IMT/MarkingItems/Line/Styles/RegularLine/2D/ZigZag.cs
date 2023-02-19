using ColossalFramework.UI;
using IMT.API;
using IMT.UI.Editors;
using IMT.Utilities;
using IMT.Utilities.API;
using ModsCommon.UI;
using ModsCommon.Utilities;
using System;
using System.Collections.Generic;
using System.Xml.Linq;
using UnityEngine;
using static IMT.Manager.StyleHelper;

namespace IMT.Manager
{
    public class ZigZagLineStyle : RegularLineStyle, IRegularLine, IEffectStyle
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
                yield return nameof(Texture);
                yield return nameof(Cracks);
                yield return nameof(Voids);
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
                yield return new StylePropertyDataProvider<float>(nameof(Texture), Texture);
                yield return new StylePropertyDataProvider<Vector2>(nameof(Cracks), Cracks);
                yield return new StylePropertyDataProvider<Vector2>(nameof(Voids), Voids);
            }
        }

        public PropertyStructValue<float> Step { get; }
        public new PropertyStructValue<float> Offset { get; }
        public PropertyBoolValue Side { get; }
        public PropertyBoolValue StartFrom { get; }

        public ZigZagLineStyle(Color32 color, float width, Vector2 cracks, Vector2 voids, float texture, float step, float offset, bool side, bool startFrom) : base(color, width, cracks, voids, texture)
        {
            Step = new PropertyStructValue<float>("S", StyleChanged, step);
            Offset = new PropertyStructValue<float>("O", StyleChanged, offset);
            Side = new PropertyBoolValue("SD", StyleChanged, side);
            StartFrom = new PropertyBoolValue("SF", StyleChanged, startFrom);
        }

        public override RegularLineStyle CopyLineStyle() => new ZigZagLineStyle(Color, Width, Cracks, Voids, Texture, Step, Offset, Side, StartFrom);
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

                    addData(new DecalData(MaterialType.Dash, lod, startPos, middlePos, Width, Color, DecalData.TextureData.Default, new DecalData.EffectData(this)));
                    addData(new DecalData(MaterialType.Dash, lod, middlePos, endPos, Width, Color, DecalData.TextureData.Default, new DecalData.EffectData(this)));
                }
                else
                {
                    var startPos = trajectory.Position(startT) + trajectory.Tangent(startT).MakeFlatNormalized().Turn90(!Side) * Offset;
                    var middlePos = trajectory.Position(middleT);
                    var endPos = trajectory.Position(endT) + trajectory.Tangent(endT).MakeFlatNormalized().Turn90(!Side) * Offset;

                    addData(new DecalData(MaterialType.Dash, lod, startPos, middlePos, Width, Color, DecalData.TextureData.Default, new DecalData.EffectData(this)));
                    addData(new DecalData(MaterialType.Dash, lod, middlePos, endPos, Width, Color, DecalData.TextureData.Default, new DecalData.EffectData(this)));
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


                addData(new DecalData(MaterialType.Dash, lod, startPos, startPos + startDir * Offset, Width, Color, DecalData.TextureData.Default, new DecalData.EffectData(this)));
                addData(new DecalData(MaterialType.Dash, lod, endPos, endPos + endDir * Offset, Width, Color, DecalData.TextureData.Default, new DecalData.EffectData(this)));
            }
        }

        protected override void GetUIComponents(MarkingRegularLine line, EditorProvider provider)
        {
            base.GetUIComponents(line, provider);

            provider.AddProperty(new PropertyInfo<FloatPropertyPanel>(this, nameof(Step), MainCategory, AddStepProperty));
            provider.AddProperty(new PropertyInfo<FloatPropertyPanel>(this, nameof(Offset), MainCategory, AddOffsetProperty));
            provider.AddProperty(new PropertyInfo<BoolListPropertyPanel>(this, nameof(Side), MainCategory, AddSideProperty));
            provider.AddProperty(new PropertyInfo<BoolListPropertyPanel>(this, nameof(StartFrom), MainCategory, AddStartFromProperty));
        }

        protected void AddStepProperty(FloatPropertyPanel stepProperty, EditorProvider provider)
        {
            stepProperty.Text = Localize.StyleOption_ZigzagStep;
            stepProperty.Format = Localize.NumberFormat_Meter;
            stepProperty.UseWheel = true;
            stepProperty.WheelStep = 0.1f;
            stepProperty.WheelTip = Settings.ShowToolTip;
            stepProperty.CheckMin = true;
            stepProperty.MinValue = 0.3f;
            stepProperty.Init();
            stepProperty.Value = Step;
            stepProperty.OnValueChanged += (value) => Step.Value = value;
        }
        new protected void AddOffsetProperty(FloatPropertyPanel offsetProperty, EditorProvider provider)
        {
            offsetProperty.Text = Localize.StyleOption_ZigzagOffset;
            offsetProperty.Format = Localize.NumberFormat_Meter;
            offsetProperty.UseWheel = true;
            offsetProperty.WheelStep = 0.1f;
            offsetProperty.WheelTip = Settings.ShowToolTip;
            offsetProperty.CheckMin = true;
            offsetProperty.MinValue = 0.1f;
            offsetProperty.Init();
            offsetProperty.Value = Offset;
            offsetProperty.OnValueChanged += (value) => Offset.Value = value;
        }
        protected void AddSideProperty(BoolListPropertyPanel sideProperty, EditorProvider provider)
        {
            sideProperty.Text = Localize.StyleOption_ZigzagSide;
            sideProperty.Init(Localize.StyleOption_SideLeft, Localize.StyleOption_SideRight);
            sideProperty.SelectedObject = Side;
            sideProperty.OnSelectObjectChanged += (value) => Side.Value = value;
        }
        protected void AddStartFromProperty(BoolListPropertyPanel startFromProperty, EditorProvider provider)
        {
            startFromProperty.Text = Localize.StyleOption_ZigzagStartFrom;
            startFromProperty.Init(Localize.StyleOption_ZigzagStartFromOutside, Localize.StyleOption_ZigzagStartFromLine);
            startFromProperty.SelectedObject = StartFrom;
            startFromProperty.OnSelectObjectChanged += (value) => StartFrom.Value = value;
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
            StartFrom.ToXml(config);
            return config;
        }
    }
}
