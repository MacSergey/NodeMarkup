using ColossalFramework.UI;
using NodeMarkup.UI.Editors;
using NodeMarkup.Utils;
using System;
using System.Collections.Generic;
using System.Xml.Linq;
using UnityEngine;

namespace NodeMarkup.Manager
{
    public class ExistCrosswalkStyle : CrosswalkStyle
    {
        public override StyleType Type => StyleType.CrosswalkExistent;
        public override float GetTotalWidth(MarkupCrosswalk crosswalk) => Width;

        public ExistCrosswalkStyle(float width) : base(new Color32(0, 0, 0, 0), width) { }

        public override IEnumerable<MarkupStyleDash> Calculate(MarkupCrosswalk crosswalk) => new MarkupStyleDash[0];
        public override CrosswalkStyle CopyCrosswalkStyle() => new ExistCrosswalkStyle(Width);

        public override List<UIComponent> GetUIComponents(object editObject, UIComponent parent, Action onHover = null, Action onLeave = null, bool isTemplate = false)
        {
            return new List<UIComponent> { AddWidthProperty(parent, onHover, onLeave, 0.1f, 0.1f) };
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

    public abstract class CustomCrosswalkStyle : CrosswalkStyle
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
        protected List<UIComponent> GetBaseUIComponents(object editObject, UIComponent parent, Action onHover = null, Action onLeave = null, bool isTemplate = false)
        {
            var components = new List<UIComponent>
            {
                AddColorProperty(parent),
                AddWidthProperty(parent, onHover, onLeave, 0.1f, 0.1f),
                AddOffsetBeforeProperty(this, parent, onHover, onLeave),
                AddOffsetAfterProperty(this, parent, onHover, onLeave),
            };
            return components;
        }
        public override List<UIComponent> GetUIComponents(object editObject, UIComponent parent, Action onHover = null, Action onLeave = null, bool isTemplate = false)
            => GetBaseUIComponents(editObject, parent, onHover, onLeave, isTemplate);


        protected static BoolPropertyPanel AddParallelProperty(IParallel parallelStyle, UIComponent parent)
        {
            var parallelProperty = parent.AddUIComponent<BoolPropertyPanel>();
            parallelProperty.Text = Localize.CrosswalkEditor_ParallelToLanes;
            parallelProperty.Init();
            parallelProperty.Value = parallelStyle.Parallel;
            parallelProperty.OnValueChanged += (bool value) => parallelStyle.Parallel = value;
            return parallelProperty;
        }
        protected static FloatPropertyPanel AddOffsetBeforeProperty(CustomCrosswalkStyle customStyle, UIComponent parent, Action onHover, Action onLeave)
        {
            var offsetBeforeProperty = AddOffsetProperty(parent, onHover, onLeave);
            offsetBeforeProperty.Text = Localize.CrosswalkEditor_OffsetBefore;
            offsetBeforeProperty.Value = customStyle.OffsetBefore;
            offsetBeforeProperty.OnValueChanged += (float value) => customStyle.OffsetBefore = value;
            return offsetBeforeProperty;
        }
        protected static FloatPropertyPanel AddOffsetAfterProperty(CustomCrosswalkStyle customStyle, UIComponent parent, Action onHover, Action onLeave)
        {
            var offsetAfterProperty = AddOffsetProperty(parent, onHover, onLeave);
            offsetAfterProperty.Text = Localize.CrosswalkEditor_OffsetAfter;
            offsetAfterProperty.Value = customStyle.OffsetAfter;
            offsetAfterProperty.OnValueChanged += (float value) => customStyle.OffsetAfter = value;
            return offsetAfterProperty;
        }
        protected static FloatPropertyPanel AddOffsetBetweenProperty(IDoubleCrosswalk customStyle, UIComponent parent, Action onHover, Action onLeave)
        {
            var offsetAfterProperty = AddOffsetProperty(parent, onHover, onLeave, 0.1f);
            offsetAfterProperty.Text = Localize.CrosswalkEditor_OffsetBetween;
            offsetAfterProperty.Value = customStyle.Offset;
            offsetAfterProperty.OnValueChanged += (float value) => customStyle.Offset = value;
            return offsetAfterProperty;
        }
        protected static FloatPropertyPanel AddOffsetProperty(UIComponent parent, Action onHover, Action onLeave, float minValue = 0f)
        {
            var offsetProperty = parent.AddUIComponent<FloatPropertyPanel>();
            offsetProperty.UseWheel = true;
            offsetProperty.WheelStep = 0.1f;
            offsetProperty.CheckMin = true;
            offsetProperty.MinValue = minValue;
            offsetProperty.Init();
            AddOnHoverLeave(offsetProperty, onHover, onLeave);
            return offsetProperty;
        }
        protected FloatPropertyPanel AddLineWidthProperty(ILinedCrosswalk linedStyle, UIComponent parent, Action onHover, Action onLeave)
        {
            var widthProperty = parent.AddUIComponent<FloatPropertyPanel>();
            widthProperty.Text = Localize.CrosswalkEditor_LineWidth;
            widthProperty.UseWheel = true;
            widthProperty.WheelStep = 0.1f;
            widthProperty.CheckMin = true;
            widthProperty.MinValue = 0.05f;
            widthProperty.Init();
            widthProperty.Value = linedStyle.LineWidth;
            widthProperty.OnValueChanged += (float value) => linedStyle.LineWidth = value;
            AddOnHoverLeave(widthProperty, onHover, onLeave);

            return widthProperty;
        }
        protected bool Cut(MarkupCrosswalk crosswalk, ILineTrajectory trajectory, float width, out ILineTrajectory cutTrajectory)
        {
            var delta = width / Mathf.Tan(crosswalk.CornerAndNormalAngle) / 2;
            if (2 * delta >= trajectory.Magnitude)
            {
                cutTrajectory = default;
                return false;
            }
            else
            {
                var startCut = trajectory.Travel(0, delta);
                var endCut = trajectory.Invert().Travel(0, delta);
                cutTrajectory = trajectory.Cut(startCut, 1 - endCut);
                return true;
            }
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
    public abstract class LinedCrosswalkStyle : CustomCrosswalkStyle, ICrosswalkStyle, ILinedCrosswalk
    {
        float _lineWidth;
        public float LineWidth
        {
            get => _lineWidth;
            set
            {
                _lineWidth = value;
                StyleChanged();
            }
        }

        public LinedCrosswalkStyle(Color32 color, float width, float offsetBefore, float offsetAfter, float lineWidth) :
            base(color, width, offsetBefore, offsetAfter)
        {
            LineWidth = lineWidth;
        }
        protected override float GetVisibleWidth(MarkupCrosswalk crosswalk) => Width / Mathf.Sin(crosswalk.CornerAndNormalAngle);
        public override void CopyTo(Style target)
        {
            base.CopyTo(target);
            if (target is ILinedCrosswalk linedTarget)
                linedTarget.LineWidth = LineWidth;
        }
        public override List<UIComponent> GetUIComponents(object editObject, UIComponent parent, Action onHover = null, Action onLeave = null, bool isTemplate = false)
        {
            var components = base.GetUIComponents(editObject, parent, onHover, onLeave, isTemplate);
            components.Add(AddLineWidthProperty(this, parent, onHover, onLeave));
            return components;
        }
        public override XElement ToXml()
        {
            var config = base.ToXml();
            config.Add(new XAttribute("LW", LineWidth));
            return config;
        }
        public override void FromXml(XElement config)
        {
            base.FromXml(config);
            LineWidth = config.GetAttrValue("LW", DefaultCrosswalkOffset);
        }
    }

    public class ZebraCrosswalkStyle : CustomCrosswalkStyle, ICrosswalkStyle, IDashedCrosswalk, IParallel
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

        protected override float GetVisibleWidth(MarkupCrosswalk crosswalk) => GetLengthCoef(Width, crosswalk);
        protected float GetLengthCoef(float length, MarkupCrosswalk crosswalk) => length / (Parallel ? 1 : Mathf.Sin(crosswalk.CornerAndNormalAngle));

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

            if (target is IDashedCrosswalk dashedTarget)
            {
                dashedTarget.DashLength = DashLength;
                dashedTarget.SpaceLength = SpaceLength;
            }
            if (target is IParallel parallelTarget)
                parallelTarget.Parallel = Parallel;
        }

        public override IEnumerable<MarkupStyleDash> Calculate(MarkupCrosswalk crosswalk)
        {
            var offset = GetVisibleWidth(crosswalk) / 2 + OffsetBefore;

            var coef = Mathf.Sin(crosswalk.CornerAndNormalAngle);
            var dashLength = Parallel ? DashLength / coef : DashLength;
            var spaceLength = Parallel ? SpaceLength / coef : SpaceLength;
            var direction = Parallel ? crosswalk.NormalDir : crosswalk.CornerDir.Turn90(true);
            var borders = crosswalk.BorderTrajectories;

            var trajectory = crosswalk.GetFullTrajectory(offset, direction);

            return StyleHelper.CalculateDashed(trajectory, dashLength, spaceLength, CalculateDashes);

            IEnumerable<MarkupStyleDash> CalculateDashes(ILineTrajectory crosswalkTrajectory, float startT, float endT)
                => CalculateCroswalkDash(crosswalkTrajectory, startT, endT, direction, borders, Width, DashLength);
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
            DashLength = config.GetAttrValue("DL", LineStyle.DefaultDashLength);
            SpaceLength = config.GetAttrValue("SL", LineStyle.DefaultSpaceLength);
            Parallel = config.GetAttrValue("P", true);
        }
    }
    public class DoubleZebraCrosswalkStyle : ZebraCrosswalkStyle, ICrosswalkStyle, IDoubleCrosswalk
    {
        public override StyleType Type => StyleType.CrosswalkDoubleZebra;

