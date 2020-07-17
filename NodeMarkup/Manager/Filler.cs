using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NodeMarkup.Manager
{
    public class MarkupFiller
    {
        public Markup Markup { get; }
        List<IFillerVertex> SupportPoints { get; } = new List<IFillerVertex>();
        public IFillerVertex First => SupportPoints.FirstOrDefault();
        public IFillerVertex Last => SupportPoints.LastOrDefault();
        public IFillerVertex Prev => SupportPoints.Count >= 2 ? SupportPoints[SupportPoints.Count - 2] : null;
        public IEnumerable<IFillerVertex> Vertices => SupportPoints;
        public int VertexCount => SupportPoints.Count;

        public MarkupFiller(Markup markup)
        {
            Markup = markup;
        }

        public void Add(IFillerVertex supportPoint) => SupportPoints.Add(supportPoint);
        public void Remove()
        {
            if (SupportPoints.Any())
                SupportPoints.RemoveAt(SupportPoints.Count - 1);
        }
    }
}
