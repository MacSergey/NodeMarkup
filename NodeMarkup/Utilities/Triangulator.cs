using ModsCommon.Utilities;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static ColossalFramework.Math.VectorUtils;

namespace NodeMarkup.Utilities
{
    public class Triangulator
    {
        public static int[] Triangulate(IEnumerable<Vector3> points, TrajectoryHelper.Direction direction) => Triangulate(points.Select(p => XZ(p)), direction);
        public static int[] Triangulate(IEnumerable<Vector2> points, TrajectoryHelper.Direction direction)
        {
            var triangulator = new Triangulator(points, direction);
            return triangulator.Triangulate();
        }

        private TrajectoryHelper.Direction Direction { get; }
        private LinkedList<Vertex> Vertices { get; }
        private HashSet<LinkedListNode<Vertex>> Ears { get; } = new HashSet<LinkedListNode<Vertex>>();
        private List<Triangle> Triangles { get; } = new List<Triangle>();

        private Triangulator(IEnumerable<Vector2> points, TrajectoryHelper.Direction direction)
        {
            Vertices = new LinkedList<Vertex>(points.Select((p, i) => new Vertex(p, i)));
            Direction = direction;
        }
        private int[] Triangulate()
        {
            foreach (var vertex in EnumerateVertex())
            {
                SetConvex(vertex);
                SetEar(vertex);
            }

            while (true)
            {
                if (Ears.LastOrDefault() is not LinkedListNode<Vertex> vertex)
                    return null;

                var prev = vertex.GetPrevious();
                var next = vertex.GetNext();

                var triangle = new Triangle(next.Value.Index, vertex.Value.Index, prev.Value.Index);
                Triangles.Add(triangle);
                Ears.Remove(vertex);
                Vertices.Remove(vertex);

                if (Vertices.Count < 3)
                    return Triangles.SelectMany(t => t.GetVertices(Direction)).ToArray();

                SetConvex(prev);
                SetConvex(next);

                SetEar(prev);
                SetEar(next);
            }
        }

        private void SetConvex(LinkedListNode<Vertex> vertex)
        {
            if (!vertex.Value.IsConvex)
                vertex.Value.SetConvex(vertex.GetPrevious().Value, vertex.GetNext().Value, Direction);
        }

        private void SetEar(LinkedListNode<Vertex> vertex)
        {
            if (vertex.Value.IsConvex)
            {
                var prev = vertex.GetPrevious();
                var next = vertex.GetNext();
                if (!EnumerateVertex(next, prev).Any(p => PointInTriangle(prev.Value.Position, vertex.Value.Position, next.Value.Position, p.Value.Position)))
                {
                    Ears.Add(vertex);
                    return;
                }
            }

            Ears.Remove(vertex);
        }

        private IEnumerable<LinkedListNode<Vertex>> EnumerateVertex() => EnumerateVertex(Vertices.First);
        private IEnumerable<LinkedListNode<Vertex>> EnumerateVertex(LinkedListNode<Vertex> startFrom)
        {
            yield return startFrom;

            for (var vertex = startFrom.GetNext(); vertex != startFrom; vertex = vertex.GetNext())
                yield return vertex;
        }
        private IEnumerable<LinkedListNode<Vertex>> EnumerateVertex(LinkedListNode<Vertex> from, LinkedListNode<Vertex> to)
        {
            for (var vertex = from.GetNext(); vertex != to; vertex = vertex.GetNext())
                yield return vertex;
        }


        private bool PointInTriangle(Vector2 a, Vector2 b, Vector2 c, Vector2 p)
        {
            float area = 0.5f * (-b.y * c.x + a.y * (-b.x + c.x) + a.x * (b.y - c.y) + b.x * c.y);
            float s = 1 / (2 * area) * (a.y * c.x - a.x * c.y + (c.y - a.y) * p.x + (a.x - c.x) * p.y);
            float t = 1 / (2 * area) * (a.x * b.y - a.y * b.x + (a.y - b.y) * p.x + (b.x - a.x) * p.y);
            return s >= 0 && t >= 0 && (s + t) <= 1;

        }
    }

    internal class Vertex
    {
        public Vector2 Position { get; }
        public int Index { get; }
        public bool IsConvex { get; private set; }

        public Vertex(Vector2 position, int index)
        {
            Position = position;
            Index = index;
        }

        public void SetConvex(Vertex prev, Vertex next, TrajectoryHelper.Direction direction)
        {
            var a = Position - prev.Position;
            var b = next.Position - Position;

            var sign = (int)Mathf.Sign(a.x * b.y - a.y * b.x);
            IsConvex = sign >= 0 ^ direction == TrajectoryHelper.Direction.ClockWise;
        }

        public override string ToString() => $"{Index}:{Position} ({(IsConvex ? "Conver" : "Reflex")})";
    }

    internal class Triangle
    {
        private int A { get; }
        private int B { get; }
        private int C { get; }

        public Triangle(int a, int b, int c)
        {
            A = a;
            B = b;
            C = c;
        }

        public IEnumerable<int> GetVertices(TrajectoryHelper.Direction direction)
        {
            if (direction == TrajectoryHelper.Direction.ClockWise)
            {
                yield return C;
                yield return B;
                yield return A;
            }
            else
            {
                yield return A;
                yield return B;
                yield return C;
            }
        }

        public override string ToString() => $"{A}-{B}-{C}";
    }
}
