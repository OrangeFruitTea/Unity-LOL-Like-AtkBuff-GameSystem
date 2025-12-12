using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

[ExecuteInEditMode]
public class BuffDebugger : EditorWindow
{
    [SerializeField]
    private GameObject TargetPlayer;

    [MenuItem("Tools/Buff调试工具")]
    public static void ShowWindow()
    {
        GetWindow<BuffDebugger>("Buff调试工具"); 
    }

    private void OnGUI()
    {
        GUILayout.Label("Buff调试控制",EditorStyles.boldLabel);
        EditorGUILayout.Space();
        TargetPlayer = (GameObject)EditorGUILayout.ObjectField(
            "目标实体",
            TargetPlayer,
            typeof(GameObject),
            true);
        
    }
}
