using System.Collections.Generic;

namespace IMT.Manager
{
    public readonly struct MarkingLinePair
    {
        public static MarkupLinePairComparer Comparer { get; } = new MarkupLinePairComparer();

        public readonly MarkingLine first;
        public readonly MarkingLine second;

        public Marking Marking => first.Marking == second.Marking ? first.Marking : null;
        public bool IsSelf => first == second;
        public bool? MustIntersect
        {
            get
            {
                if (IsSelf || first.IsStopLine || second.IsStopLine)
                    return false;

                if (first.IsCrosswalk && second.IsCrosswalk && first.Start.Enter == second.Start.Enter)
                    return false;

                if (first.Start == second.Start && first.GetAlignment(first.Start) == second.GetAlignment(second.Start))
                    return false;
                if (first.Start == second.End && first.GetAlignment(first.Start) == second.GetAlignment(second.End))
                    return false;
                if (first.End == second.Start && first.GetAlignment(first.End) == second.GetAlignment(second.Start))
                    return false;
                if (first.End == second.End && first.GetAlignment(first.End) == second.GetAlignment(second.End))
                    return false;

                if (IsBorder(first, second) || IsBorder(second, first))
                    return true;

                return null;

                static bool IsBorder(MarkingLine line1, MarkingLine line2) => line1 is MarkingCrosswalkLine crosswalkLine && crosswalkLine.Crosswalk.IsBorder(line2);
            }
        }

        public MarkingLinePair(MarkingLine first, MarkingLine second)
        {
            this.first = first;
            this.second = second;
        }
        public bool ContainLine(MarkingLine line) => first == line || second == line;
        public MarkingLine GetOther(MarkingLine line)
        {
            if (ContainLine(line))
                return line == first ? second : first;
            else
                return null;
        }
        public MarkingLine GetLine(MarkingPoint point)
        {
            if (first.ContainsPoint(point))
                return first;
            else if (second.ContainsPoint(point))
                return second;
            else
                return null;
        }

        public override string ToString() => $"{first} × {second}";

        public static bool operator ==(MarkingLinePair a, MarkingLinePair b) => Comparer.Equals(a, b);
        public static bool operator !=(MarkingLinePair a, MarkingLinePair b) => !Comparer.Equals(a, b);

        public class MarkupLinePairComparer : IEqualityComparer<MarkingLinePair>
        {
            public bool Equals(MarkingLinePair x, MarkingLinePair y) => x.first == y.first && x.second == y.second || x.first == y.second && x.second == y.first;
            public int GetHashCode(MarkingLinePair pair) => pair.GetHashCode();
        }
    }
}
