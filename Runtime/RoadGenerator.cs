using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public class RoadGenerator : MonoBehaviour
{
    [SerializeField]
    private bool updateRunTime;
    [SerializeField]
    public bool updateEditor;

    [Header("Track Settings")]
    [SerializeField]
    [Tooltip("The list of points the road should follow, the Last one is the finish")]
    private List<RoadPointController> roadPoints = new List<RoadPointController>();
    [HideInInspector]
    public List<RoadPoint> newroadPoints = new List<RoadPoint>();
    [SerializeField]
    [Range(1, 90)]
    private float maxAngleShift = 15f;
    [SerializeField]
    [Range(1, 10)]
    private int widthResolution = 1;
    [SerializeField]
    private float maxLengthSegment = 10f;
    [SerializeField]
    [Range(0, 0.5f)]
    private float otherPointWitdhImpact = 0.3f;
    [SerializeField]
    private bool looping;
    [SerializeField]
    private float textureTiling = 1f;

    [Header("Extra Objects")]
    [SerializeField]
    private bool updateTalud;
    [SerializeField]
    private CreateTalud[] createTalud = new CreateTalud[0];
    [SerializeField]
    private bool updateBorder;
    [SerializeField]
    private CreateBorder[] createBorders;
    [SerializeField]
    private bool updateMiniMapRoad;
    [SerializeField]
    private CreateMiniMapRoad createMiniMapRoad;

    float pathLength = 1;
    private Mesh mesh;
    private MeshFilter meshFilter;
    private MeshCollider meshCollider;
    private MeshRenderer meshRenderer;

    // Start is called before the first frame update
    void Start()
    {
        //UpdateRoad();
    }

    // Update is called once per frame
    void Update()
    {
        if (updateRunTime) UpdateRoad();
    }

    public void UpdateRoad()
    {
        newroadPoints = createRoadPoints(roadPoints.ToArray());

        if (!meshFilter) meshFilter = GetComponent<MeshFilter>();
        meshFilter.mesh = CreateBasicRoadMesh(newroadPoints.ToArray());
        meshFilter.sharedMesh.RecalculateBounds();

        if(!meshCollider) meshCollider = gameObject.GetComponent<MeshCollider>();
        meshCollider.sharedMesh = meshFilter.sharedMesh;

        if (!meshRenderer) meshRenderer = GetComponent<MeshRenderer>();
        int textureRepeat = Mathf.RoundToInt(textureTiling * pathLength * 0.01f);
        meshRenderer.sharedMaterial.mainTextureScale = new Vector2(1, textureRepeat);

        if (updateTalud && createTalud.Length > 0) foreach (var talud in createTalud) talud.UpdateRoad(newroadPoints.ToArray(), looping);
        if (updateBorder && createBorders.Length > 0) foreach (var border in createBorders) border.UpdateRoad(newroadPoints.ToArray(), looping);
        if (updateMiniMapRoad && createMiniMapRoad) createMiniMapRoad.UpdateRoad(newroadPoints.ToArray(), looping);

    }

    private void OnDrawGizmos()
    {
        if (roadPoints.Count > 0)
        {
            if (updateEditor) Gizmos.DrawLine(looping ? roadPoints[roadPoints.Count - 1].transform.position : transform.position, roadPoints[0].transform.position);
            roadPoints[0].drawGizmos = updateEditor;

            for (int i = 1; i < roadPoints.Count; i++)
            {
                if (updateEditor) Gizmos.DrawLine(roadPoints[i - 1].transform.position, roadPoints[i].transform.position);
                roadPoints[i].drawGizmos = updateEditor;

            }
        }

        if (updateEditor)
        {
            if (newroadPoints.Count > 0)
            {
                Gizmos.DrawLine(looping ? newroadPoints[newroadPoints.Count - 1].position : transform.position, newroadPoints[0].position);

                for (int i = 1; i < newroadPoints.Count; i++)
                {
                    Gizmos.color = Color.blue;
                    Gizmos.DrawLine(newroadPoints[i - 1].position, newroadPoints[i].position);
                    Gizmos.DrawSphere(newroadPoints[i].Pos(), 0.5f);
                }
            }
        }


    }
    List<RoadPoint> addroadPoints(RoadPointController[] oldPoints)
    {
        List<RoadPoint> points = new List<RoadPoint>();

        foreach (var point in oldPoints)
        {
            points.Add(point.roadPoint);
        }

        return points;
    }

    public void UpdateCornerPoints()
    {
        newroadPoints = createRoadPoints(roadPoints.ToArray());
    }

    List<RoadPoint> createRoadPoints(RoadPointController[] oldPoints)
    {
        List<RoadPoint> points = new List<RoadPoint>();

        foreach (var point in oldPoints)
        {
            point.UpdatePos();
            points.Add(point.roadPoint);
        }

        List<RoadPoint> newPoints = new List<RoadPoint>();
        RoadPoint newPoint = new RoadPoint();

        if (!looping) newPoints.Add(points[0]);

        for (int i = 0; i < points.Count; i++)
        {

            if (looping || (i > 0 && i < points.Count - 1))
            {
                //get the indexes of the neighbouring points
                int lastIndex = ((i - 1) % points.Count + points.Count) % points.Count;
                int nextIndex = (i + 1) % points.Count;

                float totalAngleShift = Vector3.Angle(points[lastIndex].Pos() - points[i].Pos(), points[nextIndex].Pos() - points[i].Pos()); // the angle between the last and next point
                float cornerAngle = 180 - totalAngleShift; // the total radial angle of the corner 
                float startCornerOffset = points[i].cornerRadius / Mathf.Tan(totalAngleShift / 2f * Mathf.Deg2Rad); // the distance from the cornermpoint to start the curve

                // add the first point
                Vector3 firstExtraPoint = points[i].Pos() + (points[lastIndex].Pos() - points[i].Pos()).normalized * startCornerOffset;
                newPoint = new RoadPoint();
                newPoint.SetFakePoint(points[i], firstExtraPoint);
                newPoint.roadWidth = points[i].roadWidth * (1 - otherPointWitdhImpact) + points[lastIndex].roadWidth * otherPointWitdhImpact;
                newPoints.Add(newPoint);

                Vector3 lastExtraPoint = points[i].Pos() + (points[nextIndex].Pos() - points[i].Pos()).normalized * startCornerOffset;

                //add the points in between
                int nrOfExtaCornerPoints = (maxAngleShift > 0 && maxAngleShift < cornerAngle) ? (Mathf.RoundToInt(cornerAngle / maxAngleShift) - 1) : 0; // the amount of extra steps in the curve exlcuding the first and last point
                float angleIncrement = nrOfExtaCornerPoints > 0 ? cornerAngle / ((float)nrOfExtaCornerPoints) : 0;
                Vector3 forward = points[i].Pos() - firstExtraPoint;
                float sign = Mathf.Sign(Vector3.SignedAngle(firstExtraPoint - points[i].Pos(), lastExtraPoint - points[i].Pos(), Vector3.up));
                Vector3 cornerPivot = firstExtraPoint + new Vector3(-forward.z, 0, forward.x).normalized * points[i].cornerRadius * sign;

                oldPoints[i].roadPoint.cornerPivot = cornerPivot;
                oldPoints[i].roadPoint.startCornerPoint = firstExtraPoint;
                oldPoints[i].roadPoint.endCornerPoint = lastExtraPoint;
                //Debug.Log("pointnr: " + i + ", total angle: "+ cornerAngle +", angle increment:" + angleIncrement+ ", nrofpoints: " + nrOfExtaCornerPoints);

                for (int j = 0; j < nrOfExtaCornerPoints - 1; j++)
                {
                    Vector3 extraPoint = Vector3.zero;
                    float parameter = angleIncrement / cornerAngle * (j + 1);

                    switch (points[i].cornerType)
                    {
                        case CornerType.arc:
                            extraPoint = Vector3.Slerp(firstExtraPoint - cornerPivot, lastExtraPoint - cornerPivot, parameter) + cornerPivot; //Quaternion.Euler(0, 0, cornerAngle / nrOfExtaCornerPoints * j) * (firstExtraPoint - cornerPivot) + cornerPivot;
                            break;
                        case CornerType.bezier2:
                            extraPoint = Bezier2(firstExtraPoint, points[i].Pos(), lastExtraPoint, parameter); //Quaternion.Euler(0, 0, cornerAngle / nrOfExtaCornerPoints * j) * (firstExtraPoint - cornerPivot) + cornerPivot;
                            break;

                        default:
                            break;
                    }


                    RoadPoint newnewPoint = new RoadPoint();
                    newnewPoint.SetFakePoint(points[i], extraPoint);
                    float newWidth = (j < nrOfExtaCornerPoints / 2) ?
                        Mathf.Lerp(points[i].roadWidth * (1 - otherPointWitdhImpact) + points[lastIndex].roadWidth * otherPointWitdhImpact, points[i].roadWidth, parameter * 2) :
                        Mathf.Lerp(points[i].roadWidth, points[i].roadWidth * (1 - otherPointWitdhImpact) + points[nextIndex].roadWidth * otherPointWitdhImpact, (parameter - 0.5f) * 2);
                    newnewPoint.roadWidth = newWidth;
                    float newCurveImpact = 1 - Mathf.Abs(2 * j / ((float)nrOfExtaCornerPoints - 1) - 1);
                    newnewPoint.curveImpact = newCurveImpact;

                    newPoints.Add(newnewPoint);
                }


                // add the last point

                newPoint = new RoadPoint();
                newPoint.SetFakePoint(points[i], lastExtraPoint);
                newPoint.roadWidth = points[i].roadWidth * (1 - otherPointWitdhImpact) + points[nextIndex].roadWidth * otherPointWitdhImpact;
                newPoints.Add(newPoint);

                //Debug.DrawLine(firstExtraPoint, cornerPivot, Color.blue);
                //Debug.DrawLine(lastExtraPoint, cornerPivot, Color.blue);

                //Vector3 extraPos = points[i].cornerRadius
            }
        }

        if (!looping) newPoints.Add(points[points.Count - 1]);

        for (int i = 0; i < newPoints.Count - (looping ? 0 : 1); i++)
        {
            float distanceBtwnPoints = Vector3.Distance(newPoints[i].Pos(), newPoints[(i + 1) % newPoints.Count].Pos());
            if (distanceBtwnPoints > maxLengthSegment)
            {
                int segmentAmount = Mathf.FloorToInt(distanceBtwnPoints / maxLengthSegment);
                RoadPoint firstPoint = newPoints[i];
                RoadPoint lastPoint = newPoints[(i + 1) % newPoints.Count];
                for (int j = 0; j < segmentAmount; j++)
                {
                    RoadPoint extraPoint = new RoadPoint();
                    extraPoint.SetInterpolatePoint(firstPoint, lastPoint, (j + 1) / (float)(segmentAmount + 1));
                    newPoints.Insert(i + 1, extraPoint);
                    i++;
                }
            }
        }

        return newPoints;
    }

    //generates a mesh
    public Mesh CreateBasicRoadMesh(RoadPoint[] points)
    {
        if (widthResolution < 1) widthResolution = 1;

        Vector3[] verts = new Vector3[points.Length * (1 + widthResolution)];
        Vector2[] uvs = new Vector2[verts.Length];
        int[] tris = new int[(points.Length - (looping ? 0 : 1)) * (1 + widthResolution) * 6];
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
            float completionPercent = i / (float)(points.Length);
            float v = 1 - Mathf.Abs(2 * completionPercent - 1);

            for (int j = 0; j < (1 + widthResolution); j++)
            {
                verts[vertIndex + j] = points[i].Pos() - left * points[i].roadWidth * (j / (float)(widthResolution) - 0.5f) + up * points[i].curveImpact * points[i].cornerProfile.Evaluate(j / (float)(widthResolution));
                uvs[vertIndex + j] = new Vector2(j / (float)(widthResolution), v);

                if (i < points.Length - (looping ? 0 : 1))
                {
                    if (j < widthResolution)
                    {

                        tris[triIndex + 6 * j] = vertIndex + j;
                        tris[triIndex + 1 + 6 * j] = (vertIndex + j + (1 + widthResolution)) % verts.Length;
                        tris[triIndex + 2 + 6 * j] = vertIndex + j + 1;

                        tris[triIndex + 3 + 6 * j] = vertIndex + j + 1;
                        tris[triIndex + 4 + 6 * j] = (vertIndex + j + (1 + widthResolution)) % verts.Length;
                        tris[triIndex + 5 + 6 * j] = (vertIndex + j + (1 + widthResolution) + 1) % verts.Length;


                    }
                }

            }

            if (i < points.Length - 1) pathDist += Vector3.Distance(points[i].Pos(), points[i + 1].Pos());
            vertIndex += 1 + widthResolution;
            triIndex += (1 + widthResolution) * 6;

            //Debug.DrawLine(points[i].Pos(), points[i].Pos() + left * points[i].roadWidth * 1.5f, Color.green);
        }

        mesh = new Mesh();
        mesh.vertices = verts;
        mesh.triangles = tris;
        mesh.uv = uvs;
        mesh.RecalculateNormals();
        mesh.name = "track";

        return mesh;

    }

    public Vector3 Bezier2(Vector3 s, Vector3 p, Vector3 e, float t)
    {
        float rt = 1 - t;
        return rt * rt * s + 2 * rt * t * p + t * t * e;
    }



}