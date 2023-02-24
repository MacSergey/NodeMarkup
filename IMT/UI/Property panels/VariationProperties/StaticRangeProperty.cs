using IMT.Utilities;
using ModsCommon.UI;
using System;

namespace IMT.UI
{
    public abstract class StaticRangeProperty<ValueType, FieldType> : BaseVariationProperty<ValueType, FieldType>
        where FieldType : ComparableUITextField<ValueType>
        where ValueType : IComparable<ValueType>
    {
        private bool allowInvert;
        public bool AllowInvert
        {
            get => allowInvert;
            set
            {
                if (value != allowInvert)
                {
                    allowInvert = value;
                    Refresh();
                }
            }
        }

        protected override void AddSelectorItems()
        {
            AddItem(FirstOptionIndex, new OptionData(IMT.Localize.StyleOption_ObjectStatic, IMTTextures.Atlas, IMTTextures.SingleButtonIcon));
            AddItem(SecondOptionIndex, new OptionData(IMT.Localize.StyleOption_ObjectRange, IMTTextures.Atlas, IMTTextures.RangeButtonIcon));
        }

        public override void DeInit()
        {
            base.DeInit();

            allowInvert = false;
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

                if (AllowInvert)
                {
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
                else
                {
                    FieldB.CheckMin = true;
                    FieldB.CheckMax = CheckMax;
                    FieldB.MinValue = FieldA.Value;
                    FieldB.MaxValue = MaxValue;
                    FieldB.CyclicalValue = false;
                    FieldB.Value = FieldB.Value;

                    FieldA.CheckMin = CheckMin;
                    FieldA.CheckMax = true;
                    FieldA.MinValue = MinValue;
                    FieldA.MaxValue = FieldB.Value;
                    FieldA.CyclicalValue = false;
                    FieldA.Value = FieldA.Value;
                }
            }
        }
    }

    public class FloatStaticRangeProperty : StaticRangeProperty<float, FloatUITextField> { }
}
