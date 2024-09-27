using IMT.Manager;
using IMT.Utilities;
using ModsCommon.UI;
using System;

namespace IMT.UI
{
    public abstract class StaticRangeProperty<ValueType, FieldType> : BaseTwoVariationProperty<ValueType, FieldType>
        where FieldType : ComparableUITextField<ValueType>
        where ValueType : IComparable<ValueType>
    {
        public event Action<Spread> OnSpreadChanged;

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

        protected override void AddSelector1Items()
        {
            AddItem1(FirstOptionIndex, new OptionData(IMT.Localize.StyleOption_ObjectStatic, IMTTextures.Atlas, IMTTextures.SingleButtonIcon));
            AddItem1(SecondOptionIndex, new OptionData(IMT.Localize.StyleOption_ObjectRange, IMTTextures.Atlas, IMTTextures.RangeButtonIcon));
            
        }
        protected override void AddSelector2Items()
        {
            AddItem2((int)Spread.Random, new OptionData(IMT.Localize.StyleOption_ObjectSpreadRandom, IMTTextures.Atlas, IMTTextures.RandomButtonIcon));
            AddItem2((int)Spread.Slope, new OptionData(IMT.Localize.StyleOption_ObjectSpreadSlope, IMTTextures.Atlas, IMTTextures.LeftToRightButtonIcon));
        }

        public override void DeInit()
        {
            base.DeInit();

            allowInvert = false;
        }

        protected override void OnSetValue()
        {
            SelectedObject1 = FieldA.Value.CompareTo(FieldB.Value) == 0 ? FirstOptionIndex : SecondOptionIndex;
        }

        protected override void Selector1ChangedImpl(int index)
        {
            base.Selector1ChangedImpl(index);

            if (index == FirstOptionIndex)
            {
                ValueChanged(FieldA.Value, FieldA.Value);
            }
            else if (index == SecondOptionIndex)
            {
                ValueChanged(FieldA.Value, FieldB.Value);
            }
        }
        protected override void Selector2ChangedImpl(int index)
        {
            base.Selector2ChangedImpl(index);
            SpreadChanged((Spread)index);
        }

        protected override void OnValueAChanged(ValueType value)
        {
            if (SelectedObject1 == FirstOptionIndex)
            {
                ValueChanged(value, value);
            }
            else if (SelectedObject1 == SecondOptionIndex)
            {
                ValueChanged(value, FieldB.Value);
            }
        }
        protected override void OnValueBChanged(ValueType value)
        {
            if (SelectedObject1 == FirstOptionIndex)
            {
                ValueChanged(FieldA.Value, FieldA.Value);
            }
            else if (SelectedObject1 == SecondOptionIndex)
            {
                ValueChanged(FieldA.Value, value);
            }
        }

        protected override void Refresh()
        {
            if (SelectedObject1 == FirstOptionIndex)
            {
                SetOneField();
                Selector2.isVisible = false;
            }
            else if (SelectedObject1 == SecondOptionIndex)
            {
                SetTwoFields();
                Selector2.isVisible = true;
            }
            else
            {
                Selector2.isVisible = false;
            }
        }

        protected void SpreadChanged(Spread spread) => OnSpreadChanged?.Invoke(spread);

        public void SetSpread(Spread spread)
        {
            SelectedObject2 = (int)spread;
        }

        protected void SetOneField()
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
        protected void SetTwoFields()
        {
            FieldB.isVisible = true;
            FieldA.width = (FieldWidth - Content.AutoLayoutSpace) * 0.5f;
            FieldB.width = (FieldWidth - Content.AutoLayoutSpace) * 0.5f;

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

    public class FloatStaticRangeProperty : StaticRangeProperty<float, FloatUITextField> { }
}
