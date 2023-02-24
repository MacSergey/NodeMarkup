using ColossalFramework.UI;
using ModsCommon.UI;
using ModsCommon.Utilities;
using System;

namespace IMT.UI
{
    public abstract class SingleDoubleInvertedProperty<ValueType, FieldType> : SingleDoubleProperty<ValueType, FieldType>
            where FieldType : ComparableUITextField<ValueType>
            where ValueType : IComparable<ValueType>
    {
        protected MultyAtlasUIButton Invert { get; }

        public SingleDoubleInvertedProperty()
        {
            Invert = Content.AddUIComponent<MultyAtlasUIButton>();
            Invert.SetDefaultStyle();
            Invert.width = 20;
            Invert.atlasForeground = CommonTextures.Atlas;
            Invert.normalFgSprite = CommonTextures.PlusMinusButton;
            Invert.eventClick += InvertClick;
        }

        private void InvertClick(UIComponent component, UIMouseEventParameter eventParam)
        {
            FieldA.Value = InvertValue(FieldA.Value);
            FieldB.Value = InvertValue(FieldB.Value);

            Refresh();
            if (SelectedObject == FirstOptionIndex)
                ValueChanged(FieldA.Value, FieldA.Value);
            else if (SelectedObject == SecondOptionIndex)
                ValueChanged(FieldA.Value, FieldB.Value);
        }
        protected abstract ValueType InvertValue(ValueType value);

        protected override void OnSizeChanged()
        {
            base.OnSizeChanged();
            SetSize();
        }
        protected virtual void SetSize()
        {
            if (Invert != null)
                Invert.height = Content.height - ItemsPadding * 2;
        }
    }
    public class FloatSingleDoubleInvertedProperty : SingleDoubleInvertedProperty<float, FloatUITextField>
    {
        protected override float InvertValue(float value) => -value;
    }
}
