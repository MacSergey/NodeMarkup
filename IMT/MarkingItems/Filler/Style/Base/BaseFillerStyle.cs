using ColossalFramework.DataBinding;
using ColossalFramework.UI;
using IMT.UI;
using IMT.UI.Editors;
using IMT.Utilities;
using ModsCommon.UI;
using ModsCommon.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Linq;
using UnityEngine;

namespace IMT.Manager
{
    public interface IFillerStyle : IStyle
    {
        PropertyVector2Value Offset { get; }
    }
    public interface IPeriodicFiller : IFillerStyle
    {
        PropertyValue<float> Step { get; }
    }
    public interface IRotateFiller : IFillerStyle
    {
        PropertyValue<float> Angle { get; }
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
    public abstract class BaseFillerStyle : Style<BaseFillerStyle>, IFillerStyle
    {
        public static float DefaultAngle => 0f;
        public static float DefaultStepStripe => 3f;
        public static float DefaultStepGrid => 6f;
        public static Vector2 DefaultOffset => Vector2.zero;
        public static float StripeDefaultWidth => 0.5f;
        public static float DefaultAngleBetween => 90f;
        public static float DefaultElevation => 0.3f;
        public static Vector2 DefaultCornerRadius => Vector2.zero;
        public static Vector2 DefaultCurbSize => Vector2.zero;
        public static bool DefaultFollowGuides => false;

        protected static StyleHelper.SplitParams SplitParams => new StyleHelper.SplitParams()
        {
            minAngle = 5f,
            minLength = 0.5f,
            maxLength = 10f,
            maxHeight = 3f,
        };

        protected static string Guide => nameof(Guide);

        private static Dictionary<FillerType, BaseFillerStyle> Defaults { get; } = new Dictionary<FillerType, BaseFillerStyle>()
        {
            {FillerType.Stripe, new StripeFillerStyle(DefaultColor, StripeDefaultWidth, DefaultEffect, DefaultEffect, DefaultTexture, DefaultOffset, DefaultAngle, DefaultStepStripe,  DefaultFollowGuides)},
            {FillerType.Grid, new GridFillerStyle(DefaultColor, DefaultWidth, DefaultEffect, DefaultEffect, DefaultTexture, DefaultAngle, DefaultStepGrid, DefaultOffset)},
            {FillerType.Solid, new SolidFillerStyle(DefaultColor, DefaultEffect, DefaultEffect, DefaultTexture, DefaultOffset)},
            {FillerType.Chevron, new ChevronFillerStyle(DefaultColor, StripeDefaultWidth, DefaultEffect, DefaultEffect, DefaultTexture, DefaultOffset, DefaultAngleBetween, DefaultStepStripe)},
            {FillerType.Decal, new DecalFillerStyle(null, DefaultColor, DefaultOffset, Vector2.one, 0f)},
            {FillerType.Pavement, new PavementFillerStyle(DefaultOffset, DefaultElevation, DefaultCornerRadius)},
            {FillerType.Grass, new GrassFillerStyle(DefaultOffset, DefaultElevation, DefaultCornerRadius, DefaultCurbSize)},
            {FillerType.Gravel, new GravelFillerStyle(DefaultOffset, DefaultElevation, DefaultCornerRadius, DefaultCurbSize)},
            {FillerType.Ruined, new RuinedFillerStyle(DefaultOffset, DefaultElevation, DefaultCornerRadius, DefaultCurbSize)},
            {FillerType.Cliff, new CliffFillerStyle(DefaultOffset, DefaultElevation, DefaultCornerRadius, DefaultCurbSize)},
            {FillerType.Texture, new CustomTextureFillerStyle(null, null, DefaultOffset, DefaultElevation, DefaultCornerRadius, DefaultCurbSize, Vector2.one, 0f)},
        };
        public static BaseFillerStyle GetDefault(FillerType type)
        {
            return Defaults.TryGetValue(type, out var style) ? style.CopyStyle() : null;
        }

        protected override float WidthWheelStep => 0.1f;
        protected override float WidthMinValue => 0.1f;

        public new PropertyVector2Value Offset { get; }
        public float LineOffset => Offset.Value.x;
        public float MedianOffset => Offset.Value.y;

        public BaseFillerStyle(Color32 color, float width, Vector2 cracks, Vector2 voids, float texture, Vector2 offset) : base(color, width, cracks, voids, texture)
        {
            Offset = new PropertyVector2Value(StyleChanged, offset, "O", "MO");
        }
        public BaseFillerStyle(Color32 color, float width, Vector2 offset) : base(color, width)
        {
            Offset = new PropertyVector2Value(StyleChanged, offset, "O", "MO");
        }

        public override void CopyTo(BaseFillerStyle target)
        {
            base.CopyTo(target);
            if (target is IFillerStyle fillerTarget)
            {
                fillerTarget.Offset.Value = Offset;
            }
        }

        public sealed override void GetUIComponents(EditorProvider provider)
        {
            base.GetUIComponents(provider);
            if (provider.editor.EditObject is MarkingFiller filler)
                GetUIComponents(filler, provider);
            else
                GetUIComponents(null, provider);
        }
        protected virtual void GetUIComponents(MarkingFiller filler, EditorProvider provider)
        {
            if (!provider.isTemplate)
            {
                if (!filler.IsMedian)
                    provider.AddProperty(new PropertyInfo<FloatPropertyPanel>(this, nameof(Offset), MainCategory, AddLineOffsetProperty));
                else
                    provider.AddProperty(new PropertyInfo<Vector2PropertyPanel>(this, nameof(Offset), MainCategory, AddMedianOffsetProperty));
            }
        }

