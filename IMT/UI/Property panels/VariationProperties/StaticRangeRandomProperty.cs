using IMT.Manager;
using IMT.Utilities;
using ModsCommon.UI;
using ModsCommon.Utilities;
using System;
using System.ComponentModel;
using UnityEngine;

namespace IMT.UI
{
    public abstract class StaticRangeRandomProperty<ValueType, FieldType, FieldRefType, RangeType, RangeRefType> : EditorPropertyPanel, IReusable
        where ValueType : IComparable<ValueType>
        where FieldType : ComparableUITextField<ValueType, FieldRefType>
        where FieldRefType : IFieldRef, IComparableField<ValueType>
        where RangeType : ValueFieldRange<ValueType, FieldType, FieldRefType, RangeRefType>
        where RangeRefType : IFieldRef, IValueFieldRange<ValueType, FieldRefType>
    {
        bool IReusable.InCache { get; set; }
        Transform IReusable.CachedTransform { get => m_CachedTransform; set => m_CachedTransform = value; }

        public event Action<StaticRangeRandomMode> OnModeChanged;
        public event Action<Spread> OnSpreadChanged;
        public event Action<ValueType, ValueType> OnValueChanged;

        protected StaticRangeRandomModeSegmented Mode { get; private set; }
        protected SpreadSegmented Spread { get; private set; }
        protected RangeType Range { get; private set; }

        public RangeRefType RangeRef => Range.Ref;

        protected override void FillContent()
        {
            Mode = Content.AddUIComponent<StaticRangeRandomModeSegmented>();
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

        private void ModeChanged(StaticRangeRandomMode mode)
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

        public void SetRandomValues() => SetValues(StaticRangeRandomMode.Random, default, default);
        public void SetValues(ValueType valueA, ValueType valueB) => SetValues(valueA.CompareTo(valueB) == 0 ? StaticRangeRandomMode.Static : StaticRangeRandomMode.Range, valueA, valueB);
        private void SetValues(StaticRangeRandomMode mode, ValueType valueA, ValueType valueB)
        {
            Mode.SelectedObject = mode;
            Range.Mode = mode switch
            {
                StaticRangeRandomMode.Random => RangeMode.Single,
                StaticRangeRandomMode.Static => RangeMode.Single,
                StaticRangeRandomMode.Range => RangeMode.Range,
                _ => default
            };
            Range.SetValues(valueA, valueB);
            Range.isEnabled = mode != StaticRangeRandomMode.Random;
            Spread.isVisible = mode == StaticRangeRandomMode.Range;
        }
        public void SetSpread(Spread spread) => Spread.SelectedObject = spread;

        public override void SetStyle(ControlStyle style)
        {
            Mode.SegmentedStyle = style.Segmented;
            Spread.SegmentedStyle = style.Segmented;
            Range.SetStyle(style);
        }
    }

    public class FloatStaticRangeRandomProperty : StaticRangeRandomProperty<float, FloatUITextField, FloatUITextField.FloatFieldRef, FloatRangeField, FloatRangeField.FloatRangeFieldRef> { }
}
