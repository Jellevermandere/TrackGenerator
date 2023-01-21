using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public class CreateBorder : MonoBehaviour
{
    private bool looping;
    private Mesh mesh;
    float pathLength = 1;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private Material insideMat, outsideMat;

    [SerializeField]
    [Min(0.01f)]
    private float imageAspectRatio = 2;
    [SerializeField]
    private bool matchRoad = false;

    [SerializeField]
    private bool inside;
    [SerializeField]
    private Vector2 offset = Vector2.zero;
    [SerializeField]
    private float borderHeight = 1f;
    [SerializeField]
    private float topBorderWidth = 0.1f, bottomBorderWidth = 0.3f;

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
        GetComponent<MeshRenderer>().material = inside ? insideMat : outsideMat;
        meshFilter.mesh = CreateBasicRoadMesh(points);
        //GetComponent<MeshFilter>().mesh = CreateBasicRoadMesh(addroadPoints(roadPoints.ToArray()).ToArray());
        meshFilter.sharedMesh.RecalculateBounds();

        MeshCollider meshCollider = gameObject.GetComponent<MeshCollider>();
        meshCollider.sharedMesh = meshFilter.sharedMesh;
    }

    public Mesh CreateBasicRoadMesh(RoadPoint[] points)
    {

        Vector3[] verts = new Vector3[points.Length * 2 * 3]; //2 points per plane, 3 planes
        Vector2[] uvs = new Vector2[verts.Length];
        int[] tris = new int[(points.Length - (looping ? 0 : 1)) * 3 * 6];
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
            Vector3 left = new Vector3(-forward.z, points[i].bankAngle, forward.x).normalized;
            Vector3 up = Vector3.Cross(left, forward);


            // 2 3        2 3
            //1   4      1   4
            //0   5 Road 0   5
            verts[vertIndex] = points[i].Pos() + left * (points[i].roadWidth * 0.5f + (inside ? bottomBorderWidth : 0) + offset.x) * (inside ? 1 : -1) + up * (points[i].curveImpact * points[i].cornerProfile.Evaluate(inside ? 0 : 1) + offset.y);
            verts[vertIndex + 5] = points[i].Pos() + left * (points[i].roadWidth * 0.5f + (inside ? 0 : bottomBorderWidth) + offset.x) * (inside ? 1 : -1) + up * (points[i].curveImpact * points[i].cornerProfile.Evaluate(inside ? 0 : 1) + offset.y);

            if (!matchRoad)
            {
                RaycastHit leftHit;
                if (Physics.Raycast(verts[vertIndex] + Vector3.up, Vector3.down, out leftHit, Mathf.Infinity, groundLayer))
                {
                    verts[vertIndex] += Vector3.down * leftHit.distance + Vector3.up;
                }
                else verts[vertIndex].y = 0;

                RaycastHit rightHit;
                if (Physics.Raycast(verts[vertIndex + 5] + Vector3.up, Vector3.down, out rightHit, Mathf.Infinity, groundLayer))
                {
                    verts[vertIndex + 5] += Vector3.down * rightHit.distance + Vector3.up;
                }
                else verts[vertIndex + 5].y = 0;
            }

            verts[vertIndex + 1] = verts[vertIndex] + up * borderHeight + left * (-bottomBorderWidth / 2f + topBorderWidth / 2f);
            verts[vertIndex + 2] = verts[vertIndex + 1];
            verts[vertIndex + 3] = verts[vertIndex + 5] + up * borderHeight - left * (-bottomBorderWidth / 2f + topBorderWidth / 2f);
            verts[vertIndex + 4] = verts[vertIndex + 3];


            float completionPercent = pathDist / pathLength;  //i / (float)(points.Length);
            float v;
            if ((borderHeight * imageAspectRatio) == 0) v = completionPercent;
            else v = completionPercent * Mathf.Round(pathLength / (borderHeight * imageAspectRatio));  //1 - Mathf.Abs(2 * completionPercent - 1);
            uvs[vertIndex] = new Vector2(1 - v, 0);
            uvs[vertIndex + 1] = new Vector2(1 - v, 1);
            uvs[vertIndex + 2] = new Vector2(v, 1);
            uvs[vertIndex + 3] = new Vector2(v, 1);
            uvs[vertIndex + 4] = new Vector2(v, 1);
            uvs[vertIndex + 5] = new Vector2(v, 0);

            if (i < points.Length - (looping ? 0 : 1))
            {
                tris[triIndex] = vertIndex;
                tris[triIndex + 1] = (vertIndex + 6) % verts.Length;
                tris[triIndex + 2] = vertIndex + 1;

                tris[triIndex + 3] = vertIndex + 1;
                tris[triIndex + 4] = (vertIndex + 6) % verts.Length;
                tris[triIndex + 5] = (vertIndex + 7) % verts.Length;

                tris[triIndex + 6] = vertIndex + 2;
                tris[triIndex + 7] = (vertIndex + 8) % verts.Length;
                tris[triIndex + 8] = vertIndex + 3;

                tris[triIndex + 9] = vertIndex + 3;
                tris[triIndex + 10] = (vertIndex + 8) % verts.Length;
                tris[triIndex + 11] = (vertIndex + 9) % verts.Length;

                tris[triIndex + 12] = vertIndex + 4;
                tris[triIndex + 13] = (vertIndex + 10) % verts.Length;
                tris[triIndex + 14] = vertIndex + 5;

                tris[triIndex + 15] = vertIndex + 5;
                tris[triIndex + 16] = (vertIndex + 10) % verts.Length;
                tris[triIndex + 17] = (vertIndex + 11) % verts.Length;
            }


            if (i < points.Length - 1) pathDist += Vector3.Distance(points[i].Pos(), points[i + 1].Pos());
            vertIndex += 6;
            triIndex += 18;

            //Debug.DrawLine(points[i].Pos(), points[i].Pos() + left * points[i].roadWidth * 1.5f, Color.green);
        }

        mesh = new Mesh();
        mesh.vertices = verts;
        mesh.triangles = tris;
        mesh.uv = uvs;
        mesh.RecalculateNormals();
        mesh.name = "Border";

        return mesh;

    }
}