        float _offset;
        public float Offset
        {
            get => _offset;
            set
            {
                _offset = value;
                StyleChanged();
            }
        }

        public DoubleZebraCrosswalkStyle(Color32 color, float width, float offsetBefore, float offsetAfter, float dashLength, float spaceLength, bool parallel, float offset) :
            base(color, width, offsetBefore, offsetAfter, dashLength, spaceLength, parallel)
        {
            Offset = offset;
        }
        protected override float GetVisibleWidth(MarkupCrosswalk crosswalk) => GetLengthCoef(Width * 2 + Offset, crosswalk);
        public override CrosswalkStyle CopyCrosswalkStyle() => new DoubleZebraCrosswalkStyle(Color, Width, OffsetBefore, OffsetAfter, DashLength, SpaceLength, Parallel, Offset);
        public override void CopyTo(Style target)
        {
            base.CopyTo(target);
            if (target is IDoubleCrosswalk doubleTarget)
                doubleTarget.Offset = Offset;
        }

        public override IEnumerable<MarkupStyleDash> Calculate(MarkupCrosswalk crosswalk)
        {
            var middleOffset = GetVisibleWidth(crosswalk) / 2 + OffsetBefore;
            var deltaOffset = GetLengthCoef((Width + Offset) / 2, crosswalk);
            var firstOffset = -crosswalk.NormalDir * (middleOffset - deltaOffset);
            var secondOffset = -crosswalk.NormalDir * (middleOffset + deltaOffset);

            var coef = Mathf.Sin(crosswalk.CornerAndNormalAngle);
            var dashLength = Parallel ? DashLength / coef : DashLength;
            var spaceLength = Parallel ? SpaceLength / coef : SpaceLength;
            var direction = Parallel ? crosswalk.NormalDir : crosswalk.CornerDir.Turn90(true);
            var borders = crosswalk.BorderTrajectories;

            var trajectoryFirst = crosswalk.GetFullTrajectory(middleOffset - deltaOffset, direction);
            var trajectorySecond = crosswalk.GetFullTrajectory(middleOffset + deltaOffset, direction);

            foreach (var dash in StyleHelper.CalculateDashed(trajectoryFirst, dashLength, spaceLength, CalculateDashes))
                yield return dash;

            foreach (var dash in StyleHelper.CalculateDashed(trajectorySecond, dashLength, spaceLength, CalculateDashes))
                yield return dash;

            IEnumerable<MarkupStyleDash> CalculateDashes(ILineTrajectory crosswalkTrajectory, float startT, float endT)
                => CalculateCroswalkDash(crosswalkTrajectory, startT, endT, direction, borders, Width, DashLength);
        }

        public override List<UIComponent> GetUIComponents(object editObject, UIComponent parent, Action onHover = null, Action onLeave = null, bool isTemplate = false)
        {
            var components = GetBaseUIComponents(editObject, parent, onHover, onLeave, isTemplate);
            components.Add(AddOffsetBetweenProperty(this, parent, onHover, onLeave));
            components.Add(AddDashLengthProperty(this, parent, onHover, onLeave));
            components.Add(AddSpaceLengthProperty(this, parent, onHover, onLeave));
            components.Add(AddParallelProperty(this, parent));
            return components;
        }

        public override XElement ToXml()
        {
            var config = base.ToXml();
            config.Add(new XAttribute("O", Offset));
            return config;
        }
        public override void FromXml(XElement config)
        {
            base.FromXml(config);
            Offset = config.GetAttrValue("O", DefaultCrosswalkOffset);
        }
    }
    public class ParallelSolidLinesCrosswalkStyle : LinedCrosswalkStyle, ICrosswalkStyle
    {
        public override StyleType Type => StyleType.CrosswalkParallelSolidLines;

        public ParallelSolidLinesCrosswalkStyle(Color32 color, float width, float offsetBefore, float offsetAfter, float lineWidth) :
            base(color, width, offsetBefore, offsetAfter, lineWidth)
        { }

