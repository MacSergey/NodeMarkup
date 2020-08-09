using ColossalFramework;
using ColossalFramework.Math;
using ColossalFramework.PlatformServices;
using ColossalFramework.UI;
using NodeMarkup.Manager;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using UnityEngine;

namespace NodeMarkup.Utils
{
    public static class XmlExtension
    {
        public static T GetAttrValue<T>(this XElement element, string attrName, T defaultValue = default, Func<T, bool> predicate = null) => Convert(element.Attribute(attrName)?.Value, defaultValue, predicate);
        public static object GetAttrValue(this XElement element, string attrName, Type type) => Convert(element.Attribute(attrName)?.Value, type);
        public static T GetValue<T>(this XElement element, T defaultValue = default, Func<T, bool> predicate = null) => Convert(element.Value, defaultValue, predicate);
        private static T Convert<T>(string str, T defaultValue = default, Func<T, bool> predicate = null)
        {
            try
            {
                var value = Convert(str, typeof(T));
                return value is T tValue && predicate?.Invoke(tValue) != false ? tValue : defaultValue;
            }
            catch
            {
                return defaultValue;
            }
        }
        private static object Convert(string str, Type type)
        {
            try
            {
                return TypeDescriptor.GetConverter(type).ConvertFromString(str);
            }
            catch
            {
                return null;
            }
        }
    }

    public static class Utilities
    {
        public static void OpenUrl(string url)
        {
            if (PlatformService.IsOverlayEnabled())
                PlatformService.ActivateGameOverlayToWebPage(url);
            else
                Process.Start(url);
        }

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

        public static VehicleInfo.VehicleType DriveType { get; } =
            VehicleInfo.VehicleType.Car |
            VehicleInfo.VehicleType.Bicycle |
            VehicleInfo.VehicleType.Tram |
            VehicleInfo.VehicleType.Trolleybus |
            VehicleInfo.VehicleType.Plane;
        public static bool IsDriveLane(this NetInfo.Lane info) => (info.m_vehicleType & DriveType) != VehicleInfo.VehicleType.None;

        public static Vector2 Turn90(this Vector2 v, bool isClockWise) => isClockWise ? new Vector2(v.y, -v.x) : new Vector2(-v.y, v.x);
        public static Vector3 Turn90(this Vector3 v, bool isClockWise) => isClockWise ? new Vector3(v.z, v.y, -v.x) : new Vector3(-v.z, v.y, v.x);
        public static Vector3 TurnDeg(this Vector3 vector, float turnAngle, bool isClockWise) => vector.TurnRad(turnAngle * Mathf.Deg2Rad, isClockWise);
        public static Vector3 TurnRad(this Vector3 vector, float turnAngle, bool isClockWise)
        {
            turnAngle = isClockWise ? -turnAngle : turnAngle;
            var newX = vector.x * Mathf.Cos(turnAngle) - vector.z * Mathf.Sin(turnAngle);
            var newZ = vector.x * Mathf.Sin(turnAngle) + vector.z * Mathf.Cos(turnAngle);
            return new Vector3(newX, vector.y, newZ);
        }

        public static NetNode GetNode(ushort nodeId) => NetManager.m_nodes.m_buffer[nodeId];
        public static NetSegment GetSegment(ushort segmentId) => NetManager.m_segments.m_buffer[segmentId];
        public static NetLane GetLane(uint laneId) => NetManager.m_lanes.m_buffer[laneId];

