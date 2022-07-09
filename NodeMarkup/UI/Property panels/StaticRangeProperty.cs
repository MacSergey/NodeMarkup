using ModsCommon;
using ModsCommon.UI;
using NodeMarkup.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NodeMarkup.UI
{
    public abstract class StaticRangeProperty<ValueType, FieldType> : EditorPropertyPanel, IReusable
        where FieldType : ComparableUITextField<ValueType>
        where ValueType : IComparable<ValueType>
    {
        bool IReusable.InCache { get; set; }

        protected BoolSegmented Selector { get; set; }
        protected FieldType FieldA { get; set; }
        protected FieldType FieldB { get; set; }

        public event Action<ValueType, ValueType> OnValueChanged;

        private const float defaultFieldWidth = 100f;
        private float _fieldWidth = defaultFieldWidth;
        public float FieldWidth
        {
            get => _fieldWidth;
            set
            {
                if (value != _fieldWidth)
                {
                    _fieldWidth = value;
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

        private ValueType _minValue;
        public ValueType MinValue
        {
            get => _minValue;
            set
            {
                if (value.CompareTo(_minValue) != 0)
                {
                    _minValue = value;
                    Refresh();
                }
            }
        }

        private ValueType _maxValue;
        public ValueType MaxValue
        {
            get => _maxValue;
            set
            {
                if (value.CompareTo(_maxValue) != 0)
                {
                    _maxValue = value;
                    Refresh();
                }
            }
        }

        private bool _checkMin;
        public bool CheckMin
        {
            get => _checkMin;
            set
            {
                if (value != _checkMin)
                {
                    _checkMin = value;
                    Refresh();
                }
            }
        }

        private bool _checkMax;
        public bool CheckMax
        {
            get => _checkMax;
            set
            {
                if (value != _checkMax)
                {
                    _checkMax = value;
                    Refresh();
                }
            }
        }

        private bool _cyclicalValue;
        public bool CyclicalValue
        {
            get => _cyclicalValue;
            set
            {
                if (value != _cyclicalValue)
                {
                    _cyclicalValue = value;
                    Refresh();
                }
            }
        }

        private bool _allowInvert;
        public bool AllowInvert
        {
            get => _allowInvert;
            set
            {
                if (value != _allowInvert)
                {
                    _allowInvert = value;
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

        public StaticRangeProperty()
        {
            Selector = Content.AddUIComponent<BoolSegmented>();
            Selector.SetDefaultStyle();
            Selector.name = nameof(Selector);

            FieldA = Content.AddUIComponent<FieldType>();
            FieldA.SetDefaultStyle();
            FieldA.name = nameof(FieldA);

            FieldB = Content.AddUIComponent<FieldType>();
            FieldB.SetDefaultStyle();
            FieldB.name = nameof(FieldB);

            FieldA.OnValueChanged += ValueAChanged;
            FieldB.OnValueChanged += ValueBChanged;
        }

        public override void Init()
        {
            Selector.AutoButtonSize = false;
            Selector.ButtonWidth = 30f;
            Selector.SetDefaultStyle();
            Selector.StopLayout();
            Selector.AddItem(false, label: NodeMarkup.Localize.StyleOption_ObjectStatic, iconAtlas: NodeMarkupTextures.Atlas, iconSprite: NodeMarkupTextures.SingleButtonIcons);
            Selector.AddItem(true, label: NodeMarkup.Localize.StyleOption_ObjectRange, iconAtlas: NodeMarkupTextures.Atlas, iconSprite: NodeMarkupTextures.RangeButtonIcons);
            Selector.StartLayout();
            Selector.OnSelectObjectChanged += SelectorChanged;

            base.Init();
        }

        public override void DeInit()
        {
            base.DeInit();

            OnValueChanged = null;

            _fieldWidth = defaultFieldWidth;
            _checkMin = false;
            _checkMax = false;
            _minValue = default;
            _maxValue = default;
            _allowInvert = false;

            UseWheel = false;
            WheelStep = default;
            WheelTip = false;
            SubmitOnFocusLost = true;
            Format = null;

            FieldA.SetDefault();
            FieldB.SetDefault();
            Selector.DeInit();
        }

        public void SetValues(ValueType valueA, ValueType valueB)
        {
            FieldA.Value = valueA;
            FieldB.Value = valueB;
            Selector.SelectedObject = valueA.CompareTo(valueB) != 0;
            Refresh();
        }

        private void SelectorChanged(bool value)
        {
            Refresh();
            if(Selector.SelectedObject)
                OnValueChanged?.Invoke(FieldA.Value, FieldB.Value);
            else
                OnValueChanged?.Invoke(FieldA.Value, FieldA.Value);
        }
        private void ValueAChanged(ValueType value)
        {
            Refresh();
            if (Selector.SelectedObject)
                OnValueChanged?.Invoke(value, FieldB.Value);
            else
                OnValueChanged?.Invoke(value, value);
        }
        private void ValueBChanged(ValueType value)
        {
            Refresh();
            if (Selector.SelectedObject)
                OnValueChanged?.Invoke(FieldA.Value, value);
            else
                OnValueChanged?.Invoke(FieldA.Value, FieldA.Value);
        }

        private void Refresh()
        {
            if (Selector.SelectedObject)
            {
                FieldB.isVisible = true;
                FieldA.width = (FieldWidth - Content.autoLayoutPadding.horizontal) * 0.5f;
                FieldB.width = (FieldWidth - Content.autoLayoutPadding.horizontal) * 0.5f;

                if (AllowInvert)
                {
                    FieldA.CheckMin = CheckMin;
                    FieldA.CheckMax = CheckMax;
                    FieldA.MinValue = MinValue;
                    FieldA.MaxValue = MaxValue;
                    FieldA.CyclicalValue = CyclicalValue;

                    FieldB.CheckMin = CheckMin;
                    FieldB.CheckMax = CheckMax;
                    FieldB.MinValue = MinValue;
                    FieldB.MaxValue = MaxValue;
                    FieldB.CyclicalValue = CyclicalValue;
                }
                else
                {
                    FieldA.CheckMin = CheckMin;
                    FieldA.CheckMax = true;
                    FieldA.MinValue = MinValue;
                    FieldA.MaxValue = FieldB.Value;
                    FieldA.CyclicalValue = false;

                    FieldB.CheckMin = true;
                    FieldB.CheckMax = CheckMax;
                    FieldB.MinValue = FieldA.Value;
                    FieldB.MaxValue = MaxValue;
                    FieldB.CyclicalValue = false;
                }
            }
            else
            {
                FieldB.isVisible = false;
                FieldA.width = FieldWidth;

                FieldA.CheckMin = CheckMin;
                FieldA.CheckMax = CheckMax;
                FieldA.MinValue = MinValue;
                FieldA.MaxValue = MaxValue;
                FieldA.CyclicalValue = CyclicalValue;
            }

            Content.Refresh();
        }
    }

    public class FloatStaticRangeProperty : StaticRangeProperty<float, FloatUITextField> {}
}
