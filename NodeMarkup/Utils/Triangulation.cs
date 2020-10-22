using ColossalFramework.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NodeMarkup.Utils
{
    //public class Triangulation
    //{
    //    public Vector3[] Points { get; }
    //    public Triangle[] Triangles { get; private set; } //треугольники, на которые разбит наш многоугольник
    //    private bool[] Taken { get; } //была ли рассмотрена i-ая вершина многоугольника

    //    public Triangulation(Vector3[] points) //points - х и y координаты
    //    {
    //        Points = points;
    //        Triangles = new Triangle[Points.Length - 2];
    //        Taken = new bool[Points.Length];

    //        Triangulate(); //триангуляция
    //    }

    //    private void Triangulate() //триангуляция
    //    {
    //        int trainPos = 0; //
    //        int leftPoints = Points.Length; //сколько осталось рассмотреть вершин

    //        //текущие вершины рассматриваемого треугольника
    //        int ai = FindNextNotTaken(0);
    //        int bi = FindNextNotTaken(ai + 1);
    //        int ci = FindNextNotTaken(bi + 1);

    //        int count = 0; //количество шагов

    //        while (leftPoints > 3) //пока не остался один треугольник
    //        {
    //            if (IsLeft(Points[ai], Points[bi], Points[ci]) && CanBuildTriangle(ai, bi, ci)) //если можно построить треугольник
    //            {
    //                Triangles[trainPos++] = new Triangle(ai, bi, ci); //новый треугольник
    //                Taken[bi] = true; //исключаем вершину b
    //                leftPoints--;
    //                bi = ci;
    //                ci = FindNextNotTaken(ci + 1); //берем следующую вершину
    //            }
    //            else
    //            { //берем следующие три вершины
    //                ai = FindNextNotTaken(ai + 1);
    //                bi = FindNextNotTaken(ai + 1);
    //                ci = FindNextNotTaken(bi + 1);
    //            }

    //            if (count > Points.Length * Points.Length)
    //            { //если по какой-либо причине (например, многоугольник задан по часовой стрелке) триангуляцию провести невозможно, выходим
    //                Triangles = null;
    //                break;
    //            }

    //            count++;
    //        }

    //        if (Triangles != null) //если триангуляция была проведена успешно
    //            Triangles[trainPos] = new Triangle(ai, bi, ci);
    //    }

    //    private int FindNextNotTaken(int startPos) //найти следущую нерассмотренную вершину
    //    {
    //        startPos %= Points.Length;
    //        if (!Taken[startPos])
    //            return startPos;

    //        int i = (startPos + 1) % Points.Length;
    //        while (i != startPos)
    //        {
    //            if (!Taken[i])
    //                return i;
    //            i = (i + 1) % Points.Length;
    //        }

    //        return -1;
    //    }

    //    private bool IsLeft(Vector3 a, Vector3 b, Vector3 c) //левая ли тройка векторов
    //    {
    //        float abX = b.x - a.x;
    //        float abY = b.z - a.z;
    //        float acX = c.x - a.x;
    //        float acY = c.z - a.z;

    //        return abX * acY - acX * abY < 0;
    //    }

    //    private bool IsPointInside(Vector3 a, Vector3 b, Vector3 c, Vector3 p) //находится ли точка p внутри треугольника abc
    //    {
    //        float ab = (a.x - p.x) * (b.z - a.z) - (b.x - a.x) * (a.z - p.z);
    //        float bc = (b.x - p.x) * (c.z - b.z) - (c.x - b.x) * (b.z - p.z);
    //        float ca = (c.x - p.x) * (a.z - c.z) - (a.x - c.x) * (c.z - p.z);

    //        return (ab >= 0 && bc >= 0 && ca >= 0) || (ab <= 0 && bc <= 0 && ca <= 0);
    //    }

    //    private bool CanBuildTriangle(int ai, int bi, int ci) //false - если внутри есть вершина
    //    {
    //        for (int i = 0; i < Points.Length; i++) //рассмотрим все вершины многоугольника
    //            if (i != ai && i != bi && i != ci) //кроме троих вершин текущего треугольника
    //                if (IsPointInside(Points[ai], Points[bi], Points[ci], Points[i]))
    //                    return false;
    //        return true;
    //    }
    //}
    //public class Triangle
    //{
    //    public int A { get; }
    //    public int B { get; }
    //    public int C { get; }

    //    public Triangle(int a, int b, int c)
    //    {
    //        A = a;
    //        B = b;
    //        C = c;
    //    }
    //}


    //public static class Triangulator
    //{
    //    #region Fields

    //    static readonly IndexableCyclicalLinkedList<Vertex> polygonVertices = new IndexableCyclicalLinkedList<Vertex>();
    //    static readonly IndexableCyclicalLinkedList<Vertex> earVertices = new IndexableCyclicalLinkedList<Vertex>();
    //    static readonly CyclicalList<Vertex> convexVertices = new CyclicalList<Vertex>();
    //    static readonly CyclicalList<Vertex> reflexVertices = new CyclicalList<Vertex>();

    //    #endregion

    //    #region Public Methods

    //    #region Triangulate

    //    /// <summary>
    //    /// Triangulates a 2D polygon produced the indexes required to render the points as a triangle list.
    //    /// </summary>
    //    /// <param name="inputVertices">The polygon vertices in counter-clockwise winding order.</param>
    //    /// <param name="desiredWindingOrder">The desired output winding order.</param>
    //    /// <param name="outputVertices">The resulting vertices that include any reversals of winding order and holes.</param>
    //    /// <param name="indices">The resulting indices for rendering the shape as a triangle list.</param>
    //    public static void Triangulate(Vector3[] inputVertices, WindingOrder desiredWindingOrder, out Vector3[] outputVertices, out int[] indices)
    //    {
    //        List<Triangle> triangles = new List<Triangle>();

    //        //make sure we have our vertices wound properly
    //        if (DetermineWindingOrder(inputVertices) == WindingOrder.Clockwise)
    //            outputVertices = ReverseWindingOrder(inputVertices);
    //        else
    //            outputVertices = (Vector3[])inputVertices.Clone();

    //        //clear all of the lists
    //        polygonVertices.Clear();
    //        earVertices.Clear();
    //        convexVertices.Clear();
    //        reflexVertices.Clear();

    //        //generate the cyclical list of vertices in the polygon
    //        for (int i = 0; i < outputVertices.Length; i++)
    //            polygonVertices.AddLast(new Vertex(outputVertices[i], i));

    //        //categorize all of the vertices as convex, reflex, and ear
    //        FindConvexAndReflexVertices();
    //        FindEarVertices();

    //        //clip all the ear vertices
    //        while (polygonVertices.Count > 3 && earVertices.Count > 0)
    //            ClipNextEar(triangles);

    //        //if there are still three points, use that for the last triangle
    //        if (polygonVertices.Count == 3)
    //            triangles.Add(new Triangle(polygonVertices[0].Value, polygonVertices[1].Value, polygonVertices[2].Value));

    //        //add all of the triangle indices to the output array
    //        indices = new int[triangles.Count * 3];

    //        //move the if statement out of the loop to prevent all the
    //        //redundant comparisons
    //        if (desiredWindingOrder == WindingOrder.CounterClockwise)
    //        {
    //            for (int i = 0; i < triangles.Count; i++)
    //            {
    //                indices[(i * 3)] = triangles[i].A.Index;
    //                indices[(i * 3) + 1] = triangles[i].B.Index;
    //                indices[(i * 3) + 2] = triangles[i].C.Index;
    //            }
    //        }
    //        else
    //        {
    //            for (int i = 0; i < triangles.Count; i++)
    //            {
    //                indices[(i * 3)] = triangles[i].C.Index;
    //                indices[(i * 3) + 1] = triangles[i].B.Index;
    //                indices[(i * 3) + 2] = triangles[i].A.Index;
    //            }
    //        }
    //    }

    //    #endregion

    //    #region CutHoleInShape

    //    /// <summary>
    //    /// Cuts a hole into a shape.
    //    /// </summary>
    //    /// <param name="shapeVerts">An array of vertices for the primary shape.</param>
    //    /// <param name="holeVerts">An array of vertices for the hole to be cut. It is assumed that these vertices lie completely within the shape verts.</param>
    //    /// <returns>The new array of vertices that can be passed to Triangulate to properly triangulate the shape with the hole.</returns>
    //    public static Vector3[] CutHoleInShape(Vector3[] shapeVerts, Vector3[] holeVerts)
    //    {
    //        //make sure the shape vertices are wound counter clockwise and the hole vertices clockwise
    //        shapeVerts = EnsureWindingOrder(shapeVerts, WindingOrder.CounterClockwise);
    //        holeVerts = EnsureWindingOrder(holeVerts, WindingOrder.Clockwise);

    //        //clear all of the lists
    //        polygonVertices.Clear();
    //        earVertices.Clear();
    //        convexVertices.Clear();
    //        reflexVertices.Clear();

    //        //generate the cyclical list of vertices in the polygon
    //        for (int i = 0; i < shapeVerts.Length; i++)
    //            polygonVertices.AddLast(new Vertex(shapeVerts[i], i));

    //        var holePolygon = new CyclicalList<Vertex>();
    //        for (int i = 0; i < holeVerts.Length; i++)
    //            holePolygon.Add(new Vertex(holeVerts[i], i + polygonVertices.Count));

    //        FindConvexAndReflexVertices();
    //        FindEarVertices();

    //        //find the hole vertex with the largest X value
    //        var rightMostHoleVertex = holePolygon[0];
    //        foreach (var v in holePolygon)
    //            if (v.Position.x > rightMostHoleVertex.Position.x)
    //                rightMostHoleVertex = v;

    //        //construct a list of all line segments where at least one vertex
    //        //is to the right of the rightmost hole vertex with one vertex
    //        //above the hole vertex and one below
    //        var segmentsToTest = new List<LineSegment>();
    //        for (int i = 0; i < polygonVertices.Count; i++)
    //        {
    //            var a = polygonVertices[i].Value;
    //            var b = polygonVertices[i + 1].Value;

    //            if ((a.Position.x > rightMostHoleVertex.Position.x || b.Position.x > rightMostHoleVertex.Position.x) &&
    //                ((a.Position.z >= rightMostHoleVertex.Position.z && b.Position.z <= rightMostHoleVertex.Position.z) ||
    //                (a.Position.z <= rightMostHoleVertex.Position.z && b.Position.z >= rightMostHoleVertex.Position.z)))
    //                segmentsToTest.Add(new LineSegment(a, b));
    //        }

    //        //now we try to find the closest intersection point heading to the right from
    //        //our hole vertex.
    //        float? closestPoint = null;
    //        var closestSegment = new LineSegment();
    //        foreach (var segment in segmentsToTest)
    //        {
    //            float? intersection = segment.IntersectsWithRay(rightMostHoleVertex.Position, Vector3.left);
    //            if (intersection != null)
    //            {
    //                if (closestPoint == null || closestPoint.Value > intersection.Value)
    //                {
    //                    closestPoint = intersection;
    //                    closestSegment = segment;
    //                }
    //            }
    //        }

    //        //if closestPoint is null, there were no collisions (likely from improper input data),
    //        //but we'll just return without doing anything else
    //        if (closestPoint == null)
    //            return shapeVerts;

    //        //otherwise we can find our mutually visible vertex to split the polygon
    //        Vector2 I = rightMostHoleVertex.Position + Vector3.left * closestPoint.Value;
    //        Vertex P = (closestSegment.A.Position.x > closestSegment.B.Position.x) ? closestSegment.A : closestSegment.B;

    //        //construct triangle MIP
    //        Triangle mip = new Triangle(rightMostHoleVertex, new Vertex(I, 1), P);

    //        //see if any of the reflex vertices lie inside of the MIP triangle
    //        var interiorReflexVertices = new List<Vertex>();
    //        foreach (var v in reflexVertices)
    //            if (mip.ContainsPoint(v))
    //                interiorReflexVertices.Add(v);

    //        //if there are any interior reflex vertices, find the one that, when connected
    //        //to our rightMostHoleVertex, forms the line closest to Vector2.UnitX
    //        if (interiorReflexVertices.Count > 0)
    //        {
    //            float closestDot = -1f;
    //            foreach (var v in interiorReflexVertices)
    //            {
    //                //compute the dot product of the vector against the UnitX
    //                Vector3 d = (v.Position.XZ() - rightMostHoleVertex.Position.XZ()).normalized;
    //                float dot = Vector2.Dot(Vector2.left, d);

    //                //if this line is the closest we've found
    //                if (dot > closestDot)
    //                {
    //                    //save the value and save the vertex as P
    //                    closestDot = dot;
    //                    P = v;
    //                }
    //            }
    //        }

    //        //now we just form our output array by injecting the hole vertices into place
    //        //we know we have to inject the hole into the main array after point P going from
    //        //rightMostHoleVertex around and then back to P.
    //        int mIndex = holePolygon.IndexOf(rightMostHoleVertex);
    //        int injectPoint = polygonVertices.IndexOf(P);

    //        for (int i = mIndex; i <= mIndex + holePolygon.Count; i++)
    //            polygonVertices.AddAfter(polygonVertices[injectPoint++], holePolygon[i]);

    //        polygonVertices.AddAfter(polygonVertices[injectPoint], P);


    //        //finally we write out the new polygon vertices and return them out
    //        var newShapeVerts = new Vector3[polygonVertices.Count];
    //        for (int i = 0; i < polygonVertices.Count; i++)
    //            newShapeVerts[i] = polygonVertices[i].Value.Position;

    //        return newShapeVerts;
    //    }

    //    #endregion

    //    #region EnsureWindingOrder

    //    /// <summary>
    //    /// Ensures that a set of vertices are wound in a particular order, reversing them if necessary.
    //    /// </summary>
    //    /// <param name="vertices">The vertices of the polygon.</param>
    //    /// <param name="windingOrder">The desired winding order.</param>
    //    /// <returns>A new set of vertices if the winding order didn't match; otherwise the original set.</returns>
    //    public static Vector3[] EnsureWindingOrder(Vector3[] vertices, WindingOrder windingOrder)
    //        => DetermineWindingOrder(vertices) != windingOrder ? ReverseWindingOrder(vertices) : vertices;

    //    #endregion

    //    #region ReverseWindingOrder

    //    /// <summary>
    //    /// Reverses the winding order for a set of vertices.
    //    /// </summary>
    //    /// <param name="vertices">The vertices of the polygon.</param>
    //    /// <returns>The new vertices for the polygon with the opposite winding order.</returns>
    //    public static Vector3[] ReverseWindingOrder(Vector3[] vertices)
    //    {
    //        var newVerts = new Vector3[vertices.Length];

    //        newVerts[0] = vertices[0];
    //        for (int i = 1; i < newVerts.Length; i++)
    //            newVerts[i] = vertices[vertices.Length - i];

    //        return newVerts;
    //    }

    //    #endregion

    //    #region DetermineWindingOrder

    //    /// <summary>
    //    /// Determines the winding order of a polygon given a set of vertices.
    //    /// </summary>
    //    /// <param name="vertices">The vertices of the polygon.</param>
    //    /// <returns>The calculated winding order of the polygon.</returns>
    //    public static WindingOrder DetermineWindingOrder(Vector3[] vertices)
    //    {
    //        int clockWiseCount = 0;
    //        int counterClockWiseCount = 0;
    //        var p1 = vertices[0];

    //        for (int i = 1; i < vertices.Length; i++)
    //        {
    //            var p2 = vertices[i];
    //            var p3 = vertices[(i + 1) % vertices.Length];

    //            var e1 = p1 - p2;
    //            var e2 = p3 - p2;

    //            if (e1.x * e2.z - e1.z * e2.x >= 0)
    //                clockWiseCount++;
    //            else
    //                counterClockWiseCount++;

    //            p1 = p2;
    //        }

    //        return (clockWiseCount > counterClockWiseCount) ? WindingOrder.Clockwise : WindingOrder.CounterClockwise;
    //    }

    //    #endregion

    //    #endregion

    //    #region Private Methods

    //    #region ClipNextEar

    //    private static void ClipNextEar(ICollection<Triangle> triangles)
    //    {
    //        //find the triangle
    //        var ear = earVertices[0].Value;
    //        var prev = polygonVertices[polygonVertices.IndexOf(ear) - 1].Value;
    //        var next = polygonVertices[polygonVertices.IndexOf(ear) + 1].Value;
    //        triangles.Add(new Triangle(ear, next, prev));

    //        //remove the ear from the shape
    //        earVertices.RemoveAt(0);
    //        polygonVertices.RemoveAt(polygonVertices.IndexOf(ear));

    //        //validate the neighboring vertices
    //        ValidateAdjacentVertex(prev);
    //        ValidateAdjacentVertex(next);
    //    }

    //    #endregion

    //    #region ValidateAdjacentVertex

    //    private static void ValidateAdjacentVertex(Vertex vertex)
    //    {

    //        if (reflexVertices.Contains(vertex))
    //        {
    //            if (IsConvex(vertex))
    //            {
    //                reflexVertices.Remove(vertex);
    //                convexVertices.Add(vertex);
    //            }
    //        }

    //        if (convexVertices.Contains(vertex))
    //        {
    //            bool wasEar = earVertices.Contains(vertex);
    //            bool isEar = IsEar(vertex);

    //            if (wasEar && !isEar)
    //                earVertices.Remove(vertex);
    //            else if (!wasEar && isEar)
    //                earVertices.AddFirst(vertex);
    //        }
    //    }

    //    #endregion

    //    #region FindConvexAndReflexVertices

    //    private static void FindConvexAndReflexVertices()
    //    {
    //        for (int i = 0; i < polygonVertices.Count; i++)
    //        {
    //            Vertex v = polygonVertices[i].Value;

    //            if (IsConvex(v))
    //                convexVertices.Add(v);
    //            else
    //                reflexVertices.Add(v);
    //        }
    //    }

    //    #endregion

    //    #region FindEarVertices

    //    private static void FindEarVertices()
    //    {
    //        for (int i = 0; i < convexVertices.Count; i++)
    //        {
    //            var c = convexVertices[i];

    //            if (IsEar(c))
    //                earVertices.AddLast(c);
    //        }
    //    }

    //    #endregion

    //    #region IsEar

    //    private static bool IsEar(Vertex c)
    //    {
    //        var p = polygonVertices[polygonVertices.IndexOf(c) - 1].Value;
    //        var n = polygonVertices[polygonVertices.IndexOf(c) + 1].Value;

    //        foreach (var t in reflexVertices)
    //        {
    //            if (t.Equals(p) || t.Equals(c) || t.Equals(n))
    //                continue;

    //            if (Triangle.ContainsPoint(p, c, n, t))
    //                return false;
    //        }

    //        return true;
    //    }

    //    #endregion


    //    private static bool IsConvex(Vertex c)
    //    {
    //        var p = polygonVertices[polygonVertices.IndexOf(c) - 1].Value;
    //        var n = polygonVertices[polygonVertices.IndexOf(c) + 1].Value;

    //        Vector2 d1 = (c.Position.XZ() - p.Position.XZ()).normalized;
    //        Vector2 d2 = (n.Position.XZ() - n.Position.XZ()).normalized;
    //        Vector2 n2 = d2.Turn90(false);

    //        return Vector2.Dot(d1, n2) <= 0f;
    //    }
    //    private static bool IsReflex(Vertex c) => !IsConvex(c);

    //    #endregion
    //}
    //public enum WindingOrder
    //{
    //    Clockwise,
    //    CounterClockwise
    //}

    //struct Vertex
    //{
    //    public readonly Vector3 Position;
    //    public readonly int Index;

    //    public Vertex(Vector3 position, int index)
    //    {
    //        Position = position;
    //        Index = index;
    //    }

    //    public override bool Equals(object obj) => obj is Vertex vertex && Equals(vertex);
    //    public bool Equals(Vertex obj) => obj.Position.Equals(Position) && obj.Index == Index;

    //    public override int GetHashCode()
    //    {
    //        unchecked
    //        {
    //            return (Position.GetHashCode() * 397) ^ Index;
    //        }
    //    }
    //}
    //struct Triangle
    //{
    //    public readonly Vertex A;
    //    public readonly Vertex B;
    //    public readonly Vertex C;

    //    public Triangle(Vertex a, Vertex b, Vertex c)
    //    {
    //        A = a;
    //        B = b;
    //        C = c;
    //    }

    //    public bool ContainsPoint(Vertex point)
    //    {
    //        //return true if the point to test is one of the vertices
    //        if (point.Equals(A) || point.Equals(B) || point.Equals(C))
    //            return true;

    //        bool oddNodes = false;

    //        if (CheckPointToSegment(C, A, point))
    //            oddNodes = !oddNodes;
    //        if (CheckPointToSegment(A, B, point))
    //            oddNodes = !oddNodes;
    //        if (CheckPointToSegment(B, C, point))
    //            oddNodes = !oddNodes;

    //        return oddNodes;
    //    }

    //    public static bool ContainsPoint(Vertex a, Vertex b, Vertex c, Vertex point) => new Triangle(a, b, c).ContainsPoint(point);

    //    static bool CheckPointToSegment(Vertex sA, Vertex sB, Vertex point)
    //    {
    //        if ((sA.Position.z < point.Position.z && sB.Position.z >= point.Position.z) ||
    //            (sB.Position.z < point.Position.z && sA.Position.z >= point.Position.z))
    //        {
    //            float x = sA.Position.x + (point.Position.z - sA.Position.z) / (sB.Position.z - sA.Position.z) * (sB.Position.x - sA.Position.x);

    //            if (x < point.Position.x)
    //                return true;
    //        }

    //        return false;
    //    }

    //    public override bool Equals(object obj) => obj is Triangle triangle && Equals(triangle);
    //    public bool Equals(Triangle obj) => obj.A.Equals(A) && obj.B.Equals(B) && obj.C.Equals(C);

    //    public override int GetHashCode()
    //    {
    //        unchecked
    //        {
    //            int result = A.GetHashCode();
    //            result = (result * 397) ^ B.GetHashCode();
    //            result = (result * 397) ^ C.GetHashCode();
    //            return result;
    //        }
    //    }
    //}
    //struct LineSegment
    //{
    //    public Vertex A;
    //    public Vertex B;

    //    public LineSegment(Vertex a, Vertex b)
    //    {
    //        A = a;
    //        B = b;
    //    }

    //    public float? IntersectsWithRay(Vector3 origin, Vector3 direction)
    //    {
    //        float largestDistance = Mathf.Max(A.Position.x - origin.x, B.Position.x - origin.x) * 2f;
    //        LineSegment raySegment = new LineSegment(new Vertex(origin, 0), new Vertex(origin + (direction * largestDistance), 0));

    //        Vector3? intersection = FindIntersection(this, raySegment);
    //        float? value = null;

    //        if (intersection != null)
    //            value = Vector3.Distance(origin, intersection.Value);

    //        return value;
    //    }

    //    public static Vector3? FindIntersection(LineSegment a, LineSegment b)
    //    {
    //        float x1 = a.A.Position.x;
    //        float z1 = a.A.Position.z;
    //        float x2 = a.B.Position.x;
    //        float z2 = a.B.Position.z;
    //        float x3 = b.A.Position.x;
    //        float z3 = b.A.Position.z;
    //        float x4 = b.B.Position.x;
    //        float z4 = b.B.Position.z;

    //        float denom = (z4 - z3) * (x2 - x1) - (x4 - x3) * (z2 - z1);

    //        float uaNum = (x4 - x3) * (z1 - z3) - (z4 - z3) * (x1 - x3);
    //        float ubNum = (x2 - x1) * (z1 - z3) - (z2 - z1) * (x1 - x3);

    //        float ua = uaNum / denom;
    //        float ub = ubNum / denom;

    //        if (Mathf.Clamp(ua, 0f, 1f) != ua || Mathf.Clamp(ub, 0f, 1f) != ub)
    //            return null;

    //        return a.A.Position + (a.B.Position - a.A.Position) * ua;
    //    }
    //}


    //class CyclicalList<T> : List<T>
    //{
    //    public new T this[int index]
    //    {
    //        get
    //        {
    //            //perform the index wrapping
    //            while (index < 0)
    //                index = Count + index;
    //            if (index >= Count)
    //                index %= Count;

    //            return base[index];
    //        }
    //        set
    //        {
    //            //perform the index wrapping
    //            while (index < 0)
    //                index = Count + index;
    //            if (index >= Count)
    //                index %= Count;

    //            base[index] = value;
    //        }
    //    }

    //    public CyclicalList() { }

    //    public CyclicalList(IEnumerable<T> collection)
    //        : base(collection)
    //    {
    //    }

    //    public new void RemoveAt(int index)
    //    {
    //        Remove(this[index]);
    //    }
    //}
    //class IndexableCyclicalLinkedList<T> : LinkedList<T>
    //{
    //    /// <summary>
    //    /// Gets the LinkedListNode at a particular index.
    //    /// </summary>
    //    /// <param name="index">The index of the node to retrieve.</param>
    //    /// <returns>The LinkedListNode found at the index given.</returns>
    //    public LinkedListNode<T> this[int index]
    //    {
    //        get
    //        {
    //            //perform the index wrapping
    //            while (index < 0)
    //                index = Count + index;
    //            if (index >= Count)
    //                index %= Count;

    //            //find the proper node
    //            LinkedListNode<T> node = First;
    //            for (int i = 0; i < index; i++)
    //                node = node.Next;

    //            return node;
    //        }
    //    }

    //    /// <summary>
    //    /// Removes the node at a given index.
    //    /// </summary>
    //    /// <param name="index">The index of the node to remove.</param>
    //    public void RemoveAt(int index)
    //    {
    //        Remove(this[index]);
    //    }

    //    /// <summary>
    //    /// Finds the index of a given item.
    //    /// </summary>
    //    /// <param name="item">The item to find.</param>
    //    /// <returns>The index of the item if found; -1 if the item is not found.</returns>
    //    public int IndexOf(T item)
    //    {
    //        for (int i = 0; i < Count; i++)
    //            if (this[i].Value.Equals(item))
    //                return i;

    //        return -1;
    //    }
    //}
}
