using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JelleVer.TrackGenerator
{
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    [RequireComponent(typeof(MeshCollider))]

    public class TrackGenerator : MonoBehaviour
    {
        [Header("Track Shape Settings")]
        public bool randomize;
        public float meanRadius;
        [Range(3, 1000)]
        public int nrOfPoints;
        [Range(-10, 10)]
        public float relativeNoiseAmplitude;
        public float randomValue;
        public Vector3 worldOffset;
        [Range(1, 10)]
        public int startSmoothness = 3;
        public float roadWidth = 1;
        public float railingWidth = 1f;
        public float railingHeight = 1f;
        public float topRailingWidth = 0.5f;
        public float roadHeight = 0.1f;

        [Header("Visual Settings")]
        public float tiling = 1;

        public float updateInteral;

        private List<GameObject> groundPoints = new List<GameObject>();
        [HideInInspector]
        public Vector3[] pointsOnCurve;
        private LineRenderer line;
        private float noise;
        [HideInInspector]
        public float pathLength;
        [HideInInspector]
        public float[] distanceToNextPoint;

        private float timePassed;



        // Start is called before the first frame update
        void Awake()
        {
            if (randomize)
            {
                randomValue = Random.Range(0.0f, 5f);
            }
            GeneratePointsInCurve();
            UpdateRoad();
        }

        // Update is called once per frame
        void Update()
        {

            if (timePassed > updateInteral)
            {
                GeneratePointsInCurve();
                UpdateRoad();
                timePassed = 0f;
            }
            timePassed += Time.deltaTime;


        }


        // generates random points on a smooth closed curve
        public void GeneratePointsInCurve()
        {
            float angle = 0f;
            float startOffset = 0f;

            pathLength = 0f;
            distanceToNextPoint = new float[nrOfPoints];

            pointsOnCurve = new Vector3[nrOfPoints];

            for (int i = 0; i < nrOfPoints; i++)
            {
                noise = Mathf.PerlinNoise(angle, randomValue);
                if (Mathf.Cos(angle) > 0)
                {
                    startOffset = Mathf.Abs(Mathf.Pow(Mathf.Sin(angle), startSmoothness));
                }
                else startOffset = 1;

                Vector3 point = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * (meanRadius + (relativeNoiseAmplitude * meanRadius * noise * startOffset)) + worldOffset;
                pointsOnCurve[i] = point;

                angle += 2 * Mathf.PI / nrOfPoints;

                if (i > 0)
                {
                    float dist = Vector3.Distance(pointsOnCurve[i - 1], pointsOnCurve[i]);
                    distanceToNextPoint[i - 1] = dist;
                    pathLength += dist;
                }
            }
            distanceToNextPoint[nrOfPoints - 1] = Vector3.Distance(pointsOnCurve[nrOfPoints - 1], pointsOnCurve[0]);
            pathLength += distanceToNextPoint[nrOfPoints - 1];
        }


        public void UpdateRoad()
        {
            MeshFilter meshFilter = GetComponent<MeshFilter>();
            meshFilter.mesh = CreateRoadMesh(pointsOnCurve);
            meshFilter.mesh.RecalculateBounds();
            MeshCollider meshCollider = gameObject.GetComponent<MeshCollider>();
            meshCollider.sharedMesh = meshFilter.mesh;

            int textureRepeat = Mathf.RoundToInt(tiling * pathLength * 0.05f);
            GetComponent<MeshRenderer>().sharedMaterial.mainTextureScale = new Vector2(1, textureRepeat);
        }
        //generates a mesh
        public Mesh CreateBasicRoadMesh(Vector3[] points)
        {
            Vector3[] verts = new Vector3[points.Length * 2];
            Vector2[] uvs = new Vector2[verts.Length];
            int[] tris = new int[(2 * (points.Length - 1) + 2) * 3];
            int vertIndex = 0;
            int triIndex = 0;

            float pathDist = 0f;

            for (int i = 0; i < points.Length; i++)
            {
                Vector3 forward = Vector3.zero;

                forward += points[(i + 1) % points.Length] - points[i];

                forward += points[i] - points[(i - 1 + points.Length) % points.Length];

                forward.Normalize();
                Vector3 left = new Vector3(-forward.z, 0, forward.x);

                verts[vertIndex] = points[i] + left * roadWidth * 0.5f;
                verts[vertIndex + 1] = points[i] - left * roadWidth * 0.5f;



                float completionPercent = pathDist / pathLength; //i / (float)(points.Length - 1);
                float v = 1 - Mathf.Abs(2 * completionPercent - 1);
                uvs[vertIndex] = new Vector2(0, v);
                uvs[vertIndex + 1] = new Vector2(1, v);


                tris[triIndex] = vertIndex;
                tris[triIndex + 1] = (vertIndex + 2) % verts.Length;
                tris[triIndex + 2] = vertIndex + 1;

                tris[triIndex + 3] = vertIndex + 1;
                tris[triIndex + 4] = (vertIndex + 2) % verts.Length;
                tris[triIndex + 5] = (vertIndex + 3) % verts.Length;

                pathDist += distanceToNextPoint[i];
                vertIndex += 2;
                triIndex += 6;
            }

            Mesh mesh = new Mesh();
            mesh.vertices = verts;
            mesh.triangles = tris;
            mesh.uv = uvs;
            mesh.RecalculateNormals();

            return mesh;

        }
        //generates a mesh
        public Mesh CreateRoadMesh(Vector3[] points)
        {
            Vector3[] verts = new Vector3[points.Length * 8];
            Vector2[] uvs = new Vector2[verts.Length];
            int[] tris = new int[14 * points.Length * 3];
            int vertIndex = 0;
            int triIndex = 0;

            float pathDist = 0f;

            for (int i = 0; i < points.Length; i++)
            {
                Vector3 forward = Vector3.zero;

                forward += points[(i + 1) % points.Length] - points[i];

                forward += points[i] - points[(i - 1 + points.Length) % points.Length];

                forward.Normalize();
                Vector3 left = new Vector3(-forward.z, 0, forward.x);
                //Vector3 left = Vector3.Normalize(-points[i] + worldOffset);

                verts[vertIndex] = points[i] + left * (roadWidth * 0.5f + railingWidth);
                verts[vertIndex + 1] = points[i] + left * (roadWidth * 0.5f + (railingWidth - topRailingWidth) / 2f + topRailingWidth) + Vector3.up * railingHeight;
                verts[vertIndex + 2] = points[i] + left * (roadWidth * 0.5f + (railingWidth - topRailingWidth) / 2f) + Vector3.up * railingHeight;
                verts[vertIndex + 3] = points[i] + left * (roadWidth * 0.5f) + Vector3.up * roadHeight;
                verts[vertIndex + 4] = points[i] - left * (roadWidth * 0.5f) + Vector3.up * roadHeight;
                verts[vertIndex + 5] = points[i] - left * (roadWidth * 0.5f + (railingWidth - topRailingWidth) / 2f) + Vector3.up * railingHeight;
                verts[vertIndex + 6] = points[i] - left * (roadWidth * 0.5f + (railingWidth - topRailingWidth) / 2f + topRailingWidth) + Vector3.up * railingHeight;
                verts[vertIndex + 7] = points[i] - left * (roadWidth * 0.5f + railingWidth);



                float completionPercent = pathDist / pathLength; //i / (float)(points.Length - 1);
                float v = 1 - Mathf.Abs(2 * completionPercent - 1);
                uvs[vertIndex] = new Vector2(0, v);
                uvs[vertIndex + 1] = new Vector2(0.1f, v);
                uvs[vertIndex + 2] = new Vector2(0.15f, v);
                uvs[vertIndex + 3] = new Vector2(0.25f, v);
                uvs[vertIndex + 4] = new Vector2(0.75f, v);
                uvs[vertIndex + 5] = new Vector2(0.85f, v);
                uvs[vertIndex + 6] = new Vector2(0.9f, v);
                uvs[vertIndex + 7] = new Vector2(1, v);


                tris[triIndex] = vertIndex;
                tris[triIndex + 1] = (vertIndex + 8) % verts.Length;
                tris[triIndex + 2] = vertIndex + 1;

                tris[triIndex + 3] = vertIndex + 1;
                tris[triIndex + 4] = (vertIndex + 9) % verts.Length;
                tris[triIndex + 5] = vertIndex + 2;

                tris[triIndex + 6] = vertIndex + 2;
                tris[triIndex + 7] = (vertIndex + 10) % verts.Length;
                tris[triIndex + 8] = vertIndex + 3;

                tris[triIndex + 9] = vertIndex + 3;
                tris[triIndex + 10] = (vertIndex + 11) % verts.Length;
                tris[triIndex + 11] = vertIndex + 4;

                tris[triIndex + 12] = vertIndex + 4;
                tris[triIndex + 13] = (vertIndex + 12) % verts.Length;
                tris[triIndex + 14] = vertIndex + 5;

                tris[triIndex + 15] = vertIndex + 5;
                tris[triIndex + 16] = (vertIndex + 13) % verts.Length;
                tris[triIndex + 17] = vertIndex + 6;

                tris[triIndex + 18] = vertIndex + 6;
                tris[triIndex + 19] = (vertIndex + 14) % verts.Length;
                tris[triIndex + 20] = vertIndex + 7;


                tris[triIndex + 21] = vertIndex + 1;
                tris[triIndex + 22] = (vertIndex + 8) % verts.Length;
                tris[triIndex + 23] = (vertIndex + 9) % verts.Length;

                tris[triIndex + 24] = vertIndex + 2;
                tris[triIndex + 25] = (vertIndex + 9) % verts.Length;
                tris[triIndex + 26] = (vertIndex + 10) % verts.Length;

                tris[triIndex + 27] = vertIndex + 3;
                tris[triIndex + 28] = (vertIndex + 10) % verts.Length;
                tris[triIndex + 29] = (vertIndex + 11) % verts.Length;

                tris[triIndex + 30] = vertIndex + 4;
                tris[triIndex + 31] = (vertIndex + 11) % verts.Length;
                tris[triIndex + 32] = (vertIndex + 12) % verts.Length;

                tris[triIndex + 33] = vertIndex + 5;
                tris[triIndex + 34] = (vertIndex + 12) % verts.Length;
                tris[triIndex + 35] = (vertIndex + 13) % verts.Length;

                tris[triIndex + 36] = vertIndex + 6;
                tris[triIndex + 37] = (vertIndex + 13) % verts.Length;
                tris[triIndex + 38] = (vertIndex + 14) % verts.Length;

                tris[triIndex + 39] = vertIndex + 7;
                tris[triIndex + 40] = (vertIndex + 14) % verts.Length;
                tris[triIndex + 41] = (vertIndex + 15) % verts.Length;

                pathDist += distanceToNextPoint[i];
                vertIndex += 8;
                triIndex += 42;
            }

            Mesh mesh = new Mesh();
            mesh.vertices = verts;
            mesh.triangles = tris;
            mesh.uv = uvs;
            mesh.RecalculateNormals();

            return mesh;

        }
    }
}