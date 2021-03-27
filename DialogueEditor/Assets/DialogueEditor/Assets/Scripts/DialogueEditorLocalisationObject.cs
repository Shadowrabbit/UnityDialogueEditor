using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using DialogueEditor;

[CreateAssetMenu(fileName = "DialogueEditorLocalisation", menuName ="DialogueEditor/Localisation")]
[System.Serializable]
public class DialogueEditorLocalisationObject : ScriptableObject
{
    [System.Serializable]
    public class LanguageFonts
    {
        public SystemLanguage Language;
        public TMPro.TMP_FontAsset Font;
    }

    [SerializeField]
    public LocalisationDatabase Database;

    [SerializeField]
    public List<LanguageFonts> LanguageFontList = new List<LanguageFonts>();

    public void CreateDatabase()
    {
        Database = new LocalisationDatabase();
    }
}
