using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class CreateMiniMapRoad : MonoBehaviour
{
    private bool looping;
    private Mesh mesh;

    [SerializeField]
    private float tiling = 2;
    [SerializeField]
    private float widthModifier = 1.5f;

    float pathLength = 1;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    public void UpdateRoad(RoadPoint[] points, bool loop)
    {
        looping = loop;
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        meshFilter.mesh = CreateBasicRoadMesh(points);
        //GetComponent<MeshFilter>().mesh = CreateBasicRoadMesh(addroadPoints(roadPoints.ToArray()).ToArray());
        meshFilter.sharedMesh.RecalculateBounds();
    }

    public Mesh CreateBasicRoadMesh(RoadPoint[] points)
    {

        Vector3[] verts = new Vector3[points.Length * 2];
        Vector2[] uvs = new Vector2[verts.Length];
        int[] tris = new int[(points.Length - (looping ? 0 : 1)) * 2 * 3];
        int vertIndex = 0;
        int triIndex = 0;

        pathLength = 0;

        for (int i = 0; i < points.Length - (looping ? 0 : 1); i++)
        {
            pathLength += Vector3.Distance(points[i].Pos(), points[(i + 1) % points.Length].Pos());
        }

        float pathDist = 0f;

        for (int i = 0; i < points.Length; i++)
        {
            Vector3 forward = Vector3.zero;

            forward += points[(i + 1) % points.Length].Pos() - points[i].Pos();

            if (looping || (i != 0 && i != points.Length - 1)) forward += points[i].Pos() - points[(i - 1 + points.Length) % points.Length].Pos();

            if (!looping && i == points.Length - 1) forward = points[(i) % points.Length].Pos() - points[i - 1].Pos();

            forward.Normalize();
            Vector3 left = new Vector3(-forward.z, 0, forward.x).normalized;
            Vector3 up = Vector3.Cross(left, forward);

            // left 0-1 -road- 2-3 right
            verts[vertIndex] = points[i].Pos() + left * points[i].roadWidth * widthModifier * 0.5f;
            verts[vertIndex + 1] = points[i].Pos() - left * points[i].roadWidth * widthModifier * 0.5f;

            float completionPercent = pathDist / pathLength;  //i / (float)(points.Length);
            float v;
            if ((tiling) == 0) v = completionPercent;
            else v = completionPercent * Mathf.Round(pathLength / tiling);  //1 - Mathf.Abs(2 * completionPercent - 1);
            uvs[vertIndex] = new Vector2(0, v);
            uvs[vertIndex + 1] = new Vector2(1, v);

            if (i < points.Length - (looping ? 0 : 1))
            {
                tris[triIndex] = vertIndex;
                tris[triIndex + 1] = (vertIndex + 2) % verts.Length;
                tris[triIndex + 2] = vertIndex + 1;

                tris[triIndex + 3] = vertIndex + 1;
                tris[triIndex + 4] = (vertIndex + 2) % verts.Length;
                tris[triIndex + 5] = (vertIndex + 3) % verts.Length;

            }


            if (i < points.Length - 1) pathDist += Vector3.Distance(points[i].Pos(), points[i + 1].Pos());
            vertIndex += 2;
            triIndex += 6;

            //Debug.DrawLine(points[i].Pos(), points[i].Pos() + left * points[i].roadWidth * 1.5f, Color.green);
        }

        mesh = new Mesh();
        mesh.vertices = verts;
        mesh.triangles = tris;
        mesh.uv = uvs;
        mesh.RecalculateNormals();
        mesh.name = "minimap";

        return mesh;

    }
}