        public override CrosswalkStyle CopyCrosswalkStyle() => new ParallelSolidLinesCrosswalkStyle(Color, Width, OffsetBefore, OffsetAfter, LineWidth);

        public override IEnumerable<MarkupStyleDash> Calculate(MarkupCrosswalk crosswalk)
        {
            var middleOffset = GetVisibleWidth(crosswalk) / 2 + OffsetBefore;
            var deltaOffset = (Width - LineWidth) / 2 / Mathf.Sin(crosswalk.CornerAndNormalAngle);
            var firstTrajectory = crosswalk.GetTrajectory(middleOffset - deltaOffset);
            var secondTrajectory = crosswalk.GetTrajectory(middleOffset + deltaOffset);

            foreach (var dash in StyleHelper.CalculateSolid(firstTrajectory, CalculateDashes))
                yield return dash;

            foreach (var dash in StyleHelper.CalculateSolid(secondTrajectory, CalculateDashes))
                yield return dash;

            IEnumerable<MarkupStyleDash> CalculateDashes(ILineTrajectory dashTrajectory)
            {
                yield return StyleHelper.CalculateSolidDash(dashTrajectory, 0, LineWidth, Color);
            }
        }
    }
    public class ParallelDashedLinesCrosswalkStyle : LinedCrosswalkStyle, ICrosswalkStyle, IDashedLine
    {
        public override StyleType Type => StyleType.CrosswalkParallelDashedLines;

        float _dashLength;
        float _spaceLength;
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

        public ParallelDashedLinesCrosswalkStyle(Color32 color, float width, float offsetBefore, float offsetAfter, float lineWidth, float dashLength, float spaceLength) :
            base(color, width, offsetBefore, offsetAfter, lineWidth)
        {
            DashLength = dashLength;
            SpaceLength = spaceLength;
        }

        public override CrosswalkStyle CopyCrosswalkStyle() => new ParallelDashedLinesCrosswalkStyle(Color, Width, OffsetBefore, OffsetAfter, LineWidth, DashLength, SpaceLength);

        public override void CopyTo(Style target)
        {
            base.CopyTo(target);
            if (target is IDashedLine dashedTarget)
            {
                dashedTarget.DashLength = DashLength;
                dashedTarget.SpaceLength = SpaceLength;
            }
        }
        public override List<UIComponent> GetUIComponents(object editObject, UIComponent parent, Action onHover = null, Action onLeave = null, bool isTemplate = false)
        {
            var components = base.GetUIComponents(editObject, parent, onHover, onLeave, isTemplate);
            components.Add(AddDashLengthProperty(this, parent, onHover, onLeave));
            components.Add(AddSpaceLengthProperty(this, parent, onHover, onLeave));
            return components;
        }

        public override IEnumerable<MarkupStyleDash> Calculate(MarkupCrosswalk crosswalk)
        {
            var middleOffset = GetVisibleWidth(crosswalk) / 2 + OffsetBefore;
            var deltaOffset = (Width - LineWidth) / 2 / Mathf.Sin(crosswalk.CornerAndNormalAngle);
            var firstTrajectory = crosswalk.GetTrajectory(middleOffset - deltaOffset);
            var secondTrajectory = crosswalk.GetTrajectory(middleOffset + deltaOffset);

            foreach (var dash in StyleHelper.CalculateDashed(firstTrajectory, DashLength, SpaceLength, CalculateDashes))
                yield return dash;

            foreach (var dash in StyleHelper.CalculateDashed(secondTrajectory, DashLength, SpaceLength, CalculateDashes))
                yield return dash;

            IEnumerable<MarkupStyleDash> CalculateDashes(ILineTrajectory dashTrajectory, float startT, float endT)
            {
                yield return StyleHelper.CalculateDashedDash(dashTrajectory, startT, endT, DashLength, 0, LineWidth, Color);
            }
        }

