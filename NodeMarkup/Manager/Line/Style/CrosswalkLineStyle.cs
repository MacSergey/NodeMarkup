using ColossalFramework.Math;
using ColossalFramework.UI;
using NodeMarkup.UI.Editors;
using NodeMarkup.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using UnityEngine;

namespace NodeMarkup.Manager
{
    public class ExistCrosswalkStyle : CrosswalkStyle
    {
        public override StyleType Type => StyleType.CrosswalkExistent;
        public override float GetTotalWidth(MarkupCrosswalk crosswalk) => Width;

        public ExistCrosswalkStyle(float width) : base(new Color32(0, 0, 0, 0), width) { }

        protected override IEnumerable<MarkupStyleDash> Calculate(MarkupCrosswalk crosswalk, Bezier3 trajectory) => new MarkupStyleDash[0];
        public override CrosswalkStyle CopyCrosswalkStyle() => new ExistCrosswalkStyle(Width);

        public override List<UIComponent> GetUIComponents(object editObject, UIComponent parent, Action onHover = null, Action onLeave = null, bool isTemplate = false)
        {
            return new List<UIComponent> { AddWidthProperty(parent, onHover, onLeave) };
        }
        public override XElement ToXml()
        {
            var config = BaseToXml();
            config.Add(new XAttribute("W", Width));
            return config;
        }
        public override void FromXml(XElement config)
        {
            Width = config.GetAttrValue("W", DefaultCrosswalkWidth);
        }
    }

    public abstract class CustomCrosswalkStyle : CrosswalkStyle, ICrosswalkStyle
    {
        float _offsetBefore;
        float _offsetAfter;

        public float OffsetBefore
        {
            get => _offsetBefore;
            set
            {
                _offsetBefore = value;
                StyleChanged();
            }
        }
        public float OffsetAfter
        {
            get => _offsetAfter;
            set
            {
                _offsetAfter = value;
                StyleChanged();
            }
        }

        public override float GetTotalWidth(MarkupCrosswalk crosswalk) => OffsetBefore + GetVisibleWidth(crosswalk) + OffsetAfter;
        protected abstract float GetVisibleWidth(MarkupCrosswalk crosswalk);

        public CustomCrosswalkStyle(Color32 color, float width, float offsetBefore, float offsetAfter) : base(color, width)
        {
            OffsetBefore = offsetBefore;
            OffsetAfter = offsetAfter;
        }
        public override void CopyTo(Style target)
        {
            base.CopyTo(target);

            if (target is CustomCrosswalkStyle customTarget)
            {
                customTarget.OffsetBefore = OffsetBefore;
                customTarget.OffsetAfter = OffsetAfter;
            }
        }

        public override List<UIComponent> GetUIComponents(object editObject, UIComponent parent, Action onHover = null, Action onLeave = null, bool isTemplate = false)
        {
            var components = new List<UIComponent>
            {
                AddColorProperty(parent),
                AddWidthProperty(parent, onHover, onLeave, 0.1f, 0.5f),
                AddOffsetBeforeProperty(this, parent, onHover, onLeave),
                AddOffsetAfterProperty(this, parent, onHover, onLeave),
            };
            return components;
        }

        protected static BoolPropertyPanel AddParallelProperty(IParallel parallelStyle, UIComponent parent)
        {
            var parallelProperty = parent.AddUIComponent<BoolPropertyPanel>();
            parallelProperty.Text = Localize.LineEditor_ParallelToLanes;
            parallelProperty.Init();
            parallelProperty.Value = parallelStyle.Parallel;
            parallelProperty.OnValueChanged += (bool value) => parallelStyle.Parallel = value;
            return parallelProperty;
        }
        protected static FloatPropertyPanel AddOffsetBeforeProperty(CustomCrosswalkStyle customStyle, UIComponent parent, Action onHover, Action onLeave)
        {
            var offsetBeforeProperty = AddOffsetProperty(parent, onHover, onLeave);
            offsetBeforeProperty.Text = Localize.LineEditor_OffsetBefor;
            offsetBeforeProperty.Value = customStyle.OffsetBefore;
            offsetBeforeProperty.OnValueChanged += (float value) => customStyle.OffsetBefore = value;
            return offsetBeforeProperty;
        }
        protected static FloatPropertyPanel AddOffsetAfterProperty(CustomCrosswalkStyle customStyle, UIComponent parent, Action onHover, Action onLeave)
        {
            var offsetAfterProperty = AddOffsetProperty(parent, onHover, onLeave);
            offsetAfterProperty.Text = Localize.LineEditor_OffsetAfter;
            offsetAfterProperty.Value = customStyle.OffsetAfter;
            offsetAfterProperty.OnValueChanged += (float value) => customStyle.OffsetAfter = value;
            return offsetAfterProperty;
        }
        private static FloatPropertyPanel AddOffsetProperty(UIComponent parent, Action onHover, Action onLeave)
        {
            var offsetProperty = parent.AddUIComponent<FloatPropertyPanel>();
            offsetProperty.UseWheel = true;
            offsetProperty.WheelStep = 0.1f;
            offsetProperty.CheckMin = true;
            offsetProperty.MinValue = 0f;
            offsetProperty.Init();
            AddOnHoverLeave(offsetProperty, onHover, onLeave);
            return offsetProperty;
        }

