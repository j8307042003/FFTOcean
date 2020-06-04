using System.Collections;
using UnityEditor;
using System.Collections.Generic;
using UnityEngine;

[CustomEditor(typeof(Gerstner))]
public class GerstnerEditor : Editor {

    public override void OnInspectorGUI()
    {
        EditorGUI.BeginChangeCheck();
        DrawDefaultInspector();
        if ( EditorGUI.EndChangeCheck())
        {
            ((Gerstner)target).SetWaveParameter();
        }

        if (GUILayout.Button("Generate Mesh", GUILayout.Width(Screen.width * 0.8f), GUILayout.Height(50)))
        {
            ((Gerstner)target).SetMesh();
        }
        if (GUILayout.Button("Add Wave", GUILayout.Width(Screen.width * 0.8f), GUILayout.Height(50)))
        {
            ((Gerstner)target).SetMesh();
        }

    }

}
