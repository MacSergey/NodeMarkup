using ColossalFramework.UI;
using IMT.API;
using IMT.UI;
using IMT.Utilities;
using IMT.Utilities.API;
using ModsCommon.UI;
using ModsCommon.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using UnityEngine;

namespace IMT.Manager
{
    public abstract class CustomCrosswalkStyle : CrosswalkStyle
    {
        public PropertyValue<float> OffsetBefore { get; }
        public PropertyValue<float> OffsetAfter { get; }
#if DEBUG
        public PropertyValue<int> RenderOnly { get; }
        public PropertyBoolValue Start { get; }
        public PropertyBoolValue End { get; }
        public PropertyBoolValue StartBorder { get; }
        public PropertyBoolValue EndBorder { get; }
#endif

        public override float GetTotalWidth(MarkingCrosswalk crosswalk) => OffsetBefore + GetVisibleWidth(crosswalk) + OffsetAfter;
        protected abstract float GetVisibleWidth(MarkingCrosswalk crosswalk);
        protected virtual float GetAbsoluteWidth(float length, MarkingCrosswalk crosswalk) => length / Mathf.Sin(crosswalk.CornerAndNormalAngle);

        public CustomCrosswalkStyle(Color32 color, float width, Vector2 cracks, Vector2 voids, float texture, float offsetBefore, float offsetAfter) : base(color, width, cracks, voids, texture)
        {
            OffsetBefore = GetOffsetBeforeProperty(offsetBefore);
            OffsetAfter = GetOffsetAfterProperty(offsetAfter);
#if DEBUG
            RenderOnly = new PropertyStructValue<int>(StyleChanged, -1);
            Start = new PropertyBoolValue(StyleChanged, true);
            End = new PropertyBoolValue(StyleChanged, true);
            StartBorder = new PropertyBoolValue(StyleChanged, true);
            EndBorder = new PropertyBoolValue(StyleChanged, true);
#endif
        }
        public override void CopyTo(CrosswalkStyle target)
        {
            base.CopyTo(target);

            if (target is CustomCrosswalkStyle customTarget)
            {
                customTarget.OffsetBefore.Value = OffsetBefore;
                customTarget.OffsetAfter.Value = OffsetAfter;
            }
        }

        protected bool GetContour(MarkingCrosswalk crosswalk, float offset, float width, out Contour result)
        {
            var cutContours = new Queue<Contour>();
            cutContours.Enqueue(crosswalk.Contour);

            var beforeOffset = offset - width * 0.5f;
            if (beforeOffset >= 0.05f)
            {
                var before = crosswalk.GetOffsetTrajectory(offset - width * 0.5f);
                cutContours.Process(in before, Intersection.Side.Right);
                if (cutContours.Count == 0)
                {
                    result = null;
                    return false;
                }
            }

            var after = crosswalk.GetOffsetTrajectory(offset + width * 0.5f);
            cutContours.Process(in after, Intersection.Side.Left);
            if (cutContours.Count == 0)
            {
                result = null;
                return false;
            }

            result = cutContours.Dequeue();
            return true;
        }

