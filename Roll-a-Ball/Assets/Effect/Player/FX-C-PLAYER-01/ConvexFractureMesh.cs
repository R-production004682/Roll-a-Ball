using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 凸形状をランダムな平面で切り、断面を閉じた不揃いの破片メッシュを作る
/// </summary>
public static class ConvexFractureMesh
{
    public const int MinimumPieceCount = 2;
    public const int MaximumPieceCount = 12;

    private const float Epsilon = 0.00001f;
    private const float MinimumTriangleCrossSquared = 0.000000000001f;
    private const float MinimumPieceSelectionWeight = 0.8f;
    private const float MaximumPieceSelectionWeight = 1.2f;
    private const float MinimumCutPositionRatio = 0.34f;
    private const float MaximumCutPositionRatio = 0.66f;
    private const float CapAxisAlignmentThreshold = 0.9f;

    /// <summary>
    /// 切断平面の法線方向と逆方向のどちらを残すかを指定する
    /// </summary>
    private enum PlaneSide
    {
        Negative = -1,
        Positive = 1
    }

    private struct Vertex
    {
        public Vector3 position;
        public Vector3 normal;
        public Color color;
    }

    private sealed class Piece
    {
        public readonly List<Vertex> vertices = new List<Vertex>();

        /// <summary>
        /// 分割する大きな破片を選ぶために閉じた形状の体積を求める
        /// </summary>
        public float Volume()
        {
            var volume = 0f;
            for (var i = 0; i < vertices.Count; i += 3)
            {
                volume += Vector3.Dot(vertices[i].position,
                    Vector3.Cross(vertices[i + 1].position, vertices[i + 2].position)) / 6f;
            }
            return Mathf.Abs(volume);
        }
    }

    /// <summary>
    /// 読み取り可能な凸メッシュを指定した個数へ分割する
    /// </summary>
    /// <param name="source">球などの閉じた凸メッシュ</param>
    /// <param name="count">生成する個数、2〜12 に制限する</param>
    /// <param name="seed">ゲーム全体の乱数状態を変えずに切断形状を決める種</param>
    /// <returns>呼び出し側が寿命を管理する断面付きメッシュ</returns>
    public static List<Mesh> Generate(Mesh source, int count, int seed)
    {
        var pieces = new List<Piece>();
        var result = new List<Mesh>();
        if (source == null || !source.isReadable)
        {
            return result;
        }
        var positions = source.vertices;
        var normals = source.normals;
        var triangles = source.triangles;
        var first = new Piece();
        foreach (var index in triangles)
        {
            first.vertices.Add(new Vertex
            {
                position = positions[index],
                normal = normals.Length == positions.Length ? normals[index] : positions[index].normalized,
                color = new Color(0f, 0f, 0f, 1f)
            });
        }
        pieces.Add(first);
        var random = new System.Random(seed);
        var targetCount = Mathf.Clamp(count, MinimumPieceCount, MaximumPieceCount);
        while (pieces.Count < targetCount)
        {
            var index = 0;
            var largest = 0f;
            for (var i = 0; i < pieces.Count; i++)
            {
                var score = pieces[i].Volume() * Mathf.Lerp(MinimumPieceSelectionWeight, MaximumPieceSelectionWeight, (float)random.NextDouble());
                if (score > largest)
                {
                    largest = score;
                    index = i;
                }
            }
            var piece = pieces[index];
            var normal = RandomDirection(random);
            var low = float.PositiveInfinity;
            var high = float.NegativeInfinity;
            foreach (var vertex in piece.vertices)
            {
                var distance = Vector3.Dot(normal, vertex.position);
                low = Mathf.Min(low, distance);
                high = Mathf.Max(high, distance);
            }
            var offset = Mathf.Lerp(low, high, Mathf.Lerp(MinimumCutPositionRatio, MaximumCutPositionRatio, (float)random.NextDouble()));
            var plane = new Plane(normal, -offset);
            var positive = Clip(piece, plane, PlaneSide.Positive);
            var negative = Clip(piece, plane, PlaneSide.Negative);
            if (positive.vertices.Count == 0 || negative.vertices.Count == 0)
            {
                break;
            }
            pieces[index] = positive;
            pieces.Add(negative);
        }
        foreach (var piece in pieces)
        {
            result.Add(CreateMesh(piece));
        }
        return result;
    }

    /// <summary>
    /// 一様な球面方向をローカル乱数から生成する
    /// </summary>
    private static Vector3 RandomDirection(System.Random random)
    {
        var y = (float)random.NextDouble() * 2f - 1f;
        var angle = (float)random.NextDouble() * Mathf.PI * 2f;
        var radius = Mathf.Sqrt(1f - y * y);
        return new Vector3(radius * Mathf.Cos(angle), y, radius * Mathf.Sin(angle));
    }

    /// <summary>
    /// 三角形を平面の片側へ切り取り切断面を新しいポリゴンで閉じる
    /// </summary>
    private static Piece Clip(Piece source, Plane plane, PlaneSide side)
    {
        var sideSign = (float)side;
        var result = new Piece();
        var rim = new List<Vector3>();
        var polygon = new List<Vertex>(4);
        for (var i = 0; i < source.vertices.Count; i += 3)
        {
            polygon.Clear();
            for (var j = 0; j < 3; j++)
            {
                var a = source.vertices[i + j];
                var b = source.vertices[i + (j + 1) % 3];
                var da = plane.GetDistanceToPoint(a.position) * sideSign;
                var db = plane.GetDistanceToPoint(b.position) * sideSign;
                if (da >= -Epsilon)
                {
                    polygon.Add(a);
                }
                if ((da > Epsilon && db < -Epsilon) || (da < -Epsilon && db > Epsilon))
                {
                    var t = da / (da - db);
                    var color = Color.Lerp(a.color, b.color, t);
                    color.g = 1f;
                    var intersection = new Vertex
                    {
                        position = Vector3.Lerp(a.position, b.position, t),
                        normal = Vector3.Lerp(a.normal, b.normal, t).normalized,
                        color = color
                    };
                    polygon.Add(intersection);
                    AddUnique(rim, intersection.position);
                }
                else if (Mathf.Abs(da) <= Epsilon)
                {
                    AddUnique(rim, a.position);
                }
            }
            for (var j = 1; j + 1 < polygon.Count; j++)
            {
                AddTriangle(result, polygon[0], polygon[j], polygon[j + 1]);
            }
        }
        Cap(result, rim, -plane.normal * sideSign);
        return result;
    }

    /// <summary>
    /// UV 継ぎ目で重複する切断点を許容差でまとめる
    /// </summary>
    private static void AddUnique(List<Vector3> points, Vector3 point)
    {
        foreach (var existing in points)
        {
            if ((existing - point).sqrMagnitude < Epsilon * Epsilon)
            {
                return;
            }
        }
        points.Add(point);
    }

    /// <summary>
    /// 切断点を平面内の角度順に並べ外向き法線の断面を作る
    /// </summary>
    private static void Cap(Piece piece, List<Vector3> rim, Vector3 normal)
    {
        if (rim.Count < 3)
        {
            return;
        }
        var center = Vector3.zero;
        foreach (var point in rim)
        {
            center += point;
        }
        center /= rim.Count;
        var axis = Vector3.Cross(normal, Mathf.Abs(normal.y) < CapAxisAlignmentThreshold ? Vector3.up : Vector3.right).normalized;
        var secondAxis = Vector3.Cross(normal, axis);
        rim.Sort((a, b) => Mathf.Atan2(Vector3.Dot(a - center, secondAxis), Vector3.Dot(a - center, axis))
            .CompareTo(Mathf.Atan2(Vector3.Dot(b - center, secondAxis), Vector3.Dot(b - center, axis))));
        var middle = new Vertex { position = center, normal = normal, color = new Color(1f, 0f, 0f, 1f) };
        for (var i = 0; i < rim.Count; i++)
        {
            var a = new Vertex { position = rim[i], normal = normal, color = new Color(1f, 1f, 0f, 1f) };
            var b = new Vertex { position = rim[(i + 1) % rim.Count], normal = normal, color = a.color };
            AddTriangle(piece, middle, a, b);
        }
    }

    /// <summary>
    /// 面積がある三角形だけを追加する
    /// </summary>
    private static void AddTriangle(Piece piece, Vertex a, Vertex b, Vertex c)
    {
        if (Vector3.Cross(b.position - a.position, c.position - a.position).sqrMagnitude <= MinimumTriangleCrossSquared)
        {
            return;
        }
        piece.vertices.Add(a);
        piece.vertices.Add(b);
        piece.vertices.Add(c);
    }

    /// <summary>
    /// 外周の滑らかな法線と断面の平面法線・色属性をメッシュへ保存する
    /// </summary>
    private static Mesh CreateMesh(Piece piece)
    {
        var positions = new List<Vector3>(piece.vertices.Count);
        var normals = new List<Vector3>(piece.vertices.Count);
        var colors = new List<Color>(piece.vertices.Count);
        var indices = new List<int>(piece.vertices.Count);
        foreach (var vertex in piece.vertices)
        {
            indices.Add(positions.Count);
            positions.Add(vertex.position);
            normals.Add(vertex.normal);
            colors.Add(vertex.color);
        }
        var mesh = new Mesh { name = "PlayerFractureShard" };
        mesh.SetVertices(positions);
        mesh.SetNormals(normals);
        mesh.SetColors(colors);
        mesh.SetTriangles(indices, 0);
        mesh.RecalculateBounds();
        return mesh;
    }
}
