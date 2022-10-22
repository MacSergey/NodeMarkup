using ModsCommon;
using ModsCommon.UI;
using NodeMarkup.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace NodeMarkup.UI
{
    public abstract class StaticRangeProperty<ValueType, FieldType> : VariationProperty<int, IntSegmented>
        where FieldType : ComparableUITextField<ValueType>
        where ValueType : IComparable<ValueType>
    {
        protected FieldType FieldA { get; set; }
        protected FieldType FieldB { get; set; }

        protected virtual int StaticIndex => 0;
        protected virtual int RangeIndex => 1;

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
            FieldA = Content.AddUIComponent<FieldType>();
            FieldA.SetDefaultStyle();
            FieldA.name = nameof(FieldA);

            FieldB = Content.AddUIComponent<FieldType>();
            FieldB.SetDefaultStyle();
            FieldB.name = nameof(FieldB);

            FieldA.OnValueChanged += ValueAChanged;
            FieldB.OnValueChanged += ValueBChanged;
        }

        protected override void AddSelectorItems()
        {
            AddItem(StaticIndex, NodeMarkup.Localize.StyleOption_ObjectStatic, NodeMarkupTextures.Atlas, NodeMarkupTextures.SingleButtonIcons);
            AddItem(RangeIndex, NodeMarkup.Localize.StyleOption_ObjectRange, NodeMarkupTextures.Atlas, NodeMarkupTextures.RangeButtonIcons);
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
        }

        public void SetValues(ValueType valueA, ValueType valueB)
        {
            FieldA.Value = valueA;
            FieldB.Value = valueB;
            SelectedObject = valueA.CompareTo(valueB) == 0 ? StaticIndex : RangeIndex;
            Refresh();
        }

        protected override void SelectorChangedImpl(int index)
        {
            if (index == StaticIndex)
                OnValueChanged?.Invoke(FieldA.Value, FieldA.Value);
            else if (index == RangeIndex)
                OnValueChanged?.Invoke(FieldA.Value, FieldB.Value);
        }

        private void ValueAChanged(ValueType value)
        {
            Refresh();
            if (SelectedObject == StaticIndex)
                OnValueChanged?.Invoke(value, value);
            else if (SelectedObject == RangeIndex)
                OnValueChanged?.Invoke(value, FieldB.Value);
        }
        private void ValueBChanged(ValueType value)
        {
            Refresh();
            if (SelectedObject == StaticIndex)
                OnValueChanged?.Invoke(FieldA.Value, FieldA.Value);
            else if (SelectedObject == RangeIndex)
                OnValueChanged?.Invoke(FieldA.Value, value);
        }

        protected override void RefreshImpl()
        {
            if (SelectedObject == StaticIndex)
            {
                FieldB.isVisible = false;
                FieldA.width = FieldWidth;

                FieldA.CheckMin = CheckMin;
                FieldA.CheckMax = CheckMax;
                FieldA.MinValue = MinValue;
                FieldA.MaxValue = MaxValue;
                FieldA.CyclicalValue = CyclicalValue;
                FieldA.Value = FieldA.Value;
            }
            else if (SelectedObject == RangeIndex)
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
                    FieldA.Value = FieldA.Value;

                    FieldB.CheckMin = CheckMin;
                    FieldB.CheckMax = CheckMax;
                    FieldB.MinValue = MinValue;
                    FieldB.MaxValue = MaxValue;
                    FieldB.CyclicalValue = CyclicalValue;
                    FieldB.Value = FieldB.Value;
                }
                else
                {
                    FieldB.CheckMin = true;
                    FieldB.CheckMax = CheckMax;
                    FieldB.MinValue = FieldA.Value;
                    FieldB.MaxValue = MaxValue;
                    FieldB.CyclicalValue = false;
                    FieldB.Value = FieldB.Value;

                    FieldA.CheckMin = CheckMin;
                    FieldA.CheckMax = true;
                    FieldA.MinValue = MinValue;
                    FieldA.MaxValue = FieldB.Value;
                    FieldA.CyclicalValue = false;
                    FieldA.Value = FieldA.Value;
                }
            }
        }
    }

    public abstract class StaticRangeAutoProperty<ValueType, FieldType> : StaticRangeProperty<ValueType, FieldType>
        where FieldType : ComparableUITextField<ValueType>
        where ValueType : IComparable<ValueType>
    {
        public event Action OnAutoValue;

        protected virtual int AutoIndex => 0;
        protected override int StaticIndex => 1;
        protected override int RangeIndex => 2;

        protected override void AddSelectorItems()
        {
            AddItem(AutoIndex, NodeMarkup.Localize.StyleOption_ObjectAuto, NodeMarkupTextures.Atlas, NodeMarkupTextures.AutoButtonIcons);
            base.AddSelectorItems();
        }

        public void SetAuto()
        {
            FieldA.Value = default;
            FieldB.Value = default;
            SelectedObject = AutoIndex;
            Refresh();
        }
        protected override void SelectorChangedImpl(int index)
        {
            if (index == AutoIndex)
                OnAutoValue?.Invoke();
            else 
                base.SelectorChangedImpl(index);
        }

        protected override void RefreshImpl()
        {
            if(SelectedObject == AutoIndex)
            {
                FieldB.isVisible = false;
                FieldA.isEnabled = false;
                FieldA.width = FieldWidth;
                FieldA.Value = FieldA.Value;
            }
            else
            {
                FieldA.isEnabled = true;
                base.RefreshImpl();
            }
        }
    }

    public class FloatStaticRangeProperty : StaticRangeProperty<float, FloatUITextField> { }
    public class FloatStaticRangeAutoProperty : StaticRangeAutoProperty<float, FloatUITextField> { }
}