        public static float Length(this Bezier3 bezier, float minAngleDelta = 10, int depth = 0, int maxDepth = 5)
        {
            var start = bezier.b - bezier.a;
            var end = bezier.c - bezier.d;
            if (start.magnitude < Vector3.kEpsilon || end.magnitude < Vector3.kEpsilon)
                return 0;

            var angle = Vector3.Angle(start, end);
            if (depth < maxDepth && 180 - angle > minAngleDelta)
            {
                bezier.Divide(out Bezier3 first, out Bezier3 second);
                var firstLength = first.Length(depth: depth + 1, maxDepth: maxDepth);
                var secondLength = second.Length(depth: depth + 1, maxDepth: maxDepth);
                return firstLength + secondLength;
            }
            else
            {
                var length = (bezier.d - bezier.a).magnitude;
                return length;
            }
        }
        public static float Length(this Bezier3 bezier, out List<BezierPoint> bezierPoints, float minAngleDelta = 10, int depth = 0)
        {
            bezierPoints = new List<BezierPoint>();

            var start = bezier.b - bezier.a;
            var end = bezier.c - bezier.d;
            if (start.magnitude < Vector3.kEpsilon || end.magnitude < Vector3.kEpsilon)
                return 0;

            var angle = Vector3.Angle(start, end);
            if (depth < 5 && 180 - angle > minAngleDelta)
            {
                bezier.Divide(out Bezier3 first, out Bezier3 second);
                var firstLength = first.Length(out List<BezierPoint> firstPoints, depth: depth + 1);
                var secondLength = second.Length(out List<BezierPoint> secondPoints, depth: depth + 1);
                var length = firstLength + secondLength;
                if (length == 0)
                    return 0;

                var firstPart = firstLength / length;
                var secondPart = secondLength / length;

                foreach (var point in firstPoints)
                {
                    bezierPoints.Add(new BezierPoint(point.T * firstPart, point.Length));
                }
                foreach (var point in secondPoints.Skip(1))
                {
                    bezierPoints.Add(new BezierPoint(point.T * secondPart + firstPart, point.Length + firstLength));
                }
                return length;
            }
            else
            {
                var length = (bezier.d - bezier.a).magnitude;
                bezierPoints.Add(new BezierPoint(0, 0));
                bezierPoints.Add(new BezierPoint(1, length));
                return length;
            }
        }
        public static Vector2 XZ(this Vector3 vector) => VectorUtils.XZ(vector);
        public static float AbsoluteAngle(this Vector3 vector) => Mathf.Atan2(vector.z, vector.x);
        public static Vector4 ToX3Vector(this Color c) => new Vector4(ColorChange(c.r), ColorChange(c.g), ColorChange(c.b), Mathf.Pow(c.a, 2));
        static float ColorChange(float c) => Mathf.Pow(c, 4);

        public static float DeltaAngle(this Bezier3 bezier) => 180 - Vector3.Angle(bezier.b - bezier.a, bezier.c - bezier.d);

        public static int ToInt(this Color32 color) => (color.r << 24) + (color.g << 16) + (color.b << 8) + color.a;
        public static Color32 ToColor(this int color) => new Color32((byte)(color >> 24), (byte)(color >> 16), (byte)(color >> 8), (byte)color);

        public static Style.StyleType GetSimpleStyle(this Event e) =>
            NodeMarkupTool.ShiftIsPressed ? 
            (NodeMarkupTool.CtrlIsPressed ? Style.StyleType.LineDoubleSolid : Style.StyleType.LineSolid) : 
            (NodeMarkupTool.CtrlIsPressed ? Style.StyleType.LineDoubleDashed : Style.StyleType.LineDashed);
        public static Style.StyleType GetStopStyle(this Event e) => NodeMarkupTool.ShiftIsPressed ? Style.StyleType.StopLineDashed : Style.StyleType.StopLineSolid;
        public static Style.StyleType GetCrosswalkStyle(this Event e) => Style.StyleType.CrosswalkZebra;
    }

    public struct BezierPoint
    {
        public float Length;
        public float T;
        public BezierPoint(float t, float length)
        {
            T = t;
            Length = length;
        }
        public override string ToString() => $"{T} - {Length}";
    }
    public class BezierPointComparer : IComparer<BezierPoint>
    {
        public static BezierPointComparer Instance { get; } = new BezierPointComparer();
        public int Compare(BezierPoint x, BezierPoint y) => x.Length.CompareTo(y.Length);
    }
}
