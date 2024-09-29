using ColossalFramework.UI;
using IMT.Utilities;
using ModsCommon.UI;
using ModsCommon.Utilities;
using Mono.Cecil;
using System;

namespace IMT.UI
{
    public abstract class StaticAutoProperty<ValueType, FieldType, RefType> : VariationProperty<int, IntSegmented, ISingleSegmented<int>>
        where ValueType : IComparable<ValueType>
        where FieldType : ComparableUITextField<ValueType>, RefType
        where RefType : IComparableField<ValueType> 
    {
        protected FieldType Field { get; private set; }
        public RefType FieldRef => Field;

        protected virtual int AutoIndex => 0;
        protected virtual int StaticIndex => 1;

        public event Action<ValueType> OnValueChanged;
        public event Action OnAutoValue;

        public StaticAutoProperty()
        {
            Field = Content.AddUIComponent<FieldType>();
            Field.SetDefaultStyle();
            Field.name = nameof(Field);

            Field.OnValueChanged += ValueChanged;
        }

        protected override void AddSelectorItems()
        {
            AddItem(AutoIndex, new OptionData(IMT.Localize.StyleOption_ObjectAuto, IMTTextures.Atlas, IMTTextures.AutoButtonIcon));
            AddItem(StaticIndex, new OptionData(IMT.Localize.StyleOption_ObjectStatic, IMTTextures.Atlas, IMTTextures.SingleButtonIcon));
        }

        public override void DeInit()
        {
            base.DeInit();

            OnValueChanged = null;
            OnAutoValue = null;

            Field.SetDefault();
        }

        public void SetValue(ValueType value)
        {
            Field.Value = value;
            SelectedObject = StaticIndex;
            Refresh();
        }
        public void SetAuto()
        {
            Field.Value = default;
            SelectedObject = AutoIndex;
            Refresh();
        }

        protected override void SelectorChanged(int selectedItem)
        {
            base.SelectorChanged(selectedItem);

            Refresh();

            if (selectedItem == StaticIndex)
                OnValueChanged?.Invoke(Field.Value);
            else if (selectedItem == AutoIndex)
                OnAutoValue?.Invoke();
        }

        private void ValueChanged(ValueType value)
        {
            Refresh();
            if (SelectedObject == StaticIndex)
                OnValueChanged?.Invoke(value);
        }

        protected virtual void Refresh()
        {
            if (SelectedObject == StaticIndex)
                Field.isEnabled = true;
            else if (SelectedObject == AutoIndex)
                Field.isEnabled = false;
        }

        public void SimulateEnterValue(ValueType value) => Field.SimulateEnterValue(value);

        public override void SetStyle(ControlStyle style)
        {
            base.SetStyle(style);
            Field.TextFieldStyle = style.TextField;
        }
    }
    public class FloatStaticAutoProperty : StaticAutoProperty<float, FloatUITextField, IComparableField<float>> { }
}
