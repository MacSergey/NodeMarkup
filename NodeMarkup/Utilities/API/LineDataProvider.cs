using NodeMarkup.API;
using NodeMarkup.Manager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NodeMarkup.Utilities.API
{
    public class RegularLineDataProvider : IRegularLineData
    {
        private MarkupRegularLine Line { get; }
        public RegularLineDataProvider(MarkupRegularLine line)
        {
            Line = line;
        }
    }
    public class StopLineDataProvider : IStopLineData
    {
        private MarkupStopLine Line { get; }
        public StopLineDataProvider(MarkupStopLine line)
        {
            Line = line;
        }
    }
    public class NormalLineDataProvider : INormalLineData
    {
        private MarkupNormalLine Line { get; }
        public NormalLineDataProvider(MarkupNormalLine line)
        {
            Line = line;
        }
    }
    public class LaneLineDataProvider : ILaneLineData
    {
        private MarkupLaneLine Line { get; }
        public LaneLineDataProvider(MarkupLaneLine line)
        {
            Line = line;
        }
    }
    public class CrosswalkLineDataProvider : ICrosswalkLineData
    {
        private MarkupCrosswalkLine Line { get; }
        public CrosswalkLineDataProvider(MarkupCrosswalkLine line)
        {
            Line = line;
        }
    }
}
