using ModsCommon.UI;
using System;

namespace IMT.UI
{
    public class GapProperty : EditorPropertyPanel, IReusable
    {
        bool IReusable.InCache { get; set; }

        public event Action<bool, float, int> OnValueChanged;

        private CustomUIToggle UseToggle { get; }
        private FloatUITextField LengthField { get; }
        private IntUITextField PeriodField { get; }

        public bool EnableGap
        {
            get => UseToggle.State;
            set
            {
                UseToggle.State = value;
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
            UseToggle = Content.AddUIComponent<CustomUIToggle>();
            UseToggle.CustomStyle();
            UseToggle.OnStateChanged += UseChanged;

            LengthField = Content.AddUIComponent<FloatUITextField>();
            LengthField.SetDefaultStyle();
            LengthField.width = 50f;
            LengthField.name = nameof(LengthField);
            LengthField.Format = IMT.Localize.NumberFormat_Meter;
            LengthField.OnValueChanged += LengthChanged;

            PeriodField = Content.AddUIComponent<IntUITextField>();
            PeriodField.SetDefaultStyle();
            PeriodField.width = 80f;
            PeriodField.name = nameof(PeriodField);
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
    }
}
