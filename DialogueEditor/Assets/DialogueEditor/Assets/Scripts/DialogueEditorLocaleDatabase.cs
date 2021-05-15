using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace DialogueEditor
{
    [DataContract]
    [System.Serializable]
    public class LocalisationDatabase
    {
        public LocalisationDatabase()
        {
            AddLanguage(SystemLanguage.English);
        }

        public int GetLocalisationEntryCount { get { return m_localisationEntries.Count; } }
        public List<SystemLanguage> GetSupportedLanguages { get { return m_supportedLanguages; } }

        [DataMember]
        [SerializeField]
        private List<LocaleEntry> m_localisationEntries = new List<LocaleEntry>();
        [DataMember]
        [SerializeField]
        private List<UnityEngine.SystemLanguage> m_supportedLanguages = new List<SystemLanguage>();

        // ---

        public string GetTranslation(string id, SystemLanguage language)
        {
            LocaleEntry entry = GetEntryByID(id);
            if (entry != null)
            {
                return entry.GetLanguageText(language);
            }
            return "";
        }

        public LocaleEntry GetEntryByID(string id)
        {
            for (int i = 0; i < m_localisationEntries.Count; i++)
            {
                if (m_localisationEntries[i].ID == id)
                {
                    return m_localisationEntries[i];
                }
            }

            return null;
        }

        public LocaleEntry GetEntryByIndex(int index)
        {
            int i = Mathf.Clamp(index, 0, GetLocalisationEntryCount - 1);
            return m_localisationEntries[i];
        }

        public void DeleteEntryByIndex(int index)
        {
            int i = Mathf.Clamp(index, 0, GetLocalisationEntryCount - 1);
            m_localisationEntries.RemoveAt(i);
        }

        public void DeleteEntryByID(string id)
        {
            for (int i = 0; i < m_localisationEntries.Count; i++)
            {
                if (m_localisationEntries[i].ID == id)
                {
                    m_localisationEntries.RemoveAt(i);
                    return;
                }
            }
        }

        public bool IsLanguageSupported(SystemLanguage lang)
        {
            return m_supportedLanguages.Contains(lang);
        }

        public void AddLanguage(SystemLanguage lang)
        {
            // Add the language to our supported languages list
            if (!IsLanguageSupported(lang))
            {
                m_supportedLanguages.Add(lang);

            }
          
            // For each localisation entry, add the new language
            for (int i = 0; i < m_localisationEntries.Count; i++)
            {
                if (!m_localisationEntries[i].HasLanguage(lang))
                {
                    m_localisationEntries[i].AddLanguage(lang);
                }
            }
        }

        public void RemoveLanguage(SystemLanguage lang)
        {
            // Cannot remove English - requires atleast one Default language. 
            if (lang == SystemLanguage.English)
            {
                return;
            }

            m_supportedLanguages.Remove(lang);
        }

        public void CreateNewEntry(string id, string englishText)
        {
            if (string.IsNullOrEmpty(id))
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(id))
            {
                return;
            }

            if (DoesIDExist(id))
            {
                return;
            }

            LocaleEntry newEntry = new LocaleEntry(id);

            // Set the english text 
            newEntry.SetLanguageText(SystemLanguage.English, englishText); 

            // Add a language entry for each other supported language 
            foreach (SystemLanguage language in m_supportedLanguages)
            {
                if (language == SystemLanguage.English) { continue; }
                newEntry.SetLanguageText(language, "");
            }

            // Add to our entries
            m_localisationEntries.Add(newEntry);
        }

        public bool DoesIDExist(string id)
        {
            for (int i = 0; i < m_localisationEntries.Count; i++)
            {
                if (m_localisationEntries[i].ID == id)
                {
                    return true;
                }
            }

            return false;
        }
    }

    
    [DataContract]
    [System.Serializable]
    public class LocaleEntry
    {
        public LocaleEntry(string id)
        {
            m_id = id;
            m_languageData = new List<LanguageData>();
        }

        public string ID { get { return m_id; } }

        [DataMember]
        [SerializeField]
        private string m_id;
        [DataMember]
        [SerializeField]
        private List<LanguageData> m_languageData = new List<LanguageData>();

        // ----

        public void SetLanguageText(SystemLanguage lang, string txt)
        {
            for (int i = 0; i < m_languageData.Count; i++)
            {
                if (m_languageData[i].Language == lang)
                {
                    m_languageData[i].SetText(txt);
                    return;
                }
            }

            // If we get to here, the language was not present, hence we need to add it. 
            AddLanguage(lang);
        }

        public void AddLanguage(SystemLanguage lang)
        {
            if (HasLanguage(lang))
            {
                return;
            }

            m_languageData.Add(new LanguageData(lang, ""));
        }

        public bool HasLanguage(SystemLanguage lang)
        {
            for (int i = 0; i < m_languageData.Count; i++)
            {
                if (m_languageData[i].Language == lang)
                {
                    return true;
                }
            }

            return false;
        }

        public string GetLanguageText(UnityEngine.SystemLanguage lang)
        {
            for (int i = 0; i < m_languageData.Count; i++)
            {
                if (m_languageData[i].Language == lang)
                {
                    return m_languageData[i].Text;
                }
            }

            return "";
        }

        public LanguageData GetLanguageData(SystemLanguage lang)
        {
            for (int i = 0; i < m_languageData.Count; i++)
            {
                if (m_languageData[i].Language == lang)
                {
                    return m_languageData[i];
                }
            }

            return null;
        }
    }


    [DataContract]
    [System.Serializable]
    public class LanguageData
    {
        public LanguageData(UnityEngine.SystemLanguage lang, string txt)
        {
            m_language = lang;
            m_languageAsString = lang.ToString();
            m_text = txt;
        }

        // ----

        public SystemLanguage Language { get { return m_language; } }
        public string Text { get { return m_text; } }

        [DataMember]
        [SerializeField]
        public UnityEngine.SystemLanguage m_language;
        [DataMember]
        [SerializeField]
        public string m_languageAsString;
        [DataMember]
        [SerializeField]
        public string m_text;

        public void SetText(string txt)
        {
            m_text = txt;
        }
    }
}