using IMT.Utilities;
using ModsCommon.UI;
using System;

namespace IMT.UI
{
    public abstract class StaticRangeRandomProperty<ValueType, FieldType> : StaticRangeProperty<ValueType, FieldType>
        where FieldType : ComparableUITextField<ValueType>
        where ValueType : IComparable<ValueType>
    {
        public event Action OnRandomValue;

        protected virtual int RandomIndex => 0;
        protected override int FirstOptionIndex => 1;
        protected override int SecondOptionIndex => 2;

        protected override void AddSelector1Items()
        {
            AddItem1(RandomIndex, new OptionData(IMT.Localize.StyleOption_ObjectRandom, IMTTextures.Atlas, IMTTextures.RandomButtonIcon));
            base.AddSelector1Items();
        }

        public void SetRandom()
        {
            FieldA.Value = default;
            FieldB.Value = default;
            SelectedObject1 = RandomIndex;
            Refresh();
        }
        protected override void Selector1ChangedImpl(int index)
        {
            if (index == RandomIndex)
                OnRandomValue?.Invoke();
            else
                base.Selector1ChangedImpl(index);
        }

        protected override void Refresh()
        {
            if (SelectedObject1 == RandomIndex)
            {
                FieldB.isVisible = false;
                FieldA.isEnabled = false;
                FieldA.width = FieldWidth;
                FieldA.Value = FieldA.Value;
            }
            else
            {
                FieldA.isEnabled = true;
            }

            base.Refresh();
        }

        public override void DeInit()
        {
            base.DeInit();

            OnRandomValue = null;
        }
    }
    public class FloatStaticRangeRandomProperty : StaticRangeRandomProperty<float, FloatUITextField> { }
}
