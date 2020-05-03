using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace DialogueEditor
{
    [CustomEditor(typeof(NPCConversation))]
    public class NPCConversationEditor : Editor
    {
        private static GUIStyle s;

        void OnEnable()
        {
            s = new GUIStyle();
            s.alignment = TextAnchor.LowerCenter;
            s.fontStyle = FontStyle.Bold;
            s.wordWrap = true;
            s.fontSize = 16;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.BeginVertical();
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(serializedObject.targetObject.name + " - Conversation.", s);
            EditorGUILayout.EndVertical();
            serializedObject.ApplyModifiedProperties();
        }
    }

    [CustomEditor(typeof(NodeEventHolder))]
    public class NodeEventHolderEditor : Editor
    {
        private static GUIStyle s;
        private NodeEventHolder n;

        void OnEnable()
        {
            s = new GUIStyle();
            s.alignment = TextAnchor.LowerCenter;
            s.wordWrap = true;
            s.fontSize = 10;

            n = (base.target as NodeEventHolder);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField("Node " + n.NodeID + " - EventInformation.", s);
            EditorGUILayout.EndVertical();
            serializedObject.ApplyModifiedProperties();
        }
    }
}