        public virtual void Calculate(MarkingFiller filler, Action<IStyleData> addData)
        {
            var contours = GetContours(filler);
            foreach (var lod in EnumExtension.GetEnumValues<MarkingLOD>())
            {
                CalculateImpl(filler, contours, lod, addData);
            }
        }
        protected virtual ContourGroup GetContours(MarkingFiller filler)
        {
            var originalContour = filler.Contour.Edges;
            var contourSets = originalContour.SetOffset(LineOffset, MedianOffset);
            return contourSets;
        }
        protected abstract void CalculateImpl(MarkingFiller filler, ContourGroup contourSets, MarkingLOD lod, Action<IStyleData> addData);

        public virtual void Render(MarkingFiller filler, OverlayData data) { }

        public override XElement ToXml()
        {
            var config = base.ToXml();
            Offset.ToXml(config);
            return config;
        }
        public override void FromXml(XElement config, ObjectsMap map, bool invert, bool typeChanged)
        {
            base.FromXml(config, map, invert, typeChanged);
            Offset.FromXml(config, DefaultOffset);
        }

        protected void AddLineOffsetProperty(FloatPropertyPanel offsetProperty, EditorProvider provider)
        {
            offsetProperty.Text = Localize.StyleOption_Offset;
            offsetProperty.Format = Localize.NumberFormat_Meter;
            offsetProperty.UseWheel = true;
            offsetProperty.WheelStep = 0.1f;
            offsetProperty.WheelTip = Settings.ShowToolTip;
            offsetProperty.CheckMin = true;
            offsetProperty.MinValue = 0f;
            offsetProperty.Init();
            offsetProperty.Value = LineOffset;
            offsetProperty.OnValueChanged += (float value) => Offset.Value = new Vector2(value, value);
        }
        private void AddMedianOffsetProperty(Vector2PropertyPanel offsetProperty, EditorProvider provider)
        {
            offsetProperty.Text = Localize.StyleOption_Offset;
            offsetProperty.FieldsWidth = 50f;
            offsetProperty.SetLabels(Localize.StyleOption_LineOffsetAbrv, Localize.StyleOption_MedianOffsetAbrv);
            offsetProperty.Format = Localize.NumberFormat_Meter;
            offsetProperty.UseWheel = true;
            offsetProperty.WheelStep = new Vector2(0.1f, 0.1f);
            offsetProperty.WheelTip = Settings.ShowToolTip;
            offsetProperty.CheckMin = true;
            offsetProperty.MinValue = new Vector2(0f, 0f);
            offsetProperty.Init(0, 1);
            offsetProperty.Value = Offset;
            offsetProperty.OnValueChanged += (Vector2 value) => Offset.Value = value;
        }
        protected void AddAngleProperty(FloatPropertyPanel angleProperty, EditorProvider provider)
        {
            if (this is IRotateFiller rotateStyle)
            {
                angleProperty.Text = Localize.StyleOption_Angle;
                angleProperty.Format = Localize.NumberFormat_Degree;
                angleProperty.UseWheel = true;
                angleProperty.WheelStep = 1f;
                angleProperty.WheelTip = Settings.ShowToolTip;
                angleProperty.CheckMin = true;
                angleProperty.MinValue = -90;
                angleProperty.CheckMax = true;
                angleProperty.MaxValue = 90;
                angleProperty.CyclicalValue = true;
                angleProperty.Init();
                angleProperty.Value = rotateStyle.Angle;
                angleProperty.OnValueChanged += (float value) => rotateStyle.Angle.Value = value;
            }
            else
                throw new NotSupportedException();
        }

        public enum FillerType
        {
            [Description(nameof(Localize.FillerStyle_Stripe))]
            [Order(0)]
            Stripe = StyleType.FillerStripe,

            [Description(nameof(Localize.FillerStyle_Grid))]
            [Order(2)]
            Grid = StyleType.FillerGrid,

            [Description(nameof(Localize.FillerStyle_Solid))]
            [Order(3)]
            Solid = StyleType.FillerSolid,

            [Description(nameof(Localize.FillerStyle_Chevron))]
            [Order(1)]
            Chevron = StyleType.FillerChevron,

            [Description(nameof(Localize.FillerStyle_Decal))]
            [Order(4)]
            Decal = StyleType.FillerDecal,

            [Description(nameof(Localize.FillerStyle_Pavement))]
            [Order(100)]
            Pavement = StyleType.FillerPavement,

            [Description(nameof(Localize.FillerStyle_Grass))]
            [Order(101)]
            Grass = StyleType.FillerGrass,

            [Description(nameof(Localize.FillerStyle_Gravel))]
            [Order(102)]
            Gravel = StyleType.FillerGravel,

            [Description(nameof(Localize.FillerStyle_Ruined))]
            [Order(103)]
            Ruined = StyleType.FillerRuined,

            [Description(nameof(Localize.FillerStyle_Cliff))]
            [Order(104)]
            Cliff = StyleType.FillerCliff,

            [Description("Texture")]
            [Order(105)]
            Texture = StyleType.FillerTexture,

            [Description(nameof(Localize.Style_FromClipboard))]
            [NotVisible]
            Buffer = StyleType.FillerBuffer,
        }
    }
}
