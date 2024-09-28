using ColossalFramework.UI;
using ModsCommon.UI;
using ModsCommon.Utilities;
using System;

namespace IMT.UI
{
    public abstract class SingleDoubleInvertedProperty<ValueType, FieldType, FieldRefType, RangeType, RangeRefType> : SingleDoubleProperty<ValueType, FieldType, FieldRefType, RangeType, RangeRefType>
        where ValueType : IComparable<ValueType>
        where FieldType : ComparableUITextField<ValueType, FieldRefType>
        where FieldRefType : IFieldRef, IComparableField<ValueType>
        where RangeType : ValueFieldRange<ValueType, FieldType, FieldRefType, RangeRefType>
        where RangeRefType : IFieldRef, IValueFieldRange<ValueType, FieldRefType>
    {
        protected CustomUIButton Invert { get; }

        public SingleDoubleInvertedProperty()
        {
            Invert = Content.AddUIComponent<CustomUIButton>();
            Invert.SetDefaultStyle();
            Invert.width = 20;
            Invert.IconAtlas = CommonTextures.Atlas;
            Invert.AllIconSprites = CommonTextures.PlusMinusButton;
            Invert.eventClick += InvertClick;
        }

        private void InvertClick(UIComponent component, UIMouseEventParameter eventParam)
        {
            Range.ValueA = InvertValue(Range.ValueA);
            Range.ValueB = InvertValue(Range.ValueB);
            
            if (SelectedObject == FirstOptionIndex)
                ValueChanged(Range.ValueA, Range.ValueA);
            else if (SelectedObject == SecondOptionIndex)
                ValueChanged(Range.ValueA, Range.ValueB);
        }
        protected abstract ValueType InvertValue(ValueType value);

        protected override void OnSizeChanged()
        {
            base.OnSizeChanged();
            SetSize();
        }
        protected virtual void SetSize()
        {
            if (Invert != null)
                Invert.height = Content.height - ItemsPadding * 2;
        }

        public override void SetStyle(ControlStyle style)
        {
            base.SetStyle(style);

            Invert.ButtonStyle = style.SmallButton;
            Invert.IconAtlas = CommonTextures.Atlas;
            Invert.AllIconSprites = CommonTextures.PlusMinusButton;
        }
    }
    public class FloatSingleDoubleInvertedProperty : SingleDoubleInvertedProperty<float, FloatUITextField, FloatUITextField.FloatFieldRef, FloatRangeField, FloatRangeField.FloatRangeFieldRef>
    {
        protected override float InvertValue(float value) => -value;
    }
}
