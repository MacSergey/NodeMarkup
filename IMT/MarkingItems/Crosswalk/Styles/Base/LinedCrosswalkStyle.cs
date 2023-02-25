using IMT.UI.Editors;
using IMT.Utilities;
using ModsCommon.UI;
using ModsCommon.Utilities;
using System.Xml.Linq;
using UnityEngine;

namespace IMT.Manager
{
    public abstract class LinedCrosswalkStyle : CustomCrosswalkStyle, ICrosswalkStyle, ILinedCrosswalk
    {
        public bool KeepColor => true;
        public PropertyValue<float> LineWidth { get; }

        public LinedCrosswalkStyle(Color32 color, float width, Vector2 cracks, Vector2 voids, float texture, float offsetBefore, float offsetAfter, float lineWidth) : base(color, width, cracks, voids, texture, offsetBefore, offsetAfter)
        {
            LineWidth = GetLineWidthProperty(lineWidth);
        }
        protected override float GetVisibleWidth(MarkingCrosswalk crosswalk) => Width / Mathf.Sin(crosswalk.CornerAndNormalAngle);
        public override void CopyTo(BaseCrosswalkStyle target)
        {
            base.CopyTo(target);
            if (target is ILinedCrosswalk linedTarget)
                linedTarget.LineWidth.Value = LineWidth;
        }

        protected override void GetUIComponents(MarkingCrosswalk crosswalk, EditorProvider provider)
        {
            base.GetUIComponents(crosswalk, provider);
            provider.AddProperty(new PropertyInfo<FloatPropertyPanel>(this, nameof(LineWidth), MainCategory, AddLineWidthProperty));
        }

        protected void AddLineWidthProperty(FloatPropertyPanel widthProperty, EditorProvider provider)
        {
            widthProperty.Label = Localize.StyleOption_LineWidth;
            widthProperty.Format = Localize.NumberFormat_Meter;
            widthProperty.UseWheel = true;
            widthProperty.WheelStep = 0.1f;
            widthProperty.WheelTip = Settings.ShowToolTip;
            widthProperty.CheckMin = true;
            widthProperty.MinValue = 0.05f;
            widthProperty.Init();
            widthProperty.Value = LineWidth;
            widthProperty.OnValueChanged += (float value) => LineWidth.Value = value;
        }

        public override XElement ToXml()
        {
            var config = base.ToXml();
            LineWidth.ToXml(config);
            return config;
        }
        public override void FromXml(XElement config, ObjectsMap map, bool invert, bool typeChanged)
        {
            base.FromXml(config, map, invert, typeChanged);
            LineWidth.FromXml(config, DefaultCrosswalkOffset);
        }
    }
}
