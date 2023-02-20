using IMT.Utilities;
using ModsCommon.UI;
using System;

namespace IMT.UI
{
    public abstract class StaticRangeAutoProperty<ValueType, FieldType> : StaticRangeProperty<ValueType, FieldType>
        where FieldType : ComparableUITextField<ValueType>
        where ValueType : IComparable<ValueType>
    {
        public event Action OnAutoValue;

        protected virtual int AutoIndex => 0;
        protected override int FirstOptionIndex => 1;
        protected override int SecondOptionIndex => 2;

        protected override void AddSelectorItems()
        {
            AddItem(AutoIndex, new OptionData(IMT.Localize.StyleOption_ObjectAuto, IMTTextures.Atlas, IMTTextures.AutoButtonIcon));
            base.AddSelectorItems();
        }

        public void SetAuto()
        {
            FieldA.Value = default;
            FieldB.Value = default;
            SelectedObject = AutoIndex;
            Refresh();
        }
        protected override void SelectorChangedImpl(int index)
        {
            if (index == AutoIndex)
                OnAutoValue?.Invoke();
            else
                base.SelectorChangedImpl(index);
        }

        protected override void RefreshImpl()
        {
            if (SelectedObject == AutoIndex)
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

            OnAutoValue = null;
        }
    }
    public class FloatStaticRangeAutoProperty : StaticRangeAutoProperty<float, FloatUITextField> { }
}
