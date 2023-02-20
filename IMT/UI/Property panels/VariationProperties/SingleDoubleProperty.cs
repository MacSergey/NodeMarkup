using ModsCommon.UI;
using System;

namespace IMT.UI
{
    public abstract class SingleDoubleProperty<ValueType, FieldType> : BaseVariationProperty<ValueType, FieldType>
        where FieldType : ComparableUITextField<ValueType>
        where ValueType : IComparable<ValueType>
    {
        private OptionData FirstOptionData { get; set; }
        private OptionData SecondOptionData { get; set; }

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

        protected override void OnSetValue()
        {
            SelectedObject = FieldA.Value.CompareTo(FieldB.Value) == 0 ? FirstOptionIndex : SecondOptionIndex;
        }

        protected override void SelectorChangedImpl(int index)
        {
            if (index == FirstOptionIndex)
                ValueChanged(FieldA.Value, FieldA.Value);
            else if (index == SecondOptionIndex)
                ValueChanged(FieldA.Value, FieldB.Value);
        }
        protected override void OnValueAChanged(ValueType value)
        {
            if (SelectedObject == FirstOptionIndex)
                ValueChanged(value, value);
            else if (SelectedObject == SecondOptionIndex)
                ValueChanged(value, FieldB.Value);
        }

        protected override void OnValueBChanged(ValueType value)
        {
            if (SelectedObject == FirstOptionIndex)
                ValueChanged(FieldA.Value, FieldA.Value);
            else if (SelectedObject == SecondOptionIndex)
                ValueChanged(FieldA.Value, value);
        }

        protected override void RefreshImpl()
        {
            if (SelectedObject == FirstOptionIndex)
            {
                FieldB.isVisible = false;
                FieldA.width = FieldWidth;

                FieldA.CheckMin = CheckMin;
                FieldA.CheckMax = CheckMax;
                FieldA.MinValue = MinValue;
                FieldA.MaxValue = MaxValue;
                FieldA.CyclicalValue = CyclicalValue;
                FieldA.Value = FieldA.Value;
            }
            else if (SelectedObject == SecondOptionIndex)
            {
                FieldB.isVisible = true;
                FieldA.width = (FieldWidth - Content.autoLayoutPadding.horizontal) * 0.5f;
                FieldB.width = (FieldWidth - Content.autoLayoutPadding.horizontal) * 0.5f;

                FieldA.CheckMin = CheckMin;
                FieldA.CheckMax = CheckMax;
                FieldA.MinValue = MinValue;
                FieldA.MaxValue = MaxValue;
                FieldA.CyclicalValue = CyclicalValue;
                FieldA.Value = FieldA.Value;

                FieldB.CheckMin = CheckMin;
                FieldB.CheckMax = CheckMax;
                FieldB.MinValue = MinValue;
                FieldB.MaxValue = MaxValue;
                FieldB.CyclicalValue = CyclicalValue;
                FieldB.Value = FieldB.Value;
            }
        }
    }
    public class FloatSingleDoubleProperty : SingleDoubleProperty<float, FloatUITextField> { }   
}
