using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JelleVer.TrackGenerator
{
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    [RequireComponent(typeof(MeshCollider))]

    public class FenceGenerator : MonoBehaviour
    {
        public TrackGenerator trackGenerator;

        [Header("Track Shape Settings")]
        public float FenceOffset = 1;
        public float FenceWidth = 1f;
        public float FenceHeight = 1f;


        [Header("Visual Settings")]
        public float tiling = 1;

        public float updateInteral;



        private float timePassed;

        // Start is called before the first frame update
        void Start()
        {
            UpdateRoad();

        }

        // Update is called once per frame
        void Update()
        {

            if (timePassed > updateInteral)
            {

                UpdateRoad();
                //CreateSpheres();
                timePassed = 0f;
            }
            timePassed += Time.deltaTime;

        }

        public void UpdateRoad()
        {
            MeshFilter meshFilter = GetComponent<MeshFilter>();
            meshFilter.mesh = CreateFenceMesh(trackGenerator.pointsOnCurve);
            meshFilter.mesh.RecalculateBounds();
            MeshCollider meshCollider = gameObject.GetComponent<MeshCollider>();
            meshCollider.sharedMesh = meshFilter.mesh;

            int textureRepeat = Mathf.RoundToInt(tiling * trackGenerator.pathLength * 0.05f);
            GetComponent<MeshRenderer>().sharedMaterial.mainTextureScale = new Vector2(1, textureRepeat);
        }

        //generates a mesh
        public Mesh CreateFenceMesh(Vector3[] points)
        {
            Vector3[] verts = new Vector3[points.Length * 3];
            Vector2[] uvs = new Vector2[verts.Length];
            int[] tris = new int[12 * points.Length];
            int vertIndex = 0;
            int triIndex = 0;

            float pathDist = 0f;

            for (int i = 0; i < points.Length; i++)
            {


                Vector3 left = Vector3.Normalize(-points[i] + trackGenerator.worldOffset);

                verts[vertIndex] = points[i] + left * (FenceOffset + FenceWidth);
                verts[vertIndex + 1] = points[i] + left * (FenceOffset + FenceWidth / 2f) + Vector3.up * FenceHeight;
                verts[vertIndex + 2] = points[i] + left * (FenceOffset);



                float completionPercent = pathDist / trackGenerator.pathLength; //i / (float)(points.Length - 1);
                float v = 1 - Mathf.Abs(2 * completionPercent - 1);
                uvs[vertIndex] = new Vector2(0, v);
                uvs[vertIndex + 1] = new Vector2(1, v);
                uvs[vertIndex + 2] = new Vector2(0, v);



                tris[triIndex] = vertIndex;
                tris[triIndex + 1] = (vertIndex + 3) % verts.Length;
                tris[triIndex + 2] = vertIndex + 1;

                tris[triIndex + 3] = vertIndex + 1;
                tris[triIndex + 4] = (vertIndex + 3) % verts.Length;
                tris[triIndex + 5] = (vertIndex + 4) % verts.Length;

                tris[triIndex + 6] = vertIndex + 1;
                tris[triIndex + 7] = (vertIndex + 4) % verts.Length;
                tris[triIndex + 8] = vertIndex + 2;

                tris[triIndex + 9] = vertIndex + 2;
                tris[triIndex + 10] = (vertIndex + 4) % verts.Length;
                tris[triIndex + 11] = (vertIndex + 5) % verts.Length;

                pathDist += trackGenerator.distanceToNextPoint[i];
                vertIndex += 3;
                triIndex += 12;
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