        public override void GetUIComponents(MarkingCrosswalk crosswalk, List<EditorItem> components, UIComponent parent, bool isTemplate = false)
        {
            base.GetUIComponents(crosswalk, components, parent, isTemplate);
            components.Add(AddOffsetProperty(parent, false));
#if DEBUG
            components.Add(GetRenderOnlyProperty(parent));
            components.Add(AddStartProperty(parent));
            components.Add(AddEndProperty(parent));
            components.Add(AddStartBorderProperty(parent));
            components.Add(AddEndBorderProperty(parent));
#endif
        }
#if DEBUG
        private IntPropertyPanel GetRenderOnlyProperty(UIComponent parent)
        {
            var property = ComponentPool.Get<IntPropertyPanel>(parent, nameof(RenderOnly));
            property.Text = "Render only";
            property.UseWheel = true;
            property.WheelStep = 1;
            property.WheelTip = Settings.ShowToolTip;
            property.CheckMin = true;
            property.MinValue = -1;
            property.Init();
            property.Value = RenderOnly;
            property.OnValueChanged += (int value) => RenderOnly.Value = value;

            return property;
        }
        protected BoolListPropertyPanel AddStartProperty(UIComponent parent)
        {
            var property = ComponentPool.Get<BoolListPropertyPanel>(parent, nameof(Start));
            property.Text = "Start";
            property.Init(Localize.StyleOption_No, Localize.StyleOption_Yes);
            property.SelectedObject = Start;
            property.OnSelectObjectChanged += (value) => Start.Value = value;
            return property;
        }
        protected BoolListPropertyPanel AddEndProperty(UIComponent parent)
        {
            var property = ComponentPool.Get<BoolListPropertyPanel>(parent, nameof(End));
            property.Text = "End";
            property.Init(Localize.StyleOption_No, Localize.StyleOption_Yes);
            property.SelectedObject = End;
            property.OnSelectObjectChanged += (value) => End.Value = value;
            return property;
        }
        protected BoolListPropertyPanel AddStartBorderProperty(UIComponent parent)
        {
            var property = ComponentPool.Get<BoolListPropertyPanel>(parent, nameof(StartBorder));
            property.Text = "Start border";
            property.Init(Localize.StyleOption_No, Localize.StyleOption_Yes);
            property.SelectedObject = StartBorder;
            property.OnSelectObjectChanged += (value) => StartBorder.Value = value;
            return property;
        }
        protected BoolListPropertyPanel AddEndBorderProperty(UIComponent parent)
        {
            var property = ComponentPool.Get<BoolListPropertyPanel>(parent, nameof(EndBorder));
            property.Text = "End border";
            property.Init(Localize.StyleOption_No, Localize.StyleOption_Yes);
            property.SelectedObject = EndBorder;
            property.OnSelectObjectChanged += (value) => EndBorder.Value = value;
            return property;
        }
#endif

        protected Vector2PropertyPanel AddOffsetProperty(UIComponent parent, bool canCollapse)
        {
            var offsetProperty = ComponentPool.Get<Vector2PropertyPanel>(parent, nameof(Offset));
            offsetProperty.Text = Localize.StyleOption_Offset;
            offsetProperty.FieldsWidth = 50f;
            offsetProperty.SetLabels(Localize.StyleOption_OffsetBeforeAbrv, Localize.StyleOption_OffsetAfterAbrv);
            offsetProperty.Format = Localize.NumberFormat_Meter;
            offsetProperty.UseWheel = true;
            offsetProperty.WheelStep = new Vector2(0.1f, 0.1f);
            offsetProperty.CheckMin = true;
            offsetProperty.WheelTip = Settings.ShowToolTip;
            offsetProperty.CanCollapse = canCollapse;
            offsetProperty.Init(0, 1);
            offsetProperty.Value = new Vector2(OffsetBefore, OffsetAfter);
            offsetProperty.OnValueChanged += (Vector2 value) =>
            {
                OffsetBefore.Value = value.x;
                OffsetAfter.Value = value.y;
            };

            return offsetProperty;
        }

        protected FloatPropertyPanel AddLineWidthProperty(ILinedCrosswalk linedStyle, UIComponent parent, bool canCollapse)
        {
            var widthProperty = ComponentPool.Get<FloatPropertyPanel>(parent, nameof(linedStyle.LineWidth));
            widthProperty.Text = Localize.StyleOption_LineWidth;
            widthProperty.Format = Localize.NumberFormat_Meter;
            widthProperty.UseWheel = true;
            widthProperty.WheelStep = 0.1f;
            widthProperty.WheelTip = Settings.ShowToolTip;
            widthProperty.CheckMin = true;
            widthProperty.MinValue = 0.05f;
            widthProperty.CanCollapse = canCollapse;
            widthProperty.Init();
            widthProperty.Value = linedStyle.LineWidth;
            widthProperty.OnValueChanged += (float value) => linedStyle.LineWidth.Value = value;

            return widthProperty;
        }

        public override XElement ToXml()
        {
            var config = base.ToXml();
            OffsetBefore.ToXml(config);
            OffsetAfter.ToXml(config);
            return config;
        }
        public override void FromXml(XElement config, ObjectsMap map, bool invert, bool typeChanged)
        {
            base.FromXml(config, map, invert, typeChanged);
            OffsetBefore.FromXml(config, DefaultCrosswalkOffset);
            OffsetAfter.FromXml(config, DefaultCrosswalkOffset);
        }
    }
}
