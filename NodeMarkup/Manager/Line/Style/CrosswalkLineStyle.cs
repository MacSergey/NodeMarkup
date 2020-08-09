using ColossalFramework.Math;
using ColossalFramework.UI;
using NodeMarkup.UI.Editors;
using NodeMarkup.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NodeMarkup.Manager
{
    public class ZebraCrosswalkStyle : CrosswalkStyle, IDashedLine, IParallel
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

        public ZebraCrosswalkStyle(Color32 color, float width, float dashLength, float spaceLength, bool parallel) : base(color, width)
        {
            DashLength = dashLength;
            SpaceLength = spaceLength;
            Parallel = parallel;
        }
        public override CrosswalkStyle CopyCrosswalkStyle() => new ZebraCrosswalkStyle(Color, Width, DashLength, SpaceLength, Parallel);

        public override IEnumerable<MarkupStyleDash> Calculate(MarkupLine line, Bezier3 trajectory)
        {
            var offset = ((line.Start.Direction + line.End.Direction) / -2).normalized * (Width / 2);
            var spaceLength = Parallel ? SpaceLength / Mathf.Sin(line.Start.Enter.CornerAndNormalAngle) : SpaceLength;
            var angle = line.Start.Enter.NormalDir.Turn90(true).AbsoluteAngle();
            return CalculateDashed(trajectory, DashLength, spaceLength, CalculateDashes);

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
    }
}
