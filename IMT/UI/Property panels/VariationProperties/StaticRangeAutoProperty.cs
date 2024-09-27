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

        protected override void AddSelector1Items()
        {
            AddItem1(AutoIndex, new OptionData(IMT.Localize.StyleOption_ObjectAuto, IMTTextures.Atlas, IMTTextures.AutoButtonIcon));
            base.AddSelector1Items();
        }

        public void SetAuto()
        {
            FieldA.Value = default;
            FieldB.Value = default;
            SelectedObject1 = AutoIndex;
            Refresh();
        }
        protected override void Selector1ChangedImpl(int index)
        {
            if (index == AutoIndex)
                OnAutoValue?.Invoke();
            else
                base.Selector1ChangedImpl(index);
        }

        protected override void Refresh()
        {
            if (SelectedObject1 == AutoIndex)
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

            OnAutoValue = null;
        }
    }
    public class FloatStaticRangeAutoProperty : StaticRangeAutoProperty<float, FloatUITextField> { }
}
