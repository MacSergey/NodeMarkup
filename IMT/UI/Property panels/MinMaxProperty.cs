using ColossalFramework.UI;
using ModsCommon.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace IMT.UI
{
    public class MinMaxProperty : EditorPropertyPanel, IReusable
    {
        bool IReusable.InCache { get; set; }

        public event Action<bool, int, int> OnValueChanged;

        private BoolSegmented UseCount { get; }
        private IntUITextField MinField { get; }
        private IntUITextField MaxField { get; }

        public bool EnableCount
        {
            get => UseCount.SelectedObject;
            set
            {
                UseCount.SelectedObject = value;
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

        public MinMaxProperty()
        {
            UseCount = Content.AddUIComponent<BoolSegmented>();
            UseCount.StopLayout();
            UseCount.AutoButtonSize = false;
            UseCount.ButtonWidth = 25f;
            UseCount.AddItem(true, "I");
            UseCount.AddItem(false, "O");
            UseCount.StartLayout();
            UseCount.OnSelectObjectChanged += UseChanged;

            var min = Content.AddUIComponent<CustomUILabel>();
            min.text = IMT.Localize.StyleOption_Min;
            min.textScale = 0.7f;
            min.padding = new RectOffset(0, 0, 2, 0);

            MinField = Content.AddUIComponent<IntUITextField>();
            MinField.SetDefaultStyle();
            MinField.width = 50f;
            MinField.name = nameof(MinField);
            MinField.OnValueChanged += MinChanged;

            var max = Content.AddUIComponent<CustomUILabel>();
            max.text = IMT.Localize.StyleOption_Max;
            max.textScale = 0.7f;
            max.padding = new RectOffset(0, 0, 2, 0);

            MaxField = Content.AddUIComponent<IntUITextField>();
            MaxField.SetDefaultStyle();
            MaxField.width = 50f;
            MaxField.name = nameof(MaxField);
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
    }
}
