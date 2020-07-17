using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NodeMarkup.Manager
{
    public class MarkupFiller : IEnumerable<ISupportPoint>
    {
        List<ISupportPoint> SupportPoints { get; } = new List<ISupportPoint>();
        public ISupportPoint Last => SupportPoints.LastOrDefault();

        public IEnumerator<ISupportPoint> GetEnumerator() => SupportPoints.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public void Add(ISupportPoint supportPoint) => SupportPoints.Add(supportPoint);
        public void Remove()
        {
            if (SupportPoints.Any())
                SupportPoints.RemoveAt(SupportPoints.Count - 1);
        }
    }
}
