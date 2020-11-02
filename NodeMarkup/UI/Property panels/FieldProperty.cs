using ColossalFramework.UI;
using NodeMarkup.Tools;
using NodeMarkup.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NodeMarkup.UI.Editors
{
    public abstract class FieldPropertyPanel<ValueType, FieldType> : EditorPropertyPanel, IReusable
        where FieldType : UITextField<ValueType>
    {
        protected FieldType Field { get; set; }

        public event Action<ValueType> OnValueChanged;
        public event Action OnHover;
        public event Action OnLeave;

        public bool UseWheel
        {
            get => Field.UseWheel;
            set => Field.UseWheel = value;
        }
        public ValueType WheelStep
        {
            get => Field.WheelStep;
            set => Field.WheelStep = value;
        }
        public float FieldWidth
        {
            get => Field.width;
            set => Field.width = value;
        }

        public ValueType Value
        {
            get => Field;
            set => Field.Value = value;
        }

        public FieldPropertyPanel()
        {
            Field = AddTextField<ValueType, FieldType>(Control);

            Field.OnValueChanged += ValueChanged;
            Field.eventMouseHover += FieldHover;
            Field.eventMouseLeave += FieldLeave;
        }

        private void ValueChanged(ValueType value) => OnValueChanged?.Invoke(value);
        private void FieldHover(UIComponent component, UIMouseEventParameter eventParam) => OnHover?.Invoke();
        private void FieldLeave(UIComponent component, UIMouseEventParameter eventParam) => OnLeave?.Invoke();

        public override void DeInit()
        {
            base.DeInit();

            Field.UseWheel = false;
            Field.WheelStep = default;

            OnValueChanged = null;
            OnHover = null;
            OnLeave = null;
        }
        public void Edit() => Field.Focus();
        public override string ToString() => Value.ToString();

        public static implicit operator ValueType(FieldPropertyPanel<ValueType, FieldType> property) => property.Value;
    }
    public abstract class ComparableFieldPropertyPanel<ValueType, FieldType> : FieldPropertyPanel<ValueType, FieldType>
        where FieldType : ComparableUITextField<ValueType>
        where ValueType : IComparable<ValueType>
    {
        public ValueType MinValue
        {
            get => Field.MinValue;
            set => Field.MinValue = value;
        }
        public ValueType MaxValue
        {
            get => Field.MaxValue;
            set => Field.MaxValue = value;
        }
        public bool CheckMax
        {
            get => Field.CheckMax;
            set => Field.CheckMax = value;
        }
        public bool CheckMin
        {
            get => Field.CheckMin;
            set => Field.CheckMin = value;
        }

        public ComparableFieldPropertyPanel() => Field.SetDefault();
        public override void DeInit()
        {
            base.DeInit();
            Field.SetDefault();
        }
    }
    public class FloatPropertyPanel : ComparableFieldPropertyPanel<float, FloatUITextField> { }
    public class StringPropertyPanel : FieldPropertyPanel<string, StringUITextField> { }
    public class IntPropertyPanel : ComparableFieldPropertyPanel<int, IntUITextField> { }

}
