﻿using ModsCommon.UI;
using System;
using UnityEngine;

namespace IMT.UI
{
    public class GapProperty : EditorPropertyPanel, IReusable
    {
        bool IReusable.InCache { get; set; }
        Transform IReusable.CachedTransform { get => m_CachedTransform; set => m_CachedTransform = value; }


        public event Action<bool, float, int> OnValueChanged;

        private CustomUIToggle UseToggle { get; set; }
        private FloatUITextField LengthField { get; set; }
        private IntUITextField PeriodField { get; set; }

        public bool EnableGap
        {
            get => UseToggle.Value;
            set
            {
                UseToggle.Value = value;
                Refresh();
            }
        }
        public float Length
        {
            get => LengthField;
            set => LengthField.Value = value;
        }
        public int Period
        {
            get => PeriodField;
            set => PeriodField.Value = value;
        }

        public bool CheckMinLength
        {
            get => LengthField.CheckMin;
            set => LengthField.CheckMin = value;
        }
        public bool CheckMinPeriod
        {
            get => PeriodField.CheckMin;
            set => PeriodField.CheckMin = value;
        }
        public float MinLength
        {
            get => LengthField.MinValue;
            set => LengthField.MinValue = value;
        }
        public int MinPeriod
        {
            get => PeriodField.MinValue;
            set => PeriodField.MinValue = value;
        }
        public bool UseWheel
        {
            set
            {
                LengthField.UseWheel = value;
                PeriodField.UseWheel = value;
            }
        }
        public bool WheelTip
        {
            set
            {
                LengthField.WheelTip = value;
                PeriodField.WheelTip = value;
            }
        }

        public float WheelStepLength
        {
            get => LengthField.WheelStep;
            set => LengthField.WheelStep = value;
        }
        public int WheelStepPeriod
        {
            get => PeriodField.WheelStep;
            set => PeriodField.WheelStep = value;
        }

        protected override void FillContent()
        {
            UseToggle = Content.AddUIComponent<CustomUIToggle>();
            UseToggle.name = nameof(UseToggle);
            UseToggle.DefaultStyle();
            UseToggle.OnValueChanged += UseChanged;

            LengthField = Content.AddUIComponent<FloatUITextField>();
            LengthField.name = nameof(LengthField);
            LengthField.SetDefaultStyle();
            LengthField.width = 50f;
            LengthField.Format = IMT.Localize.NumberFormat_Meter;
            LengthField.OnValueChanged += LengthChanged;

            PeriodField = Content.AddUIComponent<IntUITextField>();
            PeriodField.name = nameof(PeriodField);
            PeriodField.SetDefaultStyle();
            PeriodField.width = 80f;
            PeriodField.Format = IMT.Localize.NumberFormat_Period;
            PeriodField.OnValueChanged += PeriodChanged;
        }
        public override void DeInit()
        {
            base.DeInit();

            OnValueChanged = null;

            UseWheel = false;
            CheckMinLength = false;
            CheckMinPeriod = false;
            MinLength = default;
            MinPeriod = default;
            WheelStepLength = default;
            WheelStepPeriod = default;
            WheelTip = false;

            LengthField.SetDefault();
            PeriodField.SetDefault();
        }

        private void UseChanged(bool value)
        {
            Refresh();
            OnValueChanged?.Invoke(value, Length, Period);
        }
        private void LengthChanged(float value)
        {
            OnValueChanged?.Invoke(EnableGap, value, Period);
        }
        private void PeriodChanged(int value)
        {
            OnValueChanged?.Invoke(EnableGap, Length, value);
        }

        private void Refresh()
        {
            LengthField.isEnabled = EnableGap;
            PeriodField.isEnabled = EnableGap;
        }

        public override void SetStyle(ControlStyle style)
        {
            UseToggle.ToggleStyle = style.Toggle;
            LengthField.TextFieldStyle = style.TextField;
            PeriodField.TextFieldStyle = style.TextField;
        }
    }
}
