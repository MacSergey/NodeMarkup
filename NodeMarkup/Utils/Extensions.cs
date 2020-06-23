using ColossalFramework;
using ColossalFramework.Math;
using ColossalFramework.PlatformServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NodeMarkup.Utils
{
    public static class Utilities
    {
        private static NetManager NetManager => Singleton<NetManager>.instance;
        private static RenderManager RenderManager => Singleton<RenderManager>.instance;
        public static IEnumerable<NetSegment> Segments(this NetNode node)
        {
            for (var i = 0; i < 8; i += 1)
            {
                var segment = node.GetSegment(i);
                if (segment != 0)
                    yield return GetSegment(segment);
            }
        }
        public static IEnumerable<ushort> SegmentsId(this NetNode node)
        {
            for (var i = 0; i < 8; i += 1)
            {
                var segment = node.GetSegment(i);
                if (segment != 0)
                    yield return segment;
            }
        }
        public static IEnumerable<NetLane> GetLanes(this NetSegment segment)
        {
            NetLane lane;
            for (var laneId = segment.m_lanes; laneId != 0; laneId = lane.m_nextLane)
            {
                lane = GetLane(laneId);
                yield return lane;
            }
        }
        public static IEnumerable<uint> GetLanesId(this NetSegment segment)
        {
            for (var laneId = segment.m_lanes; laneId != 0; laneId = GetLane(laneId).m_nextLane)
            {
                yield return laneId;
            }
        }

        public static bool IsInvert(this NetSegment segment) => (segment.m_flags & NetSegment.Flags.Invert) == NetSegment.Flags.Invert;
        public static bool IsDriveLane(this NetInfo.Lane info) => (info.m_vehicleType & VehicleInfo.VehicleType.Car) == VehicleInfo.VehicleType.Car && (info.m_laneType & NetInfo.LaneType.Parking) == NetInfo.LaneType.None;

        public static Vector3 Turn90(this Vector3 v, bool isClockWise) => isClockWise ? new Vector3(v.z, v.y, -v.x) : new Vector3(-v.z, v.y, v.x);
        public static Vector3 TurnDeg(this Vector3 vector, float turnAngle, bool isClockWise) => vector.Turn(turnAngle * Mathf.Deg2Rad, isClockWise);
        public static Vector3 Turn(this Vector3 vector, float turnAngle, bool isClockWise)
        {
            turnAngle = isClockWise ? -turnAngle : turnAngle;
            var newX = vector.x * Mathf.Cos(turnAngle) - vector.z * Mathf.Sin(turnAngle);
            var newZ = vector.x * Mathf.Sin(turnAngle) + vector.z * Mathf.Cos(turnAngle);
            return new Vector3(newX, vector.y, newZ);
        }

        public static NetNode GetNode(ushort nodeId) => NetManager.m_nodes.m_buffer[nodeId];
        public static NetSegment GetSegment(ushort segmentId) => NetManager.m_segments.m_buffer[segmentId];
        public static NetLane GetLane(uint laneId) => NetManager.m_lanes.m_buffer[laneId];

        public static float Length(this Bezier3 bezier, float minAngleDelta = 10)
        {
            var angle = Vector3.Angle(bezier.b - bezier.a, bezier.c - bezier.d);
            if (180 - angle > minAngleDelta)
            {
                bezier.Divide(out Bezier3 first, out Bezier3 second);
                var firstLength = first.Length();
                var secondLength = second.Length();
                return firstLength + secondLength;
            }
            else
            {
                var length = (bezier.d - bezier.a).magnitude;
                return length;
            }
        }
    }
}