        public override XElement ToXml()
        {
            var config = base.ToXml();
            config.Add(new XAttribute("DL", DashLength));
            config.Add(new XAttribute("SL", SpaceLength));
            return config;
        }
        public override void FromXml(XElement config)
        {
            base.FromXml(config);
            DashLength = config.GetAttrValue("DL", LineStyle.DefaultDashLength);
            SpaceLength = config.GetAttrValue("SL", LineStyle.DefaultSpaceLength);
        }
    }
    public class LadderCrosswalkStyle : ParallelSolidLinesCrosswalkStyle, ICrosswalkStyle, IDashedCrosswalk
    {
        public override StyleType Type => StyleType.CrosswalkLadder;

        float _dashLength;
        float _spaceLength;
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

        public LadderCrosswalkStyle(Color32 color, float width, float offsetBefore, float offsetAfter, float dashLength, float spaceLength, float lineWidth) : base(color, width, offsetBefore, offsetAfter, lineWidth)
        {
            DashLength = dashLength;
            SpaceLength = spaceLength;
        }

        public override CrosswalkStyle CopyCrosswalkStyle() => new LadderCrosswalkStyle(Color, Width, OffsetBefore, OffsetAfter, DashLength, SpaceLength, LineWidth);
        public override void CopyTo(Style target)
        {
            base.CopyTo(target);

            if (target is IDashedCrosswalk dashedTarget)
            {
                dashedTarget.DashLength = DashLength;
                dashedTarget.SpaceLength = SpaceLength;
            }
        }

        public override IEnumerable<MarkupStyleDash> Calculate(MarkupCrosswalk crosswalk)
        {
            foreach (var dash in base.Calculate(crosswalk))
                yield return dash;

            var offset = GetVisibleWidth(crosswalk) / 2 + OffsetBefore;

            var direction = crosswalk.CornerDir.Turn90(true);
            var borders = crosswalk.BorderTrajectories;
            var width = Width - 2 * LineWidth;

            var trajectory = crosswalk.GetFullTrajectory(offset, direction);

            foreach (var dash in StyleHelper.CalculateDashed(trajectory, DashLength, SpaceLength, CalculateDashes))
                yield return dash;

            IEnumerable<MarkupStyleDash> CalculateDashes(ILineTrajectory crosswalkTrajectory, float startT, float endT)
                => CalculateCroswalkDash(crosswalkTrajectory, startT, endT, direction, borders, Width, DashLength);
        }

        public override List<UIComponent> GetUIComponents(object editObject, UIComponent parent, Action onHover = null, Action onLeave = null, bool isTemplate = false)
        {
            var components = base.GetUIComponents(editObject, parent, onHover, onLeave, isTemplate);
            components.Add(AddDashLengthProperty(this, parent, onHover, onLeave));
            components.Add(AddSpaceLengthProperty(this, parent, onHover, onLeave));
            return components;
        }

        public override XElement ToXml()
        {
            var config = base.ToXml();
            config.Add(new XAttribute("DL", DashLength));
            config.Add(new XAttribute("SL", SpaceLength));
            return config;
        }
        public override void FromXml(XElement config)
        {
            base.FromXml(config);
            DashLength = config.GetAttrValue("DL", LineStyle.DefaultDashLength);
            SpaceLength = config.GetAttrValue("SL", LineStyle.DefaultSpaceLength);
        }
    }
    public class SolidCrosswalkStyle : CustomCrosswalkStyle, ICrosswalkStyle
    {
        public override StyleType Type => StyleType.CrosswalkSolid;

        public SolidCrosswalkStyle(Color32 color, float width, float offsetBefore, float offsetAfter) : base(color, width, offsetBefore, offsetAfter) { }

        public override CrosswalkStyle CopyCrosswalkStyle() => new SolidCrosswalkStyle(Color, Width, OffsetBefore, OffsetAfter);
        protected override float GetVisibleWidth(MarkupCrosswalk crosswalk) => Width / Mathf.Sin(crosswalk.CornerAndNormalAngle);

        public override IEnumerable<MarkupStyleDash> Calculate(MarkupCrosswalk crosswalk)
        {
            StyleHelper.GetParts(Width, 0, out int count, out float partWidth);
            var partOffset = GetVisibleWidth(crosswalk) / count;
            var startOffset = partOffset / 2;
            for (var i = 0; i < count; i += 1)
            {
                var trajectory = crosswalk.GetTrajectory(startOffset + partOffset * i + OffsetBefore);
                yield return new MarkupStyleDash(trajectory.StartPosition, trajectory.EndPosition, trajectory.Direction, partWidth, Color);
            }
        }
    }
}
