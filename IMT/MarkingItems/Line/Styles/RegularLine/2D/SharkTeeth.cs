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
    public class SharkTeethLineStyle : RegularLineStyle, IColorStyle, IAsymLine, ISharkLine
    {
        public override StyleType Type => StyleType.LineSharkTeeth;
        public override MarkingLOD SupportLOD => MarkingLOD.LOD0 | MarkingLOD.LOD1;
        protected override float LodWidth => 0.5f;

        public PropertyValue<float> Base { get; }
        public PropertyValue<float> Height { get; }
        public PropertyValue<float> Space { get; }
        public PropertyBoolValue Invert { get; }
        public PropertyValue<float> Angle { get; }

        private static Dictionary<string, int> PropertyIndicesDic { get; } = CreatePropertyIndices(PropertyIndicesList);
        private static IEnumerable<string> PropertyIndicesList
        {
            get
            {
                yield return nameof(Color);
                yield return nameof(Width);
                yield return nameof(Triangle);
                yield return nameof(Space);
                yield return nameof(Angle);
                yield return nameof(Invert);
            }
        }
        public override Dictionary<string, int> PropertyIndices => PropertyIndicesDic;
        public override IEnumerable<IStylePropertyData> Properties
        {
            get
            {
                yield return new StylePropertyDataProvider<Color32>(nameof(Color), Color);
                yield return new StylePropertyDataProvider<float>(nameof(Width), Width);
                yield return new StylePropertyDataProvider<float>(nameof(Base), Base);
                yield return new StylePropertyDataProvider<float>(nameof(Height), Height);
                yield return new StylePropertyDataProvider<float>(nameof(Space), Space);
                yield return new StylePropertyDataProvider<float>(nameof(Angle), Angle);
                yield return new StylePropertyDataProvider<bool>(nameof(Invert), Invert);
            }
        }

        public SharkTeethLineStyle(Color32 color, float baseValue, float height, float space, float angle) : base(color, 0)
        {
            Base = GetBaseProperty(baseValue);
            Height = GetHeightProperty(height);
            Space = GetSpaceProperty(space);
            Invert = GetInvertProperty(true);
            Angle = GetAngleProperty(angle);
        }
        protected override void CalculateImpl(MarkingRegularLine line, ITrajectory trajectory, MarkingLOD lod, Action<IStyleData> addData)
        {
            if (CheckDashedLod(lod, Height, Base))
            {
                var borders = line.Borders;
                var ratio = Mathf.Cos(Angle * Mathf.Deg2Rad);
                addData(new MarkingPartGroupData(lod, StyleHelper.CalculateDashed(trajectory, Base / ratio, Space / ratio, CalculateDashes)));

                IEnumerable<MarkingPartData> CalculateDashes(ITrajectory trajectory, float startT, float endT)
                {
                    if (StyleHelper.CalculateDashedParts(borders, trajectory, Invert ? endT : startT, Invert ? startT : endT, Base, Height / (Invert ? 2 : -2), Height, Color, out MarkingPartData dash))
                    {
                        dash.Material = RenderHelper.MaterialLib[MaterialType.Triangle];
                        dash.Angle -= Angle * Mathf.Deg2Rad;
                        yield return dash;
                    }
                }
            }
        }

        public override RegularLineStyle CopyLineStyle() => new SharkTeethLineStyle(Color, Base, Height, Space, Angle);
        public override void CopyTo(LineStyle target)
        {
            base.CopyTo(target);
            if (target is SharkTeethLineStyle sharkTeethTarget)
            {
                sharkTeethTarget.Base.Value = Base;
                sharkTeethTarget.Height.Value = Height;
                sharkTeethTarget.Space.Value = Space;
            }
        }
        public override void GetUIComponents(MarkingRegularLine line, List<EditorItem> components, UIComponent parent, bool isTemplate = false)
        {
            base.GetUIComponents(line, components, parent, isTemplate);
            components.Add(AddTriangleProperty(this, parent, false));
            components.Add(AddSpaceProperty(this, parent, false));
            components.Add(AddAngleProperty(parent, true));

            if (!isTemplate)
                components.Add(AddInvertProperty(parent, false));
        }

        protected FloatPropertyPanel AddAngleProperty(UIComponent parent, bool canCollapse)
        {
            var angleProperty = ComponentPool.Get<FloatPropertyPanel>(parent, nameof(Angle));
            angleProperty.Text = Localize.StyleOption_SharkToothAngle;
            angleProperty.Format = Localize.NumberFormat_Degree;
            angleProperty.UseWheel = true;
            angleProperty.WheelStep = 1f;
            angleProperty.CheckMin = true;
            angleProperty.MinValue = -60f;
            angleProperty.CheckMax = true;
            angleProperty.MaxValue = 60f;
            angleProperty.CanCollapse = canCollapse;
            angleProperty.Init();
            angleProperty.Value = Angle;
            angleProperty.OnValueChanged += (value) => Angle.Value = value;

            return angleProperty;
        }
        protected ButtonPanel AddInvertProperty(UIComponent parent, bool canCollapse)
        {
            var invertButton = ComponentPool.Get<ButtonPanel>(parent, nameof(Invert));
            invertButton.Text = Localize.StyleOption_Invert;
            invertButton.CanCollapse = canCollapse;
            invertButton.Init();

            invertButton.OnButtonClick += () =>
            {
                Invert.Value = !Invert;
                Angle.Value = -Angle;
                if (parent.Find<FloatPropertyPanel>(nameof(Angle)) is FloatPropertyPanel angleProperty)
                    angleProperty.Value = Angle;
            };

            return invertButton;
        }

        public override XElement ToXml()
        {
            var config = base.ToXml();
            Base.ToXml(config);
            Height.ToXml(config);
            Space.ToXml(config);
            Invert.ToXml(config);
            Angle.ToXml(config);
            return config;
        }
        public override void FromXml(XElement config, ObjectsMap map, bool invert, bool typeChanged)
        {
            base.FromXml(config, map, invert, typeChanged);
            Base.FromXml(config, DefaultSharkBaseLength);
            Height.FromXml(config, DefaultSharkHeight);
            Space.FromXml(config, DefaultSharkSpaceLength);
            Angle.FromXml(config, DefaultSharkAngle);
            Invert.FromXml(config, false);
            Invert.Value ^= map.Invert ^ invert ^ typeChanged;
        }
    }
}
