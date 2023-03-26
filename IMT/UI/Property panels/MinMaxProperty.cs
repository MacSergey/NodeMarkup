using ModsCommon.UI;
using System;
using UnityEngine;

namespace IMT.UI
{
    public class MinMaxProperty : EditorPropertyPanel, IReusable
    {
        public event Action<bool, int, int> OnValueChanged;

        private CustomUIToggle UseCount { get; set; }
        private IntUITextField MinField { get; set; }
        private IntUITextField MaxField { get; set; }

        public bool EnableCount
        {
            get => UseCount.Value;
            set
            {
                UseCount.Value = value;
                Refresh();
            }
        }

        public int MinValue
        {
            get => MinField;
            set
            {
                MinField.Value = value;
                SetRange(MinRange, MaxRange);
            }
        }
        public int MaxValue
        {
            get => MaxField;
            set
            {
                MaxField.Value = value;
                SetRange(MinRange, MaxRange);
            }
        }

        public int MinRange
        {
            get => MinField.MinValue;
            set => SetRange(value, MaxRange);
        }
        public int MaxRange
        {
            get => MaxField.MaxValue;
            set => SetRange(MinRange, value);
        }

        public bool UseWheel
        {
            set
            {
                MinField.UseWheel = value;
                MaxField.UseWheel = value;
            }
        }
        public bool WheelTip
        {
            set
            {
                MinField.WheelTip = value;
                MaxField.WheelTip = value;
            }
        }
        public int WheelStep
        {
            set
            {
                MinField.WheelStep = value;
                MaxField.WheelStep = value;
            }
        }

        protected override void FillContent()
        {
            UseCount = Content.AddUIComponent<CustomUIToggle>();
            UseCount.name = nameof(UseCount);
            UseCount.DefaultStyle();
            UseCount.OnValueChanged += UseChanged;

            var min = Content.AddUIComponent<CustomUILabel>();
            min.text = IMT.Localize.StyleOption_Min;
            min.textScale = 0.7f;
            min.Padding = new RectOffset(0, 0, 2, 0);

            MinField = Content.AddUIComponent<IntUITextField>();
            MinField.name = nameof(MinField);
            MinField.SetDefaultStyle();
            MinField.width = 50f;
            MinField.OnValueChanged += MinChanged;

            var max = Content.AddUIComponent<CustomUILabel>();
            max.text = IMT.Localize.StyleOption_Max;
            max.textScale = 0.7f;
            max.Padding = new RectOffset(0, 0, 2, 0);

            MaxField = Content.AddUIComponent<IntUITextField>();
            MaxField.name = nameof(MaxField);
            MaxField.SetDefaultStyle();
            MaxField.width = 50f;
            MaxField.OnValueChanged += MaxChanged;

            SetDefault();
        }
        public override void DeInit()
        {
            base.DeInit();

            OnValueChanged = null;
            SetDefault();
        }
        private void SetDefault()
        {
            MinField.SetDefault();
            MaxField.SetDefault();

            MinField.CheckMin = true;
            MinField.CheckMax = true;
            MaxField.CheckMin = true;
            MaxField.CheckMax = true;
        }

        private void UseChanged(bool value)
        {
            Refresh();
            OnValueChanged?.Invoke(value, MinValue, MaxValue);
        }
        private void MinChanged(int value)
        {
            SetRange(MinRange, MaxRange);
            OnValueChanged?.Invoke(EnableCount, value, MaxValue);
        }
        private void MaxChanged(int value)
        {
            SetRange(MinRange, MaxRange);
            OnValueChanged?.Invoke(EnableCount, MinValue, value);
        }

        private void SetRange(int minRange, int maxRange)
        {
            var min = Math.Min(minRange, maxRange);
            var max = Math.Max(maxRange, minRange);

            MinField.MinValue = min;
            MinField.MaxValue = MaxValue;

            MaxField.MinValue = MinValue;
            MaxField.MaxValue = max;
        }
        private void Refresh()
        {
            MinField.isEnabled = EnableCount;
            MaxField.isEnabled = EnableCount;
        }

        public override void SetStyle(ControlStyle style)
        {
            UseCount.ToggleStyle = style.Toggle;
            MinField.TextFieldStyle = style.TextField;
            MaxField.TextFieldStyle = style.TextField;
        }
    }
}
