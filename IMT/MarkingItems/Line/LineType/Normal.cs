using ColossalFramework.Math;
using IMT.Utilities;
using ModsCommon;
using ModsCommon.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Xml.Linq;
using UnityEngine;

namespace IMT.Manager
{
    public class MarkingNormalLine : MarkingRegularLine
    {
        public MarkingNormalLine(Marking marking, MarkingPointPair pointPair, RegularLineStyle style = null, Alignment alignment = Alignment.Centre) : base(marking, pointPair, style, alignment) { }
        protected override ITrajectory CalculateTrajectory() => new StraightTrajectory(Start.GetAbsolutePosition(RawAlignment), End.GetAbsolutePosition(RawAlignment.Value.Invert()));
    }
}
