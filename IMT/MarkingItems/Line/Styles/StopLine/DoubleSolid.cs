﻿using IMT.API;
using IMT.UI;
using IMT.UI.Editors;
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
    public class DoubleSolidStopLineStyle : SolidStopLineStyle, IStopLine, IDoubleLine, IEffectStyle
    {
        public override StyleType Type => StyleType.StopLineDoubleSolid;
        public override MarkingLOD SupportLOD => MarkingLOD.LOD0 | MarkingLOD.LOD1;

        public PropertyBoolValue TwoColors { get; }
        public PropertyColorValue SecondColor { get; }
        public PropertyValue<float> Offset { get; }

        private static Dictionary<string, int> PropertyIndicesDic { get; } = CreatePropertyIndices(PropertyIndicesList);
        private static IEnumerable<string> PropertyIndicesList
        {
            get
            {
                yield return nameof(TwoColors);
                yield return nameof(Color);
                yield return nameof(SecondColor);
                yield return nameof(Width);
                yield return nameof(Offset);
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
                yield return new StylePropertyDataProvider<bool>(nameof(TwoColors), TwoColors);
                yield return new StylePropertyDataProvider<Color32>(nameof(Color), Color);
                yield return new StylePropertyDataProvider<Color32>(nameof(SecondColor), SecondColor);
                yield return new StylePropertyDataProvider<float>(nameof(Width), Width);
                yield return new StylePropertyDataProvider<float>(nameof(Offset), Offset);
                yield return new StylePropertyDataProvider<float>(nameof(Texture), Texture);
                yield return new StylePropertyDataProvider<Vector2>(nameof(Cracks), Cracks);
                yield return new StylePropertyDataProvider<Vector2>(nameof(Voids), Voids);
            }
        }

        public DoubleSolidStopLineStyle(Color32 color, Color32 secondColor, bool useSecondColor, float width, Vector2 cracks, Vector2 voids, float texture, float offset) : base(color, width, cracks, voids, texture)
        {
            TwoColors = GetTwoColorsProperty(useSecondColor);
            SecondColor = GetSecondColorProperty(TwoColors ? secondColor : color);
            Offset = GetOffsetProperty(offset);
        }
        protected override void CalculateImpl(MarkingStopLine stopLine, ITrajectory trajectory, MarkingLOD lod, Action<IStyleData> addData)
        {
            var offsetNormal = ((stopLine.Start.Direction + stopLine.End.Direction) / -2).normalized;
            var offsetLeft = offsetNormal * (Width * 0.5f);
            var offsetRight = offsetNormal * (Width * 0.5f + 2 * Offset);

            var parts = StyleHelper.CalculateSolid(trajectory, lod, StyleHelper.SplitParams.Default);
            foreach (var part in parts)
            {
                StyleHelper.GetPartParams(trajectory, part, offsetLeft, offsetLeft, out var leftStartPos, out var leftEndPos, out var leftDir);
                var left = new DecalData(MaterialType.Dash, lod, leftStartPos, leftEndPos, Width, Color, DecalData.TextureData.Default, new DecalData.EffectData(this));
                addData(left);

                StyleHelper.GetPartParams(trajectory, part, offsetRight, offsetRight, out var rightStartPos, out var rightEndPos, out var rightDir);
                var right = new DecalData(MaterialType.Dash, lod, rightStartPos, rightEndPos, Width, TwoColors ? SecondColor : Color, DecalData.TextureData.Default, new DecalData.EffectData(this));
                addData(right);
            }
        }

        public override StopLineStyle CopyLineStyle() => new DoubleSolidStopLineStyle(Color, SecondColor, TwoColors, Width, Cracks, Voids, Texture, Offset);
        public override void CopyTo(LineStyle target)
        {
            base.CopyTo(target);
            if (target is IDoubleLine doubleTarget)
            {
                doubleTarget.Offset.Value = Offset;
                doubleTarget.SecondColor.Value = SecondColor;
                doubleTarget.TwoColors.Value = TwoColors;
            }
        }

        protected override void GetUIComponents(MarkingStopLine line, EditorProvider provider)
        {
            base.GetUIComponents(line, provider);

            provider.AddProperty(new PropertyInfo<BoolPropertyPanel>(this, nameof(TwoColors), AdditionalCategory, AddUseSecondColorProperty));
            provider.AddProperty(new PropertyInfo<IMTColorPropertyPanel>(this, nameof(SecondColor), AdditionalCategory, AddSecondColorProperty, RefreshSecondColorProperty));
            provider.AddProperty(new PropertyInfo<FloatPropertyPanel>(this, nameof(Offset), MainCategory, AddOffsetProperty));

            //UseSecondColorChanged(this, parent, TwoColors);
        }

        public override XElement ToXml()
        {
            var config = base.ToXml();
            TwoColors.ToXml(config);
            SecondColor.ToXml(config);
            Offset.ToXml(config);
            return config;
        }
        public override void FromXml(XElement config, ObjectsMap map, bool invert, bool typeChanged)
        {
            base.FromXml(config, map, invert, typeChanged);
            TwoColors.FromXml(config, false);
            SecondColor.FromXml(config, DefaultMarkingColor);
            Offset.FromXml(config, DefaultDoubleOffset);
        }
    }
}
