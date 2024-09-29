using ModsCommon.UI;
using System;
using UnityEngine;

namespace IMT.UI
{
    public abstract class SingleDoubleProperty<ValueType, FieldType, RangeType, RefType> : VariationProperty<int, IntSegmented, ISingleSegmented<int>>
        where ValueType : IComparable<ValueType>
        where FieldType : ComparableUITextField<ValueType>
        where RangeType : ValueFieldRange<ValueType, FieldType, RefType>, RefType
        where RefType : IValueFieldRange<ValueType>
    {

        private OptionData FirstOptionData { get; set; }
        private OptionData SecondOptionData { get; set; }

        protected virtual int FirstOptionIndex => 0;
        protected virtual int SecondOptionIndex => 1;

        public event Action<ValueType, ValueType> OnValueChanged;

        protected RangeType Range { get; private set; }

        public RefType RangeRef => Range;

        protected override void FillContent()
        {
            base.FillContent();

            Range = Content.AddUIComponent<RangeType>();
            Range.SetDefaultStyle();
            Range.name = nameof(Range);
            Range.OnValueChanged += ValueChanged;
        }

        public void Init(OptionData firstOptionData, OptionData secondOptionData) 
        {
            FirstOptionData = firstOptionData;
            SecondOptionData = secondOptionData;
            base.Init();
        }
        protected override void AddSelectorItems()
        {
            AddItem(FirstOptionIndex, FirstOptionData);
            AddItem(SecondOptionIndex, SecondOptionData);
        }

        protected void ValueChanged(ValueType valueA, ValueType valueB)
        {
            OnValueChanged?.Invoke(valueA, valueB);
        }

        public void SetValues(ValueType valueA, ValueType valueB) => SetValues(valueA.CompareTo(valueB) == 0 ? FirstOptionIndex : SecondOptionIndex, valueA, valueB);
        private void SetValues(int index, ValueType valueA, ValueType valueB)
        {
            Range.Mode = valueA.CompareTo(valueB) == 0 ? RangeMode.Single : RangeMode.Range;
            Selector.SelectedObject = index;
            Range.SetValues(valueA, valueB);
        }

        protected override void SelectorChanged(int index)
        {
            base.SelectorChanged(index);

            SetValues(index, Range.ValueA, Range.ValueB);
            ValueChanged(Range.ValueA, Range.ValueB);
        }

        public override void SetStyle(ControlStyle style)
        {
            base.SetStyle(style);
            Range.SetStyle(style);
        }
    }
    public class FloatSingleDoubleProperty : SingleDoubleProperty<float, FloatUITextField, FloatRangeField, IValueFieldRange<float>> { }   
}
