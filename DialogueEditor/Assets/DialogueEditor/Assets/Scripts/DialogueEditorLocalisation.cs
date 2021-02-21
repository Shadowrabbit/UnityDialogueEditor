using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using DialogueEditor;

[CreateAssetMenu(fileName = "DialogueEditorLocalisation", menuName ="DialogueEditor/Localisation")]
[System.Serializable]
public class DialogueEditorLocalisation : ScriptableObject
{
    [SerializeField]
    public LocalisationDatabase Database;

    public void CreateDatabase()
    {
        Database = new LocalisationDatabase();
    }
}
