using ModsCommon.UI;
using NodeMarkup.Manager;
using NodeMarkup.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NodeMarkup.UI.Editors
{
    public class EnterEditor : Editor<EnterItem, Enter, ColorIcon>
    {
        protected override bool UseGroupPanel => true;

        public override string Name => NodeMarkup.Localize.PointEditor_Points;
        public override string EmptyMessage => string.Empty;
        public override Type SupportType { get; } = typeof(ISupportEnters);

        private MarkupPoint HoverPoint { get; set; }
        private bool IsHoverPoint => HoverPoint != null;

        private List<FloatPropertyPanel> Points { get; } = new List<FloatPropertyPanel>();
        protected override void FillItems()
        {
            foreach (var enter in Markup.Enters)
                    AddItem(enter);
        }
        protected override void OnObjectSelect()
        {
            foreach (var point in EditObject.Points)
                Points.Add(AddPointProperty(point));

            SetEven();
        }
        private FloatPropertyPanel AddPointProperty(MarkupPoint point)
        {
            var pointProperty = ComponentPool.Get<FloatPropertyPanel>(PropertiesPanel);
            pointProperty.Text = $"Point #{point.Num} offset" /*NodeMarkup.Localize.PointEditor_Offset*/;
            pointProperty.UseWheel = true;
            pointProperty.WheelStep = 0.1f;
            pointProperty.WheelTip = WheelTip;
            pointProperty.Init();
            pointProperty.Value = point.Offset;
            pointProperty.OnValueChanged += OffsetChanged;
            //pointProperty.OnHover += Hover;
            //pointProperty.OnLeave += Leave;
            return pointProperty;

            void OffsetChanged(float value) => point.Offset = value;
            void Hover() => HoverPoint = point;
        }
        void Leave() => HoverPoint = null;

        protected override void OnClear() => Points.Clear();
        protected override void OnObjectUpdate()
        {
            //Offset.OnValueChanged -= OffsetChanged;
            //Offset.Value = EditObject.Offset;
            //Offset.OnValueChanged += OffsetChanged;
        }

        public override void Render(RenderManager.CameraInfo cameraInfo)
        {
            if (IsHoverItem)
                HoverItem.Object.Render(cameraInfo, Colors.White, 2f);

            if(IsHoverPoint)
                HoverPoint.Render(cameraInfo, Colors.White, 2f);

        }
    }
    public class EnterItem : EditableItem<Enter, ColorIcon>
    {
        public override bool ShowDelete => false;
        public override bool ShowIcon => false;
    }
}
