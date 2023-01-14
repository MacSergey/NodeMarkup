using NodeMarkup.API;
using NodeMarkup.Manager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NodeMarkup.Utilities.API
{
    public struct CrosswalkDataProvider : ICrosswalkData
    {
        private MarkupCrosswalk Crosswalk { get; }
        public ICrosswalkLineData Line => new CrosswalkLineDataProvider(Crosswalk.CrosswalkLine);

        public CrosswalkDataProvider(MarkupCrosswalk crosswalk)
        {
            Crosswalk = crosswalk;
        }

        public override string ToString() => Crosswalk.ToString();
    }

    public struct FillerDataProvider : IFillerData
    {
        private MarkupFiller Filler { get;}

        public FillerDataProvider(MarkupFiller filler)
        {
            Filler = filler;
        }

        public override string ToString() => Filler.ToString();
    }
}
