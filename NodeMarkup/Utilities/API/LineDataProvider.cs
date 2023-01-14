using NodeMarkup.API;
using NodeMarkup.Manager;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace NodeMarkup.Utilities.API
{
    public struct RegularLineDataProvider : IRegularLineData
    {
        private MarkupRegularLine Line { get; }
        public RegularLineDataProvider(MarkupRegularLine line)
        {
            Line = line;
        }

        public override string ToString() => Line.ToString();
    }
    public struct StopLineDataProvider : IStopLineData
    {
        private MarkupStopLine Line { get; }
        public StopLineDataProvider(MarkupStopLine line)
        {
            Line = line;
        }

        public override string ToString() => Line.ToString();
    }
    public struct NormalLineDataProvider : INormalLineData
    {
        private MarkupNormalLine Line { get; }
        public NormalLineDataProvider(MarkupNormalLine line)
        {
            Line = line;
        }

        public override string ToString() => Line.ToString();
    }
    public struct LaneLineDataProvider : ILaneLineData
    {
        private MarkupLaneLine Line { get; }
        public LaneLineDataProvider(MarkupLaneLine line)
        {
            Line = line;
        }

        public override string ToString() => Line.ToString();
    }
    public struct CrosswalkLineDataProvider : ICrosswalkLineData
    {
        private MarkupCrosswalkLine Line { get; }
        public ICrosswalkData Crosswalk => new CrosswalkDataProvider(Line.Crosswalk);

        public CrosswalkLineDataProvider(MarkupCrosswalkLine line)
        {
            Line = line;
        }

        public override string ToString() => Line.ToString();
    }
}
