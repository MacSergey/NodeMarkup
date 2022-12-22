using ModsCommon;
using ModsCommon.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NodeMarkup.UI
{
    public class GapProperty : EditorPropertyPanel, IReusable
    {
        bool IReusable.InCache { get; set; }

        public event Action<bool, float, int> OnValueChanged;

        private BoolSegmented UseSegmented { get; }
        private FloatUITextField LengthField { get; }
        private IntUITextField PeriodField { get; }

        public bool Use
        {
            get => UseSegmented;
            set
            {
                UseSegmented.SelectedObject = value;
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

        public GapProperty()
        {
            UseSegmented = Content.AddUIComponent<BoolSegmented>();
            UseSegmented.StopLayout();
            UseSegmented.AutoButtonSize = false;
            UseSegmented.ButtonWidth = 25f;
            UseSegmented.AddItem(true, "I");
            UseSegmented.AddItem(false, "O");
            UseSegmented.StartLayout();
            UseSegmented.OnSelectObjectChanged += UseChanged;

            LengthField = Content.AddUIComponent<FloatUITextField>();
            LengthField.SetDefaultStyle();
            LengthField.width = 70f;
            LengthField.name = nameof(LengthField);
            LengthField.Format = NodeMarkup.Localize.NumberFormat_Meter;
            LengthField.OnValueChanged += LengthChanged;

            PeriodField = Content.AddUIComponent<IntUITextField>();
            PeriodField.SetDefaultStyle();
            PeriodField.width = 80f;
            PeriodField.name = nameof(PeriodField);
            PeriodField.Format = NodeMarkup.Localize.NumberFormat_Period;
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
            OnValueChanged?.Invoke(value, LengthField.Value, PeriodField.Value);
        }
        private void LengthChanged(float value)
        {
            OnValueChanged?.Invoke(UseSegmented.SelectedObject, value, PeriodField.Value);
        }
        private void PeriodChanged(int value)
        {
            OnValueChanged?.Invoke(UseSegmented.SelectedObject, LengthField.Value, value);
        }

        private void Refresh()
        {
            LengthField.isEnabled = UseSegmented.SelectedObject;
            PeriodField.isEnabled = UseSegmented.SelectedObject;
        }
    }
}
