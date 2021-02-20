using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace DialogueEditor
{
    [System.Serializable]
    [DataContract]
    public class LocalisationDatabase
    {
        public LocalisationDatabase()
        {
            AddLanguage(SystemLanguage.English);
        }

        public int GetLocalisationEntryCount { get { return m_LocalisationEntries.Count; } }
        public List<SystemLanguage> GetSupportedLanguages { get { return m_SupportedLanguages; } }

        [DataMember]
        private List<LocaleEntry> m_LocalisationEntries = new List<LocaleEntry>();
        [DataMember]
        private List<UnityEngine.SystemLanguage> m_SupportedLanguages = new List<SystemLanguage>();

        // ---

        public LocaleEntry GetEntryByID(string id)
        {
            for (int i = 0; i < m_LocalisationEntries.Count; i++)
            {
                if (m_LocalisationEntries[i].ID == id)
                {
                    return m_LocalisationEntries[i];
                }
            }

            return null;
        }

        public LocaleEntry GetEntryByIndex(int index)
        {
            int i = Mathf.Clamp(index, 0, GetLocalisationEntryCount - 1);
            return m_LocalisationEntries[i];
        }

        public void DeleteEntry(int index)
        {
            int i = Mathf.Clamp(index, 0, GetLocalisationEntryCount - 1);
            m_LocalisationEntries.RemoveAt(i);
        }

        public bool IsLanguageSupported(SystemLanguage lang)
        {
            return m_SupportedLanguages.Contains(lang);
        }

        public void AddLanguage(SystemLanguage lang)
        {
            m_SupportedLanguages.Add(lang);

            // For each entry, add a 
            for (int i = 0; i < m_LocalisationEntries.Count; i++)
            {
                if (!m_LocalisationEntries[i].HasLanguage(lang))
                {
                    m_LocalisationEntries[i].AddLanguage(lang);
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

            m_SupportedLanguages.Remove(lang);
        }

        public void AddNewEntry(string id, string englishText)
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
            newEntry.SetLanguageText(SystemLanguage.English, englishText); 
            m_LocalisationEntries.Add(newEntry);
        }

        public bool DoesIDExist(string id)
        {
            for (int i = 0; i < m_LocalisationEntries.Count; i++)
            {
                if (m_LocalisationEntries[i].ID == id)
                {
                    return true;
                }
            }

            return false;
        }
    }


    [System.Serializable]
    [DataContract]
    public class LocaleEntry
    {
        public LocaleEntry(string id)
        {
            ID = id;
            m_LanguageData = new List<LanguageData>();
        }

        [DataMember]
        public string ID { get; private set; }
        [DataMember]
        private List<LanguageData> m_LanguageData = new List<LanguageData>();

        // ----

        public void SetLanguageText(SystemLanguage lang, string txt)
        {
            for (int i = 0; i < m_LanguageData.Count; i++)
            {
                if (m_LanguageData[i].Language == lang)
                {
                    m_LanguageData[i].SetText(txt);
                    return;
                }
            }

            m_LanguageData.Add(new LanguageData(lang, txt));
        }

        public void AddLanguage(SystemLanguage lang)
        {
            if (HasLanguage(lang))
            {
                return;
            }

            m_LanguageData.Add(new LanguageData(lang, ""));
        }

        public bool HasLanguage(SystemLanguage lang)
        {
            for (int i = 0; i < m_LanguageData.Count; i++)
            {
                if (m_LanguageData[i].Language == lang)
                {
                    return true;
                }
            }

            return false;
        }

        public string GetLanguageText(UnityEngine.SystemLanguage lang)
        {
            for (int i = 0; i < m_LanguageData.Count; i++)
            {
                if (m_LanguageData[i].Language == lang)
                {
                    return m_LanguageData[i].Text;
                }
            }

            return "";
        }
    }


    [System.Serializable]
    [DataContract]
    public class LanguageData
    {
        public LanguageData(UnityEngine.SystemLanguage lang, string txt)
        {
            Language = lang;
            LanguageAsString = lang.ToString();
            Text = txt;
        }

        // ----

        [DataMember]
        public UnityEngine.SystemLanguage Language { get; private set; }
        [DataMember]
        public string LanguageAsString { get; private set; }
        [DataMember]
        public string Text { get; private set; }

        public void SetText(string txt)
        {
            Text = txt;
        }
    }
}