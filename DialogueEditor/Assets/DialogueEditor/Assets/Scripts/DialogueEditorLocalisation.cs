using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using DialogueEditor;

[CreateAssetMenu(fileName = "DialogueEditorLocalisation", menuName ="DialogueEditor/Localisation")]
public class DialogueEditorLocalisation : ScriptableObject
{
    private static DialogueEditorLocalisation _instance = null;

    public static DialogueEditorLocalisation Instance
    {
        get
        {
            if (_instance == null)
            {
                DialogueEditorLocalisation[] locs = Resources.FindObjectsOfTypeAll<DialogueEditorLocalisation>();
                if (locs.Length > 0)
                    _instance = locs[0];
            }
            return _instance;
        }
    }


    [SerializeField]
    public LocalisationDatabase Database { get; private set; }

    public void CreateDatabase()
    {
        Database = new LocalisationDatabase();
    }
}
