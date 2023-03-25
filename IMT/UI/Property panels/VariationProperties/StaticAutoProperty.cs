using ColossalFramework.UI;
using IMT.Utilities;
using ModsCommon.UI;
using ModsCommon.Utilities;
using System;

namespace IMT.UI
{
    public abstract class StaticAutoProperty<ValueType, FieldType> : VariationProperty<int, IntSegmented>
    where FieldType : ComparableUITextField<ValueType>
    where ValueType : IComparable<ValueType>
    {
        protected FieldType Field { get; set; }

        protected virtual int AutoIndex => 0;
        protected virtual int StaticIndex => 1;

        public event Action<ValueType> OnValueChanged;
        public event Action OnAutoValue;

        public bool SubmitOnFocusLost
        {
            get => Field.submitOnFocusLost;
            set => Field.submitOnFocusLost = value;
        }
        public ValueType Value
        {
            get => Field.Value;
            set
            {
                Field.Value = value;
                Refresh();
            }
        }
        public string Format
        {
            set => Field.Format = value;
        }

        public ValueType MinValue
        {
            get => Field.MinValue;
            set => Field.MinValue = value;
        }
        public ValueType MaxValue
        {
            get => Field.MaxValue;
            set => Field.MaxValue = value;
        }
        public bool CheckMin
        {
            get => Field.CheckMin;
            set => Field.CheckMin = value;
        }
        public bool CheckMax
        {
            get => Field.CheckMax;
            set => Field.CheckMax = value;
        }
        public bool CyclicalValue
        {
            get => Field.CyclicalValue;
            set => Field.CyclicalValue = value;
        }
        public bool UseWheel
        {
            get => Field.UseWheel;
            set => Field.UseWheel = value;
        }
        public ValueType WheelStep
        {
            set => Field.WheelStep = value;
        }
        public bool WheelTip
        {
            set => Field.WheelTip = value;
        }

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

            UseWheel = false;
            WheelStep = default;
            WheelTip = false;
            SubmitOnFocusLost = true;
            Format = null;

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

        protected override void SelectorChangedImpl(int index)
        {
            if (index == StaticIndex)
                OnValueChanged?.Invoke(Field.Value);
            else if (index == AutoIndex)
                OnAutoValue?.Invoke();
        }

        private void ValueChanged(ValueType value)
        {
            Refresh();
            if (SelectedObject == StaticIndex)
                OnValueChanged?.Invoke(value);
        }

        protected override void Refresh()
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
            Field.SetStyle(style.TextField);
        }
    }
    public class FloatStaticAutoProperty : StaticAutoProperty<float, FloatUITextField> { }
}
