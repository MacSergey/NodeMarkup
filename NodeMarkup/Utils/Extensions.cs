using ColossalFramework;
using ColossalFramework.Globalization;
using ColossalFramework.Math;
using ColossalFramework.PlatformServices;
using ColossalFramework.UI;
using ICities;
using NodeMarkup.Manager;
using NodeMarkup.Tools;
using NodeMarkup.UI;
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
                if (type == typeof(string))
                    return str;
                else if (string.IsNullOrEmpty(str))
                    return null;
                else
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
        public static AttrType GetAttr<AttrType, T>(this T value) where T : Enum where AttrType : Attribute
            => typeof(T).GetField(value.ToString()).GetCustomAttributes(typeof(AttrType), false).OfType<AttrType>().FirstOrDefault();
        public static IEnumerable<Type> GetEnumValues<Type>() where Type : Enum => Enum.GetValues(typeof(Type)).OfType<Type>();
        public static string Description<T>(this T value)
            where T : Enum
        {
            var description = value.GetAttr<DescriptionAttribute, T>()?.Description ?? value.ToString();
            return Localize.ResourceManager.GetString(description, Localize.Culture);
        }
        public static bool IsVisible<T>(this T value) where T : Enum => value.GetAttr<NotVisibleAttribute, T>() == null;

        public static string Description(this StyleModifier modifier)
        {
            var localeID = "KEYNAME";

            if (modifier.GetAttr<DescriptionAttribute, StyleModifier>() is DescriptionAttribute description)
                return Localize.ResourceManager.GetString(description.Description, Localize.Culture);
            else if (modifier.GetAttr<InputKeyAttribute, StyleModifier>() is InputKeyAttribute inputKey)
            {
                var modifierStrings = new List<string>();
                if (inputKey.Control)
                    modifierStrings.Add(Locale.Get(localeID, KeyCode.LeftControl.ToString()));
                if (inputKey.Shift)
                    modifierStrings.Add(Locale.Get(localeID, KeyCode.LeftShift.ToString()));
                if (inputKey.Alt)
                    modifierStrings.Add(Locale.Get(localeID, KeyCode.LeftAlt.ToString()));
                return string.Join("+", modifierStrings.ToArray());
            }
            else
                return modifier.ToString();
        }

        public static NetManager NetManager => Singleton<NetManager>.instance;
        public static PropManager PropManager => Singleton<PropManager>.instance;
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
        public static float DeltaAngle(this Bezier3 bezier) => 180 - Vector3.Angle(bezier.b - bezier.a, bezier.c - bezier.d);
        public static Vector3 Direction(this float absoluteAngle) => Vector3.right.TurnRad(absoluteAngle, false).normalized;

        public static Bezier3 GetBezier(this Line3 line)
        {
            var bezier = new Bezier3
            {
                a = line.a,
                d = line.b,
            };
            var dir = line.b - line.a;
            NetSegment.CalculateMiddlePoints(bezier.a, dir, bezier.d, -dir, true, true, out bezier.b, out bezier.c);

            return bezier;
        }
        public static void AddRange<T>(this HashSet<T> hashSet, IEnumerable<T> values)
        {
            foreach (var value in values)
                hashSet.Add(value);
        }
        public static Version Build(this Version version) => new Version(version.Major, version.Minor, version.Build);
        public static Version Minor(this Version version) => new Version(version.Major, version.Minor);
        public static Version PrevMinor(this Version version)
        {
            var build = version.Build();
            var isMinor = build.IsMinor();
            var toFind = build.Minor();
            var index = Mod.Versions.FindIndex(v => v == toFind);
            if (index != -1 && Mod.Versions.Skip(index + 1).FirstOrDefault(v => isMinor || v.IsMinor()) is Version minor)
                return minor;
            else
                return Mod.Versions.Last();
        }
        public static bool IsMinor(this Version version) => version.Build <= 0 && version.Revision <= 0;
        public static string GetString(this Version version)
        {
            if (version.Revision > 0)
                return version.ToString(4);
            else if (version.Build > 0)
                return version.ToString(3);
            else
                return version.ToString(2);
        }

        public static UIHelperBase AddGroup(this UIHelperBase helper)
        {
            var newGroup = helper.AddGroup("aaa") as UIHelper;
            var panel = newGroup.self as UIPanel;
            if (panel.parent.Find<UILabel>("Label") is UILabel label)
                label.isVisible = false;
            return newGroup;
        }

        public static int NextIndex(this int i, int count, int shift = 1) => (i + shift) % count;
        public static int PrevIndex(this int i, int count, int shift = 1) => shift > i ? i + count - (shift % count) : i - shift;

        public static LineStyle.StyleAlignment Invert(this LineStyle.StyleAlignment alignment) => (LineStyle.StyleAlignment)(1 - ((int)alignment - 1));

        public static float Magnitude(this Bounds bounds) => bounds.size.magnitude / Mathf.Sqrt(3);
        public static void Render(this Bounds bounds, RenderManager.CameraInfo cameraInfo, Color? color = null, float? width = null, bool? alphaBlend = null)
            => NodeMarkupTool.RenderCircle(cameraInfo, bounds.center, color, width ?? bounds.Magnitude(), alphaBlend);

        public static void SetAvailable(this UIComponent component, bool value)
        {
            component.isEnabled = value;
            component.opacity = value ? 1f : 0.15f;
        }
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

    public class NotVisibleAttribute : Attribute { }
}
