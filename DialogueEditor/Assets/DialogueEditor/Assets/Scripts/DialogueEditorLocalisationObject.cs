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
    public class LanguageFont
    {
        public SystemLanguage Language;
        public TMPro.TMP_FontAsset Font;
    }

    [SerializeField]
    public LocalisationDatabase Database;

    [SerializeField]
    public List<LanguageFont> LanguageFontList = new List<LanguageFont>();


    // ----
    // Init

    public void CreateDatabase()
    {
        Database = new LocalisationDatabase();
    }



    // ----
    // Language fonts 

    public TMPro.TMP_FontAsset GetLanguageFont(SystemLanguage language)
    {
        for (int i = 0; i < LanguageFontList.Count; i++)
        {
            if (LanguageFontList[i].Language == language)
                return LanguageFontList[i].Font;
        } 
        return null;
    }

    public void AddLanguageFont(SystemLanguage language)
    {
        if (!HasLanguageFont(language))
        {
            LanguageFont langFont = new LanguageFont();
            langFont.Language = language;
            LanguageFontList.Add(langFont);
        }
    }

    public void RemoveLanguageFont(SystemLanguage language)
    {
        int removeAtIndex = -1;

        for (int i = 0; i < LanguageFontList.Count; i++)
        {
            if (LanguageFontList[i].Language == language)
            {
                removeAtIndex = i;
                break;
            }
        }

        if (removeAtIndex >= 0)
        {
            LanguageFontList.RemoveAt(removeAtIndex);
        }
    }

    public bool HasLanguageFont(SystemLanguage language)
    {
        for (int i = 0; i < LanguageFontList.Count; i++)
        {
            if (LanguageFontList[i].Language == language)
                return true;
        }
        return false;
    }
}
