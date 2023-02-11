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
    public class MarkingFillerTempLine : MarkingRegularLine
    {
        public MarkingFillerTempLine(Marking marking, MarkingPoint first, MarkingPoint second, Alignment alignment) : base(marking, first, second, null, alignment) { }
        public MarkingFillerTempLine(Marking marking, MarkingPointPair pair, Alignment alignment) : base(marking, pair, null, alignment) { }
    }
}
