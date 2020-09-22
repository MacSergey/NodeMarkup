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
    public class SolidStopLineStyle : StopLineStyle, IStopLine
    {
        public override StyleType Type => StyleType.StopLineSolid;

        public SolidStopLineStyle(Color32 color, float width) : base(color, width) { }

        protected override IEnumerable<MarkupStyleDash> Calculate(MarkupStopLine stopLine, ILineTrajectory trajectory)
        {
            var offset = ((stopLine.Start.Direction + stopLine.End.Direction) / -2).normalized * (Width / 2);
            return StyleHelper.CalculateSolid(trajectory, CalculateDashes);

            IEnumerable<MarkupStyleDash> CalculateDashes(ILineTrajectory dashTrajectory)
            {
                yield return StyleHelper.CalculateSolidDash(dashTrajectory, offset, offset, Width, Color);
            }
        }

        public override StopLineStyle CopyStopLineStyle() => new SolidStopLineStyle(Color, Width);
    }
    public class DoubleSolidStopLineStyle : SolidStopLineStyle, IStopLine, IDoubleLine
    {
        public override StyleType Type => StyleType.StopLineDoubleSolid;

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

        public DoubleSolidStopLineStyle(Color color, float width, float offset) : base(color, width)
        {
            Offset = offset;
        }
        protected override IEnumerable<MarkupStyleDash> Calculate(MarkupStopLine stopLine, ILineTrajectory trajectory)
        {
            var offsetNormal = ((stopLine.Start.Direction + stopLine.End.Direction) / -2).normalized;
            var offsetLeft = offsetNormal * (Width / 2);
            var offsetRight = offsetNormal * (Width / 2 + 2 * Offset);

            return StyleHelper.CalculateSolid(trajectory, CalculateDashes);

            IEnumerable<MarkupStyleDash> CalculateDashes(ILineTrajectory dashTrajectory)
            {
                yield return StyleHelper.CalculateSolidDash(dashTrajectory, offsetLeft, offsetLeft, Width, Color);
                yield return StyleHelper.CalculateSolidDash(dashTrajectory, offsetRight, offsetRight, Width, Color);
            }
        }

        public override StopLineStyle CopyStopLineStyle() => new DoubleSolidStopLineStyle(Color, Width, Offset);
        public override void CopyTo(Style target)
        {
            base.CopyTo(target);
            if (target is IDoubleLine doubleTarget)
            {
                doubleTarget.Offset = Offset;
            }
        }

        public override List<UIComponent> GetUIComponents(object editObject, UIComponent parent, Action onHover = null, Action onLeave = null, bool isTemplate = false)
        {
            var components = base.GetUIComponents(editObject, parent, onHover, onLeave, isTemplate);
            components.Add(AddOffsetProperty(this, parent, onHover, onLeave));
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
            Offset = config.GetAttrValue("O", DefaultOffset);
        }
    }

    public class DashedStopLineStyle : StopLineStyle, IStopLine, IDashedLine
    {
        public override StyleType Type { get; } = StyleType.StopLineDashed;

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

        public DashedStopLineStyle(Color color, float width, float dashLength, float spaceLength) : base(color, width)
        {
            DashLength = dashLength;
            SpaceLength = spaceLength;
        }

        protected override IEnumerable<MarkupStyleDash> Calculate(MarkupStopLine stopLine, ILineTrajectory trajectory)
        {
            var offset = ((stopLine.Start.Direction + stopLine.End.Direction) / -2).normalized * (Width / 2);
            return StyleHelper.CalculateDashed(trajectory, DashLength, SpaceLength, CalculateDashes);

            IEnumerable<MarkupStyleDash> CalculateDashes(ILineTrajectory dashTrajectory, float startT, float endT)
            {
                yield return StyleHelper.CalculateDashedDash(dashTrajectory, startT, endT, DashLength, offset, offset, Width, Color);
            }
        }

        public override StopLineStyle CopyStopLineStyle() => new DashedStopLineStyle(Color, Width, DashLength, SpaceLength);
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
            DashLength = config.GetAttrValue("DL", DefaultDashLength);
            SpaceLength = config.GetAttrValue("SL", DefaultSpaceLength);
        }
    }
    public class DoubleDashedStopLineStyle : DashedStopLineStyle, IStopLine, IDoubleLine
    {
        public override StyleType Type { get; } = StyleType.StopLineDoubleDashed;

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
        public DoubleDashedStopLineStyle(Color color, float width, float dashLength, float spaceLength, float offset) : base(color, width, dashLength, spaceLength)
        {
            Offset = offset;
        }
        public override StopLineStyle CopyStopLineStyle() => new DoubleDashedStopLineStyle(Color, Width, DashLength, SpaceLength, Offset);
        public override void CopyTo(Style target)
        {
            base.CopyTo(target);
            if (target is IDoubleLine doubleTarget)
            {
                doubleTarget.Offset = Offset;
            }
        }

        protected override IEnumerable<MarkupStyleDash> Calculate(MarkupStopLine stopLine, ILineTrajectory trajectory)
        {
            var offsetNormal = ((stopLine.Start.Direction + stopLine.End.Direction) / -2).normalized;
            var offsetLeft = offsetNormal * (Width / 2);
            var offsetRight = offsetNormal * (Width / 2 + 2 * Offset);

            return StyleHelper.CalculateDashed(trajectory, DashLength, SpaceLength, CalculateDashes);

            IEnumerable<MarkupStyleDash> CalculateDashes(ILineTrajectory dashTrajectory, float startT, float endT)
            {
                yield return StyleHelper.CalculateDashedDash(dashTrajectory, startT, endT, DashLength, offsetLeft, offsetLeft, Width, Color);
                yield return StyleHelper.CalculateDashedDash(dashTrajectory, startT, endT, DashLength, offsetRight, offsetRight, Width, Color);
            }
        }

        public override List<UIComponent> GetUIComponents(object editObject, UIComponent parent, Action onHover = null, Action onLeave = null, bool isTemplate = false)
        {
            var components = base.GetUIComponents(editObject, parent, onHover, onLeave, isTemplate);
            components.Add(AddOffsetProperty(this, parent, onHover, onLeave));
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
            Offset = config.GetAttrValue("O", DefaultOffset);
        }
    }
    public class ChessBoardStopLineStyle : StopLineStyle, IColorStyle, IAsymLine
    {
        public override StyleType Type => StyleType.StopLineChessBoard;

        bool _invert;
        float _squareSide;
        int _lineCount;
        public bool Invert
        {
            get => _invert;
            set
            {
                _invert = value;
                StyleChanged();
            }
        }
        public float SquareSide
        {
            get => _squareSide;
            set
            {
                _squareSide = value;
                StyleChanged();
            }
        }
        public int LineCount
        {
            get => _lineCount;
            set
            {
                _lineCount = value;
                StyleChanged();
            }
        }

        public ChessBoardStopLineStyle(Color32 color, float squareSide, int lineCount, bool invert) : base(color, 0) 
        {
            SquareSide = squareSide;
            LineCount = lineCount;
            Invert = invert;
        }
        protected override IEnumerable<MarkupStyleDash> Calculate(MarkupStopLine stopLine, ILineTrajectory trajectory)
        {
            yield break;
            //var angle = stopLine.Start.Enter.CornerAndNormalAngle;
            //var additionalLength = SquareSide * LineCount * Mathf.Tan(angle);
            //var fullLength = trajectory.Length + additionalLength;
            //var count = (int)(fullLength / SquareSide);
            //var startSpace = (fullLength - SquareSide * count) / 2;

            //var offsetDir = ((stopLine.Start.Direction + stopLine.End.Direction) / -2).normalized;

            //for (var i = 0; i < LineCount; i += 1)
            //{
            //    for (var j = 0; i < count; j += 2)
            //    {

            //    }
            //}
        }

        public override StopLineStyle CopyStopLineStyle() => new ChessBoardStopLineStyle(Color, SquareSide, LineCount, Invert);
        public override void CopyTo(Style target)
        {
            base.CopyTo(target);
            if (target is ChessBoardStopLineStyle chessBoardTarget)
            {
                chessBoardTarget.SquareSide = SquareSide;
                chessBoardTarget.LineCount = LineCount;
                chessBoardTarget.Invert = Invert;
            }
        }

        public override List<UIComponent> GetUIComponents(object editObject, UIComponent parent, Action onHover = null, Action onLeave = null, bool isTemplate = false)
        {
            var components = base.GetUIComponents(editObject, parent, onHover, onLeave, isTemplate);
            components.Add(AddSquareSideProperty(this, parent, onHover, onLeave));
            components.Add(AddLineCountProperty(this, parent, onHover, onLeave));
            if (!isTemplate)
                components.Add(AddInvertProperty(this, parent));
            return components;
        }
        protected static FloatPropertyPanel AddSquareSideProperty(ChessBoardStopLineStyle chessBoardStyle, UIComponent parent, Action onHover, Action onLeave)
        {
            var squareSideProperty = parent.AddUIComponent<FloatPropertyPanel>();
            squareSideProperty.Text = Localize.LineEditor_SquareSide;
            squareSideProperty.UseWheel = true;
            squareSideProperty.WheelStep = 0.1f;
            squareSideProperty.CheckMin = true;
            squareSideProperty.MinValue = 0.1f;
            squareSideProperty.Init();
            squareSideProperty.Value = chessBoardStyle.SquareSide;
            squareSideProperty.OnValueChanged += (float value) => chessBoardStyle.SquareSide = value;
            AddOnHoverLeave(squareSideProperty, onHover, onLeave);
            return squareSideProperty;
        }
        protected static IntPropertyPanel AddLineCountProperty(ChessBoardStopLineStyle chessBoardStyle, UIComponent parent, Action onHover, Action onLeave)
        {
            var lineCountProperty = parent.AddUIComponent<IntPropertyPanel>();
            lineCountProperty.Text = Localize.LineEditor_LineCount;
            lineCountProperty.UseWheel = true;
            lineCountProperty.WheelStep = 1;
            lineCountProperty.CheckMin = true;
            lineCountProperty.MinValue = 2;
            lineCountProperty.Init();
            lineCountProperty.Value = chessBoardStyle.LineCount;
            lineCountProperty.OnValueChanged += (int value) => chessBoardStyle.LineCount = value;
            AddOnHoverLeave(lineCountProperty, onHover, onLeave);
            return lineCountProperty;
        }

        public override XElement ToXml()
        {
            var config = base.ToXml();
            config.Add(new XAttribute("SS", SquareSide));
            config.Add(new XAttribute("LC", LineCount));
            config.Add(new XAttribute("I", Invert ? 1 : 0));
            return config;
        }
        public override void FromXml(XElement config)
        {
            base.FromXml(config);
            SquareSide = config.GetAttrValue("SS", DefaultStopWidth);
            LineCount = config.GetAttrValue("LC", 2);
            Invert = config.GetAttrValue("I", 0) == 1;
        }
    }
}
