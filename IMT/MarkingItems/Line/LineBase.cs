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
    public abstract class MarkingLine : IStyleItem, IToXml, ISupport
    {
        public static string XmlName { get; } = "L";

        public string DeleteCaptionDescription => Localize.LineEditor_DeleteCaptionDescription;
        public string DeleteMessageDescription => Localize.LineEditor_DeleteMessageDescription;
        public Marking.SupportType Support => Marking.SupportType.Lines;

        public abstract LineType Type { get; }

        public Marking Marking { get; private set; }
        public ulong Id => PointPair.Hash;

        public MarkingPointPair PointPair { get; }
        public MarkingPoint Start => PointPair.First;
        public MarkingPoint End => PointPair.Second;
        public virtual bool IsSupportRules => false;
        public bool IsEnterLine => PointPair.IsSameEnter;
        public bool IsSame => PointPair.IsSame;
        public bool IsNormal => PointPair.IsNormal;
        public bool IsStopLine => PointPair.IsStopLine;
        public bool IsCrosswalk => PointPair.IsCrosswalk;
        public virtual Alignment Alignment => Alignment.Centre;

        public bool HasOverlapped => Rules.Any(r => r.IsOverlapped);

        public abstract IEnumerable<MarkingLineRawRule> Rules { get; }
        public abstract IEnumerable<ILinePartEdge> RulesEdges { get; }

        public ITrajectory Trajectory { get; private set; }
        public List<IStyleData> StyleData { get; } = new List<IStyleData>();

        public string XmlSection => XmlName;

        protected MarkingLine(Marking marking, MarkingPointPair pointPair, bool update = true)
        {
            Marking = marking;
            PointPair = pointPair;

            if (update)
                Update(true);
        }
        protected virtual void RuleChanged() => Marking.Update(this, true);

        public void Update(bool onlySelfUpdate = false)
        {
            Trajectory = CalculateTrajectory();
            if (!onlySelfUpdate)
                Marking.Update(this);
        }
        protected abstract ITrajectory CalculateTrajectory();

        public void RecalculateStyleData()
        {
#if DEBUG_RECALCULATE
            Mod.Logger.Debug($"Recalculate line {this}");
#endif
            StyleData.Clear();
            GetStyleData(StyleData.Add);
        }

        protected abstract void GetStyleData(Action<IStyleData> addData);

        public bool ContainsPoint(MarkingPoint point) => PointPair.ContainsPoint(point);

        protected IEnumerable<ILinePartEdge> RulesEnterPointEdge
        {
            get
            {
                yield return new EnterPointEdge(Start);
                yield return new EnterPointEdge(End);
            }
        }
        protected IEnumerable<ILinePartEdge> RulesLinesIntersectEdge
        {
            get
            {
                foreach (var line in IntersectLines)
                    yield return new LinesIntersectEdge(this, line);
            }
        }

        public IEnumerable<MarkingLine> IntersectLines
        {
            get
            {
                foreach (var intersect in Marking.GetIntersects(this))
                {
                    if (intersect.IsIntersect)
                        yield return intersect.pair.GetOther(this);
                }
            }
        }
        public virtual void Render(OverlayData data) => Trajectory.Render(data);
        public virtual void RenderRule(MarkingLineRawRule rule, OverlayData data)
        {
            if (rule.Style.Value.RenderOverlay && rule.GetTrajectory(out var trajectory))
                trajectory.Render(data);
        }
        public abstract bool ContainsRule(MarkingLineRawRule rule);
        public bool ContainsEnter(Entrance enter) => PointPair.ContainsEnter(enter);

        public Dependences GetDependences() => Marking.GetLineDependences(this);
        public bool IsStart(MarkingPoint point) => Start == point;
        public bool IsEnd(MarkingPoint point) => End == point;
        public Alignment GetAlignment(MarkingPoint point) => PointPair.ContainsPoint(point) && point.IsSplit ? (IsStart(point) ? Alignment : Alignment.Invert()) : Alignment.Centre;


        public virtual XElement ToXml()
        {
            var config = new XElement(XmlSection);
            config.AddAttr(nameof(Id), Id);
            config.AddAttr("T", (int)Type);

            return config;
        }
        public static bool FromXml(XElement config, Marking marking, ObjectsMap map, out MarkingLine line, out bool invert)
        {
            var lineId = config.GetAttrValue<ulong>(nameof(Id));
            if (!MarkingPointPair.FromHash(lineId, marking, map, out MarkingPointPair pointPair, out invert))
            {
                line = null;
                return false;
            }

            if (!marking.TryGetLine(pointPair, out line))
            {
                var type = (LineType)config.GetAttrValue("T", (int)pointPair.DefaultType);
                if ((type & marking.SupportLines) == 0)
                    return false;

                switch (type)
                {
                    case LineType.Regular:
                        line = pointPair.IsNormal ? new MarkingNormalLine(marking, pointPair) : new MarkingRegularLine(marking, pointPair);
                        break;
                    case LineType.Stop:
                        line = new MarkingStopLine(marking, pointPair);
                        break;
                    case LineType.Crosswalk:
                        line = new MarkingCrosswalkLine(marking, pointPair);
                        break;
                    case LineType.Lane:
                        line = new MarkingLaneLine(marking, pointPair);
                        break;
                    default:
                        return false;
                }
            }

            return true;
        }
        public abstract void FromXml(XElement config, ObjectsMap map, bool invert, bool typeChanged);

        public override string ToString() => PointPair.ToString();
    }

    public class LineBorders : IEnumerable<ITrajectory>
    {
        public Vector3 Center { get; }
        public List<ITrajectory> Borders { get; }
        public bool IsEmpty => !Borders.Any();
        public LineBorders(MarkingRegularLine line)
        {
            Center = line.Marking.Position;
            Borders = GetBorders(line).ToList();
        }
        public IEnumerable<ITrajectory> GetBorders(MarkingRegularLine line)
        {
            if (line.ClipSidewalk)
                return line.Marking.Contour;
            else
                return Enumerable.Empty<ITrajectory>();
        }

        public IEnumerator<ITrajectory> GetEnumerator() => Borders.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public StraightTrajectory[] GetVertex(DecalData dash)
        {
            var dirX = dash.Angle.Direction();
            var dirY = dirX.Turn90(true);

            dirX *= (dash.Length / 2);
            dirY *= (dash.Width / 2);

            return new StraightTrajectory[]
            {
                new StraightTrajectory(Center, dash.position + dirX + dirY),
                new StraightTrajectory(Center, dash.position - dirX + dirY),
                new StraightTrajectory(Center, dash.position + dirX - dirY),
                new StraightTrajectory(Center, dash.position - dirX - dirY),
            };
        }
        public StraightTrajectory[] GetVertex(Vector3 pos, Vector3 dir, float length, float width)
        {
            var dirX = dir;
            var dirY = dirX.Turn90(true);

            dirX *= length * 0.5f;
            dirY *= width * 0.5f;

            return new StraightTrajectory[]
            {
                new StraightTrajectory(Center, pos + dirX + dirY),
                new StraightTrajectory(Center, pos - dirX + dirY),
                new StraightTrajectory(Center, pos + dirX - dirY),
                new StraightTrajectory(Center, pos - dirX - dirY),
            };
        }
    }

    public enum LineType
    {
        [Description(nameof(Localize.LineStyle_RegularLinesGroup))]
        Regular = Marking.Item.RegularLine,

        [Description(nameof(Localize.LineStyle_StopLinesGroup))]
        Stop = Marking.Item.StopLine,

        [Description(nameof(Localize.LineStyle_CrosswalkLinesGroup))]
        Crosswalk = Marking.Item.Crosswalk,

        [Description(nameof(Localize.LineStyle_LaneGroup))]
        Lane = Marking.Item.Lane,

        [NotVisible]
        All = Regular | Stop | Crosswalk | Lane,
    }


    [AttributeUsage(AttributeTargets.Field)]
    public class LineTypeAttribute : Attribute
    {
        public LineType Type { get; }

        public LineTypeAttribute(LineType type)
        {
            Type = type;
        }
    }

    public enum Alignment
    {
        [Description(nameof(Localize.StyleOption_AlignmentLeft))]
        Left,

        [Description(nameof(Localize.StyleOption_AlignmentCenter))]
        Centre,

        [Description(nameof(Localize.StyleOption_AlignmentRight))]
        Right
    }
}
