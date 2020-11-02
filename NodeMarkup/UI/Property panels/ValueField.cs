using ColossalFramework.UI;
using NodeMarkup.Tools;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace NodeMarkup.UI
{
    public abstract class UITextField<ValueType> : UITextField
    {
        public event Action<ValueType> OnValueChanged;
        protected abstract bool CanUseWheel { get; }
        public bool UseWheel { get; set; }
        public ValueType WheelStep { get; set; }

        private bool ValueProgress { get; set; } = false;
        public virtual ValueType Value
        {
            get
            {
                try
                {
                    return (ValueType)TypeDescriptor.GetConverter(typeof(ValueType)).ConvertFromString(text);
                }
                catch
                {
                    return default;
                }
            }
            set
            {
                if (!ValueProgress)
                {
                    ValueProgress = true;
                    text = GetString(value);
                    OnValueChanged?.Invoke(value);
                    ValueProgress = false;
                }
            }
        }

        public UITextField()
        {
            tooltip = Settings.ShowToolTip && CanUseWheel ? NodeMarkup.Localize.FieldPanel_ScrollWheel : string.Empty;
        }

        protected virtual string GetString(ValueType value) => value.ToString();

        protected abstract ValueType Increment(ValueType value, ValueType step, WheelMode mode);
        protected abstract ValueType Decrement(ValueType value, ValueType step, WheelMode mode);

        protected override void OnSubmit()
        {
            base.OnSubmit();
            Value = Value;
        }
        protected override void OnMouseWheel(UIMouseEventParameter p)
        {
            base.OnMouseWheel(p);

            if (CanUseWheel && UseWheel)
            {
                var mode = NodeMarkupTool.ShiftIsPressed ? WheelMode.High : NodeMarkupTool.CtrlIsPressed ? WheelMode.Low : WheelMode.Normal;
                if (p.wheelDelta < 0)
                    Value = Decrement(Value, WheelStep, mode);
                else
                    Value = Increment(Value, WheelStep, mode);
            }
        }

        public override string ToString() => Value.ToString();
        public static implicit operator ValueType(UITextField<ValueType> field) => field.Value;

        protected enum WheelMode
        {
            Normal,
            Low,
            High
        }
    }
    public abstract class ComparableUITextField<ValueType> : UITextField<ValueType>
        where ValueType : IComparable<ValueType>
    {
        public ValueType MinValue { get; set; }
        public ValueType MaxValue { get; set; }
        public bool CheckMax { get; set; }
        public bool CheckMin { get; set; }

        public override ValueType Value
        {
            get => base.Value;
            set
            {
                var newValue = value;

                if (CheckMin && newValue.CompareTo(MinValue) < 0)
                    newValue = MinValue;

                if (CheckMax && newValue.CompareTo(MaxValue) > 0)
                    newValue = MaxValue;

                base.Value = newValue;
            }
        }

        public ComparableUITextField() => SetDefault();

        public void SetDefault()
        {
            MinValue = default;
            MaxValue = default;
            CheckMin = false;
            CheckMax = false;
        }
    }
    public class FloatUITextField : ComparableUITextField<float>
    {
        protected override bool CanUseWheel => true;
        protected override float Decrement(float value, float step, WheelMode mode)
        {
            step = GetStep(step, mode);
            return (value - step).RoundToNearest(step);
        }
        protected override float Increment(float value, float step, WheelMode mode)
        {
            step = GetStep(step, mode);
            return (value + step).RoundToNearest(step);
        }
        float GetStep(float step, WheelMode mode) => mode switch
        {
            WheelMode.Low => step / 10,
            WheelMode.High => step * 10,
            _ => step,
        };

        protected override string GetString(float value) => value.ToString("0.###");
    }
    public class StringUITextField : ComparableUITextField<string>
    {
        protected override bool CanUseWheel => false;

        protected override string Decrement(string value, string step, WheelMode mode) => throw new NotSupportedException();
        protected override string Increment(string value, string step, WheelMode mode) => throw new NotSupportedException();
    }
    public class IntUITextField : ComparableUITextField<int>
    {
        protected override bool CanUseWheel => true;

        protected override int Decrement(int value, int step, WheelMode mode) => value == int.MinValue ? value : value - GetStep(step, mode);
        protected override int Increment(int value, int step, WheelMode mode) => value == int.MaxValue ? value : value - GetStep(step, mode);
        int GetStep(int step, WheelMode mode) => mode switch
        {
            WheelMode.Low => Math.Max(step / 10, 1),
            WheelMode.High => step * 10,
            _ => step,
        };
    }
    public class ByteUITextField : ComparableUITextField<byte>
    {
        protected override bool CanUseWheel => true;

        protected override byte Decrement(byte value, byte step, WheelMode mode)
        {
            step = GetStep(step, mode);
            return value < step ? byte.MinValue : (byte)(value - step);
        }
        protected override byte Increment(byte value, byte step, WheelMode mode)
        {
            step = GetStep(step, mode);
            return byte.MaxValue - value < step ? byte.MaxValue : (byte)(value + step);
        }

        byte GetStep(byte step, WheelMode mode) => mode switch
        {
            WheelMode.Low => (byte)Math.Max(step / 10, 1),
            WheelMode.High => (byte)Math.Min(step * 10, byte.MaxValue),
            _ => step,
        };
    }
}