        public override XElement ToXml()
        {
            var config = base.ToXml();
            config.Add(new XAttribute("OB", OffsetBefore));
            config.Add(new XAttribute("OA", OffsetAfter));
            return config;
        }
        public override void FromXml(XElement config)
        {
            base.FromXml(config);
            OffsetBefore = config.GetAttrValue("OB", DefaultCrosswalkOffset);
            OffsetAfter = config.GetAttrValue("OA", DefaultCrosswalkOffset);
        }
    }

    public class ZebraCrosswalkStyle : CustomCrosswalkStyle, IDashedLine, IParallel
    {
        public override StyleType Type => StyleType.CrosswalkZebra;

        float _dashLength;
        float _spaceLength;
        bool _parallel;
        public float DashLength
        {
            get => _dashLength;
            set
            {
                _dashLength = value;
                StyleChanged();
            }
        }
        public float SpaceLength
        {
            get => _spaceLength;
            set
            {
                _spaceLength = value;
                StyleChanged();
            }
        }
        public bool Parallel
        {
            get => _parallel;
            set
            {
                _parallel = value;
                StyleChanged();
            }
        }

        protected override float GetVisibleWidth(MarkupCrosswalk crosswalk) => Parallel ? Width : Width / Mathf.Sin(crosswalk.CornerAndNormalAngle);

        public ZebraCrosswalkStyle(Color32 color, float width, float offsetBefore, float offsetAfter, float dashLength, float spaceLength, bool parallel) : base(color, width, offsetBefore, offsetAfter)
        {
            DashLength = dashLength;
            SpaceLength = spaceLength;
            Parallel = parallel;
        }
        public override CrosswalkStyle CopyCrosswalkStyle() => new ZebraCrosswalkStyle(Color, Width, OffsetBefore, OffsetAfter, DashLength, SpaceLength, Parallel);
        public override void CopyTo(Style target)
        {
            base.CopyTo(target);

            if (target is IDashedLine dashedTarget)
            {
                dashedTarget.DashLength = DashLength;
                dashedTarget.SpaceLength = SpaceLength;
            }
            if (target is IParallel parallelTarget)
                parallelTarget.Parallel = Parallel;
        }

        protected override IEnumerable<MarkupStyleDash> Calculate(MarkupCrosswalk crosswalk, Bezier3 trajectory)
        {
            var offset = -crosswalk.NormalDir * (GetVisibleWidth(crosswalk) / 2 + OffsetAfter);

            var coef = Mathf.Sin(crosswalk.CornerAndNormalAngle);
            var dashLength = Parallel ? DashLength / coef : DashLength;
            var spaceLength = Parallel ? SpaceLength / coef : SpaceLength;
            var angle = Parallel ? (float?)crosswalk.NormalDir.Turn90(true).AbsoluteAngle() : null;

            return CalculateDashed(trajectory, dashLength, spaceLength, CalculateDashes);

            IEnumerable<MarkupStyleDash> CalculateDashes(Bezier3 dashTrajectory, float startT, float endT)
            {
                yield return CalculateDashedDash(dashTrajectory, startT, endT, DashLength, offset, offset, angle);
            }
        }

        public override List<UIComponent> GetUIComponents(object editObject, UIComponent parent, Action onHover = null, Action onLeave = null, bool isTemplate = false)
        {
            var components = base.GetUIComponents(editObject, parent, onHover, onLeave, isTemplate);
            components.Add(AddDashLengthProperty(this, parent, onHover, onLeave));
            components.Add(AddSpaceLengthProperty(this, parent, onHover, onLeave));
            components.Add(AddParallelProperty(this, parent));
            return components;
        }

        public override XElement ToXml()
        {
            var config = base.ToXml();
            config.Add(new XAttribute("DL", DashLength));
            config.Add(new XAttribute("SL", SpaceLength));
            config.Add(new XAttribute("P", Parallel));
            return config;
        }
        public override void FromXml(XElement config)
        {
            base.FromXml(config);
            DashLength = config.GetAttrValue("DL", DefaultDashLength);
            SpaceLength = config.GetAttrValue("SL", DefaultSpaceLength);
            Parallel = config.GetAttrValue("P", true);
        }
    }
}
