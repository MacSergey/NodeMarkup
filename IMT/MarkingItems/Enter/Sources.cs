using ModsCommon.Utilities;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace IMT.Manager
{
    public interface IPointSource
    {
        public Entrance Enter { get; }
        public MarkingPoint.LocationType Location { get; }
        public NetworkType NetworkType { get; }
        public void GetAbsolutePositionAndDirection(float offset, out Vector3 position, out Vector3 direction);
        public float GetRelativePosition(float offset);
    }
    public struct NetInfoPointSource : IPointSource
    {
        public Entrance Enter { get; }
        public DriveLane LeftLane { get; }
        public DriveLane RightLane { get; }
        public MarkingPoint.LocationType Location { get; private set; }
        public NetworkType NetworkType { get; private set; }

        public bool IsEdge => GetIsEdge(LeftLane, RightLane);
        public float CenterDelte => GetCenterDelte(LeftLane, RightLane);
        public float SideDelta => GetSideDelta(LeftLane, RightLane);

        public NetInfoPointSource(Entrance enter, DriveLane leftLane, DriveLane rightLane, MarkingPoint.LocationType location)
        {
            Enter = enter;
            LeftLane = leftLane;
            RightLane = rightLane;
            Location = location;
            NetworkType = (LeftLane == null ? NetworkType.None : leftLane.NetworkType) | (RightLane == null ? NetworkType.None : RightLane.NetworkType);
        }

        public void GetAbsolutePositionAndDirection(float offset, out Vector3 position, out Vector3 direction)
        {
            if ((Location & MarkingPoint.LocationType.Between) != MarkingPoint.LocationType.None)
                GetMiddlePositionAndDirection(offset, out position, out direction);

            else if ((Location & MarkingPoint.LocationType.Edge) != MarkingPoint.LocationType.None)
                GetEdgePositionAndDirection(Location, offset, out position, out direction);

            else
                throw new Exception();
        }

        private void GetMiddlePositionAndDirection(float offset, out Vector3 position, out Vector3 direction)
        {
            if (RightLane == LeftLane)
            {
                RightLane.LaneId.GetLane().CalculatePositionAndDirection(Enter.T, out position, out direction);
                direction = direction.normalized * Enter.SideSign;
            }
            else
            {
                RightLane.LaneId.GetLane().CalculatePositionAndDirection(Enter.T, out Vector3 rightPos, out Vector3 rightDir);
                LeftLane.LaneId.GetLane().CalculatePositionAndDirection(Enter.T, out Vector3 leftPos, out Vector3 leftDir);

                direction = ((rightDir + leftDir) / (Enter.SideSign * 2)).normalized;

                var part = (RightLane.HalfWidth + SideDelta / 2) / CenterDelte;
                position = Vector3.Lerp(rightPos, leftPos, part);
            }

            position += Enter.CornerDir * (offset / Enter.TranformCoef);
        }
        private void GetEdgePositionAndDirection(MarkingPoint.LocationType location, float offset, out Vector3 position, out Vector3 direction)
        {
            float lineShift;
            switch (location)
            {
                case MarkingPoint.LocationType.LeftEdge:
                    RightLane.LaneId.GetLane().CalculatePositionAndDirection(Enter.T, out position, out direction);
                    lineShift = -RightLane.HalfWidth;
                    break;
                case MarkingPoint.LocationType.RightEdge:
                    LeftLane.LaneId.GetLane().CalculatePositionAndDirection(Enter.T, out position, out direction);
                    lineShift = LeftLane.HalfWidth;
                    break;
                default:
                    throw new Exception();
            }
            direction = (direction * Enter.SideSign).normalized;

            var shift = (lineShift + offset) / Enter.TranformCoef;

            position += Enter.CornerDir * shift;
        }
        public float GetRelativePosition(float offset)
        {
            if ((Location & MarkingPoint.LocationType.Between) == MarkingPoint.LocationType.Between)
            {
                if (Enter.IsLaneInvert)
                    return (RightLane.Position + LeftLane.Position) * 0.5f + offset;
                else
                    return -(RightLane.Position + LeftLane.Position) * 0.5f + offset;
            }
            else if ((Location & MarkingPoint.LocationType.LeftEdge) == MarkingPoint.LocationType.LeftEdge)
            {
                if (Enter.IsLaneInvert)
                    return RightLane.Position - RightLane.HalfWidth + offset;
                else
                    return -RightLane.Position - RightLane.HalfWidth + offset;
            }
            else if ((Location & MarkingPoint.LocationType.RightEdge) == MarkingPoint.LocationType.RightEdge)
            {
                if (Enter.IsLaneInvert)
                    return LeftLane.Position + LeftLane.HalfWidth + offset;
                else
                    return -LeftLane.Position + LeftLane.HalfWidth + offset;
            }
            else
                throw new Exception();
        }

        public static IEnumerable<NetInfoPointSource> GetSource(Entrance enter, DriveLane leftLane, DriveLane rightLane)
        {
            if (GetIsEdge(leftLane, rightLane))
            {
                yield return new NetInfoPointSource(enter, leftLane, rightLane, rightLane == null ? MarkingPoint.LocationType.RightEdge : MarkingPoint.LocationType.LeftEdge);
            }
            else if (leftLane == rightLane)
            {
                yield return new NetInfoPointSource(enter, leftLane, rightLane, MarkingPoint.LocationType.Between);
            }
            else if (GetSideDelta(leftLane, rightLane) >= (leftLane.HalfWidth + rightLane.HalfWidth) / 2)
            {
                yield return new NetInfoPointSource(enter, leftLane, rightLane, MarkingPoint.LocationType.RightEdge);
                yield return new NetInfoPointSource(enter, leftLane, rightLane, MarkingPoint.LocationType.LeftEdge);
            }
            else
            {
                yield return new NetInfoPointSource(enter, leftLane, rightLane, MarkingPoint.LocationType.Between);
            }
        }
        public static bool GetIsEdge(DriveLane leftLane, DriveLane rightLane) => rightLane == null ^ leftLane == null;
        public static float GetSideDelta(DriveLane leftLane, DriveLane rightLane) => GetIsEdge(leftLane, rightLane) ? 0f : Mathf.Abs(rightLane.LeftSidePos - leftLane.RightSidePos);
        public static float GetCenterDelte(DriveLane leftLane, DriveLane rightLane) => GetIsEdge(leftLane, rightLane) ? 0f : Mathf.Abs(rightLane.Position - leftLane.Position);
    }
    public struct NetLanePointSource : IPointSource
    {
        public event Action OnPointOrderChanged
        {
            add => Enter.OnPointOrderChanged += value;
            remove => Enter.OnPointOrderChanged -= value;
        }

        public Entrance Enter { get; }
        public byte Index { get; }
        public MarkingPoint.LocationType Location => MarkingPoint.LocationType.Between;

        public NetworkType NetworkType
        {
            get
            {
                GetPoints(out var pointA, out var pointB);
                return pointA.NetworkType & pointB.NetworkType;
            }
        }

        public NetLanePointSource(Entrance enter, byte index)
        {
            Enter = enter;
            Index = index;
        }

        public void GetAbsolutePositionAndDirection(float offset, out Vector3 position, out Vector3 direction)
        {
            GetPoints(out var pointA, out var pointB);
            direction = (pointA.Direction + pointB.Direction) * 0.5f;
            position = (pointA.Position + pointB.Position) * 0.5f;
        }
        public float GetRelativePosition(float offset)
        {
            GetPoints(out var pointA, out var pointB);
            return (pointA.GetRelativePosition() + pointB.GetRelativePosition()) * 0.5f;
        }
        public void GetPoints(out MarkingEnterPoint enterPointA, out MarkingEnterPoint enterPointB)
        {
            Enter.TryGetSortedPoint(Index, MarkingPoint.PointType.Enter, out var pointA);
            Enter.TryGetSortedPoint((byte)(Index + 1), MarkingPoint.PointType.Enter, out var pointB);

            enterPointA = pointA as MarkingEnterPoint;
            enterPointB = pointB as MarkingEnterPoint;
        }
    }

    //public class RoadGeneratorPointSource : IPointSource
    //{
    //    public Enter Enter { get; }
    //    public float Position { get; }
    //    public float Height { get; }
    //    public MarkupPoint.LocationType Location => throw new NotImplementedException();
    //    public NetworkType NetworkType => throw new NotImplementedException();

    //    public RoadGeneratorPointSource(Enter enter, float position, float height = -0.3f)
    //    {
    //        Enter = enter;
    //        Position = position;
    //        Height = height;
    //    }

    //    public void GetPositionAndDirection(float offset, out Vector3 position, out Vector3 direction)
    //    {
    //        position = Enter.GetPosition(Position + offset) + new Vector3(0f, Height, 0f);
    //        direction = Enter.NormalDir;
    //    }
    //}

    public class DriveLane
    {
        private Entrance Enter { get; }

        public uint LaneId { get; }
        public int Index { get; }
        public NetworkType NetworkType { get; }

        public float Position { get; }
        public float HalfWidth { get; }
        public float LeftSidePos => Position + (Enter.IsLaneInvert ? -HalfWidth : HalfWidth);
        public float RightSidePos => Position + (Enter.IsLaneInvert ? HalfWidth : -HalfWidth);

        public DriveLane(Entrance enter, int index, uint laneId, NetInfo.Lane info, NetworkType type)
        {
            Enter = enter;
            Index = index;
            LaneId = laneId;
            Position = info.m_position;
            HalfWidth = Mathf.Abs(info.m_width) / 2;
            NetworkType = type;
        }


        public override string ToString() => LaneId.ToString();
    }
    [Flags]
    public enum NetworkType
    {
        None = 0,
        Road = 1 << 0,
        Track = 1 << 1,
        Taxiway = 1 << 2,
        Path = 1 << 3,

        All = Road | Track | Taxiway | Path,
    }
    [AttributeUsage(AttributeTargets.Field)]
    public class NetworkTypeAttribute : Attribute
    {
        public NetworkType Type { get; }

        public NetworkTypeAttribute(NetworkType type)
        {
            Type = type;
        }
    }
}
