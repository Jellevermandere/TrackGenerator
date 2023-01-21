using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum CornerType { arc, bezier2 }

public class RoadPointController : MonoBehaviour
{
    [Space(10)]
    [Header("BankAngle is handeled by the Z-scale (1 is flat)")]
    [Space(-10)]
    [Header("CornerRaduis is handeled by the Y-scale")]
    [Space(-10)]
    [Header("Width is handeled by the X-scale")]
    [Space(-10)]
    [Header("Position is handled by the transform position")]


    public RoadPoint roadPoint;
    public bool drawGizmos = true;

    [SerializeField]
    private float gizmoRadius = 1f;

    private void OnDrawGizmos()
    {
        if (drawGizmos)
        {
            Gizmos.DrawSphere(transform.position, gizmoRadius);
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(roadPoint.cornerPivot, gizmoRadius / 2f);
            Gizmos.DrawLine(roadPoint.cornerPivot, roadPoint.startCornerPoint);
            Gizmos.DrawLine(roadPoint.cornerPivot, roadPoint.endCornerPoint);
        }

    }

    public void UpdatePos()
    {
        roadPoint.position = transform.position;
        roadPoint.cornerRadius = transform.localScale.y;
        roadPoint.roadWidth = transform.localScale.x;
        roadPoint.bankAngle = transform.localScale.z - 1;
    }
}


[System.Serializable]
public class RoadPoint
{
    [HideInInspector]
    public float cornerRadius = 1f;
    [HideInInspector]
    public float roadWidth = 1f;
    [HideInInspector]
    public Vector3 position = Vector3.zero;
    [HideInInspector]
    public Vector3 cornerPivot = Vector3.zero;
    [HideInInspector]
    public float bankAngle = 0f;
    public CornerType cornerType = CornerType.bezier2;
    public AnimationCurve cornerProfile = AnimationCurve.Linear(0, 0, 1, 0);
    [HideInInspector]
    public float curveImpact = 0f;
    [HideInInspector]
    public Vector3 startCornerPoint = Vector3.zero;
    [HideInInspector]
    public Vector3 endCornerPoint = Vector3.zero;

    public Vector3 Pos()
    {
        return position;

    }

    public void SetFakePoint(RoadPoint other, Vector3 pos)
    {
        cornerRadius = other.cornerRadius;
        roadWidth = other.roadWidth;
        position = pos;
        cornerPivot = other.cornerPivot;
        bankAngle = other.bankAngle;
        cornerType = other.cornerType;
        cornerProfile = other.cornerProfile;
    }

    public void SetInterpolatePoint(RoadPoint firstPoint, RoadPoint lastPoint, float percent)
    {
        cornerRadius = Mathf.Lerp(firstPoint.cornerRadius, lastPoint.cornerRadius, percent);
        roadWidth = Mathf.Lerp(firstPoint.roadWidth, lastPoint.roadWidth, percent);
        position = Vector3.Lerp(firstPoint.Pos(), lastPoint.Pos(), percent);
        cornerPivot = Vector3.Lerp(firstPoint.cornerPivot, lastPoint.cornerPivot, percent);
        bankAngle = Mathf.Lerp(firstPoint.bankAngle, lastPoint.bankAngle, percent);
        cornerType = firstPoint.cornerType;
        cornerProfile = firstPoint.cornerProfile;
    }

}