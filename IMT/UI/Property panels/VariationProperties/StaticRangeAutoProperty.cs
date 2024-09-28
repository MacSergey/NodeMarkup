using IMT.Manager;
using IMT.Utilities;
using ModsCommon.UI;
using ModsCommon.Utilities;
using System;
using System.ComponentModel;
using UnityEngine;

namespace IMT.UI
{
    public abstract class StaticRangeAutoProperty<ValueType, FieldType, FieldRefType, RangeType, RangeRefType> : EditorPropertyPanel, IReusable
        where ValueType : IComparable<ValueType>
        where FieldType : ComparableUITextField<ValueType, FieldRefType>
        where FieldRefType : IFieldRef, IComparableField<ValueType>
        where RangeType : ValueFieldRange<ValueType, FieldType, FieldRefType, RangeRefType>
        where RangeRefType : IFieldRef, IValueFieldRange<ValueType, FieldRefType>
    {
        bool IReusable.InCache { get; set; }
        Transform IReusable.CachedTransform { get => m_CachedTransform; set => m_CachedTransform = value; }

        public event Action<StaticRangeAutoMode> OnModeChanged;
        public event Action<Spread> OnSpreadChanged;
        public event Action<ValueType, ValueType> OnValueChanged;

        protected StaticRangeAutoModeSegmented Mode { get; private set; }
        protected SpreadSegmented Spread { get; private set; }
        protected RangeType Range { get; private set; }

        public RangeRefType RangeRef => Range.Ref;

        protected override void FillContent()
        {
            Mode = Content.AddUIComponent<StaticRangeAutoModeSegmented>();
            Mode.name = nameof(Mode);
            Mode.Init();
            Mode.SetDefaultStyle();
            Mode.OnSelectObject += ModeChanged;

            Spread = Content.AddUIComponent<SpreadSegmented>();
            Spread.name = nameof(Spread);
            Spread.Init();
            Spread.SetDefaultStyle();
            Spread.OnSelectObject += SpreadChanged;

            Range = Content.AddUIComponent<RangeType>();
            Range.SetDefaultStyle();
            Range.name = nameof(Range);
            Range.OnValueChanged += ValueChanged;
        }

        public override void Init()
        {
            Mode.AutoButtonSize = false;
            Mode.ButtonWidth = 30f;
            Mode.SetDefaultStyle();

            Spread.AutoButtonSize = false;
            Spread.ButtonWidth = 30f;
            Spread.SetDefaultStyle();

            base.Init();
        }

        public override void DeInit()
        {
            base.DeInit();

            OnModeChanged = null;
            OnSpreadChanged = null;
            OnValueChanged = null;

            Mode.SetDefault();
            Spread.SetDefault();
            Range.SetDefault();
        }

        private void ModeChanged(StaticRangeAutoMode mode)
        {
            SetValues(mode, Range.ValueA, Range.ValueB);
            ValueChanged(Range.ValueA, Range.ValueB);

            OnModeChanged?.Invoke(mode);
        }
        private void SpreadChanged(Spread spread)
        {
            OnSpreadChanged?.Invoke(spread);
        }
        private void ValueChanged(ValueType valueA, ValueType valueB)
        {
            OnValueChanged?.Invoke(valueA, valueB);
        }

        public void SetAutoValues() => SetValues(StaticRangeAutoMode.Auto, default, default);
        public void SetValues(ValueType valueA, ValueType valueB) => SetValues(valueA.CompareTo(valueB) == 0 ? StaticRangeAutoMode.Static : StaticRangeAutoMode.Range, valueA, valueB);
        private void SetValues(StaticRangeAutoMode mode, ValueType valueA, ValueType valueB)
        {
            Mode.SelectedObject = mode;
            Range.Mode = mode switch
            {
                StaticRangeAutoMode.Auto => RangeMode.Single,
                StaticRangeAutoMode.Static => RangeMode.Single,
                StaticRangeAutoMode.Range => RangeMode.Range,
                _ => default
            };
            Range.SetValues(valueA, valueB);
            Range.isEnabled = mode != StaticRangeAutoMode.Auto;
            Spread.isVisible = mode == StaticRangeAutoMode.Range;
        }
        public void SetSpread(Spread spread) => Spread.SelectedObject = spread;

        public override void SetStyle(ControlStyle style)
        {
            Mode.SegmentedStyle = style.Segmented;
            Spread.SegmentedStyle = style.Segmented;
            Range.SetStyle(style);
        }
    }

    public class FloatStaticRangeAutoProperty : StaticRangeAutoProperty<float, FloatUITextField, FloatUITextField.FloatFieldRef, FloatRangeField, FloatRangeField.FloatRangeFieldRef> { }
}
