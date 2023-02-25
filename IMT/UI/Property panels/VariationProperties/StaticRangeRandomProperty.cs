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

        protected override void AddSelectorItems()
        {
            AddItem(RandomIndex, new OptionData(IMT.Localize.StyleOption_ObjectRandom, IMTTextures.Atlas, IMTTextures.RandomButtonIcon));
            base.AddSelectorItems();
        }

        public void SetRandom()
        {
            FieldA.Value = default;
            FieldB.Value = default;
            SelectedObject = RandomIndex;
            Refresh();
        }
        protected override void SelectorChangedImpl(int index)
        {
            if (index == RandomIndex)
                OnRandomValue?.Invoke();
            else
                base.SelectorChangedImpl(index);
        }

        protected override void RefreshImpl()
        {
            if (SelectedObject == RandomIndex)
            {
                FieldB.isVisible = false;
                FieldA.isEnabled = false;
                FieldA.width = FieldWidth;
                FieldA.Value = FieldA.Value;
            }
            else
            {
                FieldA.isEnabled = true;
                base.RefreshImpl();
            }
        }

        public override void DeInit()
        {
            base.DeInit();

            OnRandomValue = null;
        }
    }
    public class FloatStaticRangeRandomProperty : StaticRangeRandomProperty<float, FloatUITextField> { }
}
