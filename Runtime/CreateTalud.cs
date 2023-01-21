using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public class CreateTalud : MonoBehaviour
{
    private bool looping;
    [SerializeField]
    private bool generateUnderTrack = false;
    [SerializeField]
    private bool matchTerrain = true;
    private Mesh mesh;
    [SerializeField] private LayerMask groundLayer;

    [SerializeField]
    private float tiling = 2;
    [SerializeField]
    [Range(1, 90)]
    private float maxTaludAngle = 10;
    [SerializeField]
    private float minHeight = 0.1f;

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

        MeshCollider meshCollider = gameObject.GetComponent<MeshCollider>();
        meshCollider.sharedMesh = meshFilter.sharedMesh;
    }

    public Mesh CreateBasicRoadMesh(RoadPoint[] points)
    {

        Vector3[] verts = new Vector3[points.Length * 2 * 2];
        Vector2[] uvs = new Vector2[verts.Length];
        int[] tris = new int[(generateUnderTrack ? 6 : 4) * (points.Length - (looping ? 0 : 1)) * 3];
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

            // left 0-1 -road- 2-3 right
            verts[vertIndex + 1] = points[i].Pos() + left * points[i].roadWidth * 0.5f + up * points[i].curveImpact * points[i].cornerProfile.Evaluate(0);
            verts[vertIndex + 2] = points[i].Pos() - left * points[i].roadWidth * 0.5f + up * points[i].curveImpact * points[i].cornerProfile.Evaluate(1);

            Vector3 leftGroundPointOffest = new Vector3(left.x, 0, left.z) * (Mathf.Max(minHeight, verts[vertIndex + 1].y)) / Mathf.Tan(maxTaludAngle * Mathf.Deg2Rad) + Vector3.down * Mathf.Max(minHeight, verts[vertIndex + 1].y);
            Vector3 rightGroundPointOffest = -new Vector3(left.x, 0, left.z) * (Mathf.Max(minHeight, verts[vertIndex + 2].y)) / Mathf.Tan(maxTaludAngle * Mathf.Deg2Rad) + Vector3.down * Mathf.Max(minHeight, verts[vertIndex + 2].y);

            if (matchTerrain)
            {
                RaycastHit leftHit;
                if (Physics.Raycast(verts[vertIndex + 1], leftGroundPointOffest.normalized, out leftHit, Mathf.Infinity, groundLayer))
                {
                    verts[vertIndex] = verts[vertIndex + 1] + leftGroundPointOffest.normalized * leftHit.distance;
                }
                else verts[vertIndex] = verts[vertIndex + 1] + leftGroundPointOffest;

                RaycastHit rightHit;
                if (Physics.Raycast(verts[vertIndex + 2], rightGroundPointOffest.normalized, out rightHit, Mathf.Infinity, groundLayer))
                {
                    verts[vertIndex + 3] = verts[vertIndex + 2] + rightGroundPointOffest.normalized * rightHit.distance;
                }
                else verts[vertIndex + 3] = verts[vertIndex + 2] + rightGroundPointOffest;
            }
            else
            {
                verts[vertIndex] = verts[vertIndex + 1] + leftGroundPointOffest;
                verts[vertIndex + 3] = verts[vertIndex + 2] + rightGroundPointOffest;
            }


            float completionPercent = pathDist / pathLength;  //i / (float)(points.Length);
            float v;
            if ((tiling) == 0) v = completionPercent;
            else v = completionPercent * Mathf.Round(pathLength / tiling);  //1 - Mathf.Abs(2 * completionPercent - 1);
            uvs[vertIndex] = new Vector2(0, v);
            uvs[vertIndex + 1] = new Vector2(0.5f, v);
            uvs[vertIndex + 2] = new Vector2(0.5f, v);
            uvs[vertIndex + 3] = new Vector2(1, v);

            // left 0-1 -road- 2-3 right
            if (i < points.Length - (looping ? 0 : 1))
            {
                tris[triIndex + 0] = vertIndex;
                tris[triIndex + 1] = (vertIndex + 4) % verts.Length;
                tris[triIndex + 2] = vertIndex + 1;

                tris[triIndex + 3] = vertIndex + 1;
                tris[triIndex + 4] = (vertIndex + 4) % verts.Length;
                tris[triIndex + 5] = (vertIndex + 5) % verts.Length;

                tris[triIndex + 6] = vertIndex + 2;
                tris[triIndex + 7] = (vertIndex + 6) % verts.Length;
                tris[triIndex + 8] = vertIndex + 3;

                tris[triIndex + 9] = vertIndex + 3;
                tris[triIndex + 10] = (vertIndex + 6) % verts.Length;
                tris[triIndex + 11] = (vertIndex + 7) % verts.Length;

                if (generateUnderTrack)
                {
                    tris[triIndex + 12] = vertIndex + 1;
                    tris[triIndex + 13] = (vertIndex + 5) % verts.Length;
                    tris[triIndex + 14] = vertIndex + 2;

                    tris[triIndex + 15] = vertIndex + 2;
                    tris[triIndex + 16] = (vertIndex + 5) % verts.Length;
                    tris[triIndex + 17] = (vertIndex + 6) % verts.Length;
                }


            }


            if (i < points.Length - 1) pathDist += Vector3.Distance(points[i].Pos(), points[i + 1].Pos());
            vertIndex += 4;
            triIndex += generateUnderTrack ? 18 : 12;

            //Debug.DrawLine(points[i].Pos(), points[i].Pos() + left * points[i].roadWidth * 1.5f, Color.green);
        }

        mesh = new Mesh();
        mesh.vertices = verts;
        mesh.triangles = tris;
        mesh.uv = uvs;
        mesh.RecalculateNormals();
        mesh.name = "Talud";

        return mesh;

    }
}