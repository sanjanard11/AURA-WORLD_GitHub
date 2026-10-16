using System.Collections.Generic;
using UnityEngine;

public static class CartoonMeshUtility
{
    /// <summary>
    /// Creates a tapered rounded capsule/lozenge with custom top and bottom radii, perfect for cartoon limbs, torsos, and heads.
    /// </summary>
    public static Mesh CreateTaperedCapsule(float radiusBottom, float radiusTop, float height, int radialSegments = 16, int heightSegments = 8)
    {
        Mesh mesh = new Mesh();
        mesh.name = "TaperedCapsule";

        List<Vector3> verts = new List<Vector3>();
        List<Vector3> norms = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<int> tris = new List<int>();

        int rings = heightSegments + 1;
        int sectors = radialSegments + 1;

        for (int r = 0; r < rings; r++)
        {
            float v = (float)r / (rings - 1);
            // Non-linear curvature for friendly cartoon volume
            float curve = Mathf.Sin(v * Mathf.PI);
            float radius = Mathf.Lerp(radiusBottom, radiusTop, v) + curve * 0.05f;
            float y = (v - 0.5f) * height;

            // Round top and bottom caps
            if (v < 0.15f)
            {
                float capFrac = v / 0.15f;
                radius = radiusBottom * Mathf.Sin(capFrac * Mathf.PI * 0.5f);
                y = -height * 0.5f + (1f - Mathf.Cos(capFrac * Mathf.PI * 0.5f)) * radiusBottom * 0.5f;
            }
            else if (v > 0.85f)
            {
                float capFrac = (v - 0.85f) / 0.15f;
                radius = radiusTop * Mathf.Cos(capFrac * Mathf.PI * 0.5f);
                y = height * 0.5f - (1f - Mathf.Sin(capFrac * Mathf.PI * 0.5f)) * radiusTop * 0.5f;
            }

            for (int s = 0; s < sectors; s++)
            {
                float u = (float)s / radialSegments;
                float angle = u * Mathf.PI * 2f;
                float cos = Mathf.Cos(angle);
                float sin = Mathf.Sin(angle);

                Vector3 pos = new Vector3(cos * radius, y, sin * radius);
                Vector3 norm = new Vector3(cos, 0f, sin).normalized;

                verts.Add(pos);
                norms.Add(norm);
                uvs.Add(new Vector2(u, v));
            }
        }

        for (int r = 0; r < rings - 1; r++)
        {
            for (int s = 0; s < sectors - 1; s++)
            {
                int cur = r * sectors + s;
                int next = cur + sectors;

                tris.Add(cur);
                tris.Add(next);
                tris.Add(cur + 1);

                tris.Add(cur + 1);
                tris.Add(next);
                tris.Add(next + 1);
            }
        }

        mesh.SetVertices(verts);
        mesh.SetNormals(norms);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    /// <summary>
    /// Creates a stylized cartoon head with sculpted jaw, cheeks, and rounded chin.
    /// </summary>
    public static Mesh CreateCartoonHeadMesh(float width = 0.38f, float height = 0.44f, float depth = 0.40f)
    {
        Mesh mesh = new Mesh();
        mesh.name = "CartoonHead";

        int latSegments = 16;
        int lonSegments = 20;

        List<Vector3> verts = new List<Vector3>();
        List<Vector3> norms = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<int> tris = new List<int>();

        for (int i = 0; i <= latSegments; i++)
        {
            float v = (float)i / latSegments;
            float phi = v * Mathf.PI; // 0 to PI

            for (int j = 0; j <= lonSegments; j++)
            {
                float u = (float)j / lonSegments;
                float theta = u * Mathf.PI * 2f; // 0 to 2PI

                float x = Mathf.Sin(phi) * Mathf.Cos(theta);
                float y = Mathf.Cos(phi);
                float z = Mathf.Sin(phi) * Mathf.Sin(theta);

                // Morph sphere into attractive cartoon face:
                // 1. Cheek volume (wider in lower-mid face)
                float cheekBonus = (y > -0.2f && y < 0.3f && z > 0f) ? (1f - Mathf.Abs(y - 0.05f) * 3f) * 0.12f : 0f;
                // 2. Chin taper (tapers inward towards bottom front)
                float chinTaper = (y < -0.3f) ? (1f + (y + 0.3f) * 0.6f) : 1f;
                // 3. Flattened face plane for eyes & mouth placement
                float faceFlatten = (z > 0.4f) ? 0.88f : 1f;

                Vector3 p = new Vector3(
                    x * width * 0.5f * (1f + cheekBonus) * chinTaper,
                    y * height * 0.5f,
                    z * depth * 0.5f * faceFlatten
                );

                verts.Add(p);
                norms.Add(p.normalized);
                uvs.Add(new Vector2(u, v));
            }
        }

        for (int i = 0; i < latSegments; i++)
        {
            for (int j = 0; j < lonSegments; j++)
            {
                int first = (i * (lonSegments + 1)) + j;
                int second = first + lonSegments + 1;

                tris.Add(first);
                tris.Add(second);
                tris.Add(first + 1);

                tris.Add(second);
                tris.Add(second + 1);
                tris.Add(first + 1);
            }
        }

        mesh.SetVertices(verts);
        mesh.SetNormals(norms);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    /// <summary>
    /// Creates a proper cartoon shoe with rounded toe box, flat sole, and ankle collar.
    /// </summary>
    public static Mesh CreateCartoonShoeMesh(float length = 0.28f, float width = 0.16f, float height = 0.12f)
    {
        Mesh mesh = new Mesh();
        mesh.name = "CartoonShoe";

        List<Vector3> verts = new List<Vector3>();
        List<int> tris = new List<int>();

        // Box base morphed into shoe:
        // Heel is at z = -length * 0.45, toe is at z = length * 0.55
        float hx = width * 0.5f;
        float hy = height;
        float hzToe = length * 0.55f;
        float hzHeel = -length * 0.45f;
        float hzMid = 0f;

        // Vertices: Sole (0-7), Upper (8-15), Ankle Rim (16-19)
        // Sole vertices (y = 0)
        verts.Add(new Vector3(-hx * 0.85f, 0f, hzHeel));        // 0 Heel BL
        verts.Add(new Vector3( hx * 0.85f, 0f, hzHeel));        // 1 Heel BR
        verts.Add(new Vector3(-hx * 0.75f, 0f, hzMid));         // 2 Mid BL
        verts.Add(new Vector3( hx * 0.75f, 0f, hzMid));         // 3 Mid BR
        verts.Add(new Vector3(-hx,         0f, hzToe * 0.65f)); // 4 Ball BL
        verts.Add(new Vector3( hx,         0f, hzToe * 0.65f)); // 5 Ball BR
        verts.Add(new Vector3(-hx * 0.5f,  0f, hzToe));         // 6 Toe BL
        verts.Add(new Vector3( hx * 0.5f,  0f, hzToe));         // 7 Toe BR

        // Upper vertices (y = hy)
        verts.Add(new Vector3(-hx * 0.75f, hy * 0.9f, hzHeel));        // 8 Heel TL
        verts.Add(new Vector3( hx * 0.75f, hy * 0.9f, hzHeel));        // 9 Heel TR
        verts.Add(new Vector3(-hx * 0.7f,  hy * 0.9f, hzMid));         // 10 Mid TL
        verts.Add(new Vector3( hx * 0.7f,  hy * 0.9f, hzMid));         // 11 Mid TR
        verts.Add(new Vector3(-hx * 0.9f,  hy * 0.6f, hzToe * 0.65f)); // 12 Ball TL
        verts.Add(new Vector3( hx * 0.9f,  hy * 0.6f, hzToe * 0.65f)); // 13 Ball TR
        verts.Add(new Vector3(-hx * 0.4f,  hy * 0.4f, hzToe));         // 14 Toe TL
        verts.Add(new Vector3( hx * 0.4f,  hy * 0.4f, hzToe));         // 15 Toe TR

        // Ankle opening collar
        verts.Add(new Vector3(-hx * 0.55f, hy * 1.15f, hzHeel * 0.5f)); // 16 Ankle BL
        verts.Add(new Vector3( hx * 0.55f, hy * 1.15f, hzHeel * 0.5f)); // 17 Ankle BR
        verts.Add(new Vector3(-hx * 0.55f, hy * 1.15f, hzMid * 0.8f));  // 18 Ankle FL
        verts.Add(new Vector3( hx * 0.55f, hy * 1.15f, hzMid * 0.8f));  // 19 Ankle FR

        // Helper to add quad
        System.Action<int, int, int, int> addQuad = (a, b, c, d) => {
            tris.Add(a); tris.Add(b); tris.Add(c);
            tris.Add(a); tris.Add(c); tris.Add(d);
        };

        // Sole bottom
        addQuad(0, 2, 3, 1);
        addQuad(2, 4, 5, 3);
        addQuad(4, 6, 7, 5);

        // Sides
        // Left side
        addQuad(0, 8, 10, 2);
        addQuad(2, 10, 12, 4);
        addQuad(4, 12, 14, 6);
        // Right side
        addQuad(1, 3, 11, 9);
        addQuad(3, 5, 13, 11);
        addQuad(5, 7, 15, 13);
        // Heel back
        addQuad(1, 9, 8, 0);
        // Toe front
        addQuad(6, 14, 15, 7);

        // Top front / toe bridge
        addQuad(14, 12, 13, 15);
        addQuad(12, 18, 19, 13);

        // Ankle collar sides
        addQuad(10, 8, 16, 18);
        addQuad(9, 11, 19, 17);
        addQuad(8, 9, 17, 16);

        mesh.SetVertices(verts);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    /// <summary>
    /// Creates a cartoon mitten hand with palm and an angled cartoon thumb.
    /// </summary>
    public static Mesh CreateCartoonHandMesh(float size = 0.12f)
    {
        Mesh mesh = new Mesh();
        mesh.name = "CartoonHand";

        List<Vector3> verts = new List<Vector3>();
        List<int> tris = new List<int>();

        float w = size * 0.45f;
        float h = size * 0.65f;
        float d = size * 0.28f;

        // Palm box (0-7)
        verts.Add(new Vector3(-w, 0f, -d));
        verts.Add(new Vector3( w, 0f, -d));
        verts.Add(new Vector3( w, -h, -d));
        verts.Add(new Vector3(-w, -h, -d));
        verts.Add(new Vector3(-w, 0f,  d));
        verts.Add(new Vector3( w, 0f,  d));
        verts.Add(new Vector3( w, -h,  d));
        verts.Add(new Vector3(-w, -h,  d));

        // Thumb box (8-15) - extends inward/forward
        float tw = size * 0.22f;
        float th = size * 0.35f;
        float td = size * 0.22f;
        Vector3 to = new Vector3(w * 0.8f, -h * 0.35f, d * 0.4f);

        verts.Add(to + new Vector3(0f, 0f, 0f));
        verts.Add(to + new Vector3(tw, 0f, 0f));
        verts.Add(to + new Vector3(tw, -th, td));
        verts.Add(to + new Vector3(0f, -th, td));
        verts.Add(to + new Vector3(0f, tw, 0f));
        verts.Add(to + new Vector3(tw, tw, 0f));
        verts.Add(to + new Vector3(tw, 0f, td));
        verts.Add(to + new Vector3(0f, 0f, td));

        System.Action<int, int, int, int> addQuad = (a, b, c, d4) => {
            tris.Add(a); tris.Add(b); tris.Add(c);
            tris.Add(a); tris.Add(c); tris.Add(d4);
        };

        // Palm faces
        addQuad(0, 1, 2, 3); // Front
        addQuad(5, 4, 7, 6); // Back
        addQuad(4, 0, 3, 7); // Left
        addQuad(1, 5, 6, 2); // Right
        addQuad(4, 5, 1, 0); // Top
        addQuad(3, 2, 6, 7); // Bottom

        // Thumb faces
        addQuad(8, 9, 10, 11);
        addQuad(13, 12, 15, 14);
        addQuad(12, 8, 11, 15);
        addQuad(9, 13, 14, 10);
        addQuad(12, 13, 9, 8);
        addQuad(11, 10, 14, 15);

        mesh.SetVertices(verts);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    /// <summary>
    /// Creates a detailed 3D scalloped parasol/umbrella with curved ribs, top ferrule, and hook handle.
    /// </summary>
    public static Mesh CreateUmbrellaCanopy(float radius = 0.55f, float depth = 0.24f, int ribs = 8)
    {
        Mesh mesh = new Mesh();
        mesh.name = "UmbrellaCanopy";

        List<Vector3> verts = new List<Vector3>();
        List<Vector3> norms = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<int> tris = new List<int>();

        // Apex vertex
        verts.Add(new Vector3(0f, depth, 0f));
        norms.Add(Vector3.up);
        uvs.Add(new Vector2(0.5f, 0.5f));

        int ringSegments = 4;
        for (int r = 1; r <= ringSegments; r++)
        {
            float frac = (float)r / ringSegments;
            float curR = radius * Mathf.Sin(frac * Mathf.PI * 0.5f);
            float curY = depth * Mathf.Cos(frac * Mathf.PI * 0.5f);

            int rimSectors = ribs * 4;
            for (int s = 0; s < rimSectors; s++)
            {
                float angle = (float)s / rimSectors * Mathf.PI * 2f;
                // Add scalloped drape between ribs at outer ring
                float ribAngle = angle * ribs;
                float scallop = (r == ringSegments) ? Mathf.Sin(ribAngle) * 0.035f : 0f;

                float x = Mathf.Cos(angle) * (curR + scallop);
                float z = Mathf.Sin(angle) * (curR + scallop);
                float y = curY;

                Vector3 p = new Vector3(x, y, z);
                verts.Add(p);
                norms.Add(new Vector3(x, depth * 0.5f, z).normalized);
                uvs.Add(new Vector2(0.5f + x / (radius * 2f), 0.5f + z / (radius * 2f)));
            }
        }

        int rimCount = ribs * 4;
        // Apex triangle fan
        for (int s = 0; s < rimCount; s++)
        {
            int next = (s + 1) % rimCount;
            tris.Add(0);
            tris.Add(s + 1);
            tris.Add(next + 1);
        }

        // Concentric rings
        for (int r = 1; r < ringSegments; r++)
        {
            int baseCur = 1 + (r - 1) * rimCount;
            int baseNext = 1 + r * rimCount;

            for (int s = 0; s < rimCount; s++)
            {
                int sNext = (s + 1) % rimCount;
                int c0 = baseCur + s;
                int c1 = baseCur + sNext;
                int n0 = baseNext + s;
                int n1 = baseNext + sNext;

                tris.Add(c0); tris.Add(n0); tris.Add(c1);
                tris.Add(c1); tris.Add(n0); tris.Add(n1);
            }
        }

        mesh.SetVertices(verts);
        mesh.SetNormals(norms);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }
}
