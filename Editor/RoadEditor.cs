using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(RoadGenerator))]
public class RoadEditor : Editor
{
    RoadGenerator roadGenerator;

    private void OnSceneGUI()
    {
        if (roadGenerator.updateEditor && Event.current.type == EventType.Repaint)
        {
            //Debug.Log("updating");
            roadGenerator.UpdateCornerPoints();
            roadGenerator.UpdateRoad();
        }
    }

    private void OnEnable()
    {
        roadGenerator = (RoadGenerator)target;
    }
}

