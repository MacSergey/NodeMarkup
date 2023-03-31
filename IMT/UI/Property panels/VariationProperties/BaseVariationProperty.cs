using ModsCommon.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IMT.UI
{
    public abstract class BaseVariationProperty<ValueType, FieldType> : VariationProperty<int, IntSegmented>
        where FieldType : ComparableUITextField<ValueType>
        where ValueType : IComparable<ValueType>
    {
        protected FieldType FieldA { get; set; }
        protected FieldType FieldB { get; set; }

        protected virtual int FirstOptionIndex => 0;
        protected virtual int SecondOptionIndex => 1;

        public event Action<ValueType, ValueType> OnValueChanged;

        private const float defaultFieldWidth = 100f;
        private float fieldWidth = defaultFieldWidth;
        public float FieldWidth
        {
            get => fieldWidth;
            set
            {
                if (value != fieldWidth)
                {
                    fieldWidth = value;
                    Refresh();
                }
            }
        }
        public bool SubmitOnFocusLost
        {
            get => FieldA.submitOnFocusLost && FieldB.submitOnFocusLost;
            set
            {
                FieldA.submitOnFocusLost = value;
                FieldB.submitOnFocusLost = value;
            }
        }
        public ValueType ValueA
        {
            get => FieldA;
            set
            {
                FieldA.Value = value;
                Refresh();
            }
        }
        public ValueType ValueB
        {
            get => FieldB;
            set
            {
                FieldB.Value = value;
                Refresh();
            }
        }
        public string Format
        {
            set
            {
                FieldA.Format = value;
                FieldB.Format = value;
            }
        }

        private ValueType minValue;
        private ValueType maxValue;
        private bool checkMin;
        private bool checkMax;
        private bool cyclicalValue;

        public ValueType MinValue
        {
            get => minValue;
            set
            {
                if (value.CompareTo(minValue) != 0)
                {
                    minValue = value;
                    Refresh();
                }
            }
        }
        public ValueType MaxValue
        {
            get => maxValue;
            set
            {
                if (value.CompareTo(maxValue) != 0)
                {
                    maxValue = value;
                    Refresh();
                }
            }
        }
        public bool CheckMin
        {
            get => checkMin;
            set
            {
                if (value != checkMin)
                {
                    checkMin = value;
                    Refresh();
                }
            }
        }
        public bool CheckMax
        {
            get => checkMax;
            set
            {
                if (value != checkMax)
                {
                    checkMax = value;
                    Refresh();
                }
            }
        }
        public bool CyclicalValue
        {
            get => cyclicalValue;
            set
            {
                if (value != cyclicalValue)
                {
                    cyclicalValue = value;
                    Refresh();
                }
            }
        }


        public bool UseWheel
        {
            get => FieldA.UseWheel && FieldB.UseWheel;
            set
            {
                FieldA.UseWheel = value;
                FieldB.UseWheel = value;
            }
        }
        public ValueType WheelStep
        {
            set
            {
                FieldA.WheelStep = value;
                FieldB.WheelStep = value;
            }
        }
        public bool WheelTip
        {
            set
            {
                FieldA.WheelTip = value;
                FieldB.WheelTip = value;
            }
        }

        public BaseVariationProperty()
        {
            FieldA = Content.AddUIComponent<FieldType>();
            FieldA.SetDefaultStyle();
            FieldA.name = nameof(FieldA);

            FieldB = Content.AddUIComponent<FieldType>();
            FieldB.SetDefaultStyle();
            FieldB.name = nameof(FieldB);

            FieldA.OnValueChanged += ValueAChanged;
            FieldB.OnValueChanged += ValueBChanged;
        }

        public override void DeInit()
        {
            base.DeInit();

            OnValueChanged = null;

            fieldWidth = defaultFieldWidth;
            checkMin = false;
            checkMax = false;
            minValue = default;
            maxValue = default;
            cyclicalValue = false;

            UseWheel = false;
            WheelStep = default;
            WheelTip = false;
            SubmitOnFocusLost = true;
            Format = null;

            FieldA.SetDefault();
            FieldB.SetDefault();
        }

        protected void ValueChanged(ValueType valueA, ValueType valueB) => OnValueChanged?.Invoke(valueA, valueB);

        private void ValueAChanged(ValueType value)
        {
            Refresh();
            OnValueAChanged(value);
        }
        private void ValueBChanged(ValueType value)
        {
            Refresh();
            OnValueBChanged(value);
        }

        protected abstract void OnValueAChanged(ValueType value);
        protected abstract void OnValueBChanged(ValueType value);

        public void SetValues(ValueType valueA, ValueType valueB)
        {
            FieldA.Value = valueA;
            FieldB.Value = valueB;
            OnSetValue();
            Refresh();
        }
        protected abstract void OnSetValue();

        public override void SetStyle(ControlStyle style)
        {
            base.SetStyle(style);
            FieldA.TextFieldStyle = style.TextField;
            FieldB.TextFieldStyle = style.TextField;
        }
    }
}
