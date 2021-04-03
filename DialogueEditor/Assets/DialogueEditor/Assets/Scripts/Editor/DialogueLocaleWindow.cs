using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

using UnityEngine.Serialization;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace DialogueEditor
{
    public class DialogueLocaleWindow : EditorWindow
    {
        // Consts
        public const float TOOLBAR_HEIGHT = 17;
        private const string WINDOW_NAME = "DIALOGUE_LOCALE_WINDOW";
        private const string CONTROL_NAME = "DEFAULT_LOCALE_CONTROL";
        private const string UNAVAILABLE_DURING_PLAY_TEXT = "Dialogue Editor unavaiable during play mode.";
        private const int MAX_PER_PAGE = 10;
        private const int SMALL_PADDING = 5;
        private const int LARGE_PADDING = 15;
        private const float SIDE_PADDING = 5;


        // -- Private variables -- :     

        // Misc
        private DialogueEditorLocalisationObject CurrentAsset; // The Localisation scriptable object that is currently being viewed/edited
        private List<SystemLanguage> _supportedLanguages = new List<SystemLanguage>();

        // Add new entry
        private string m_newID;
        private string m_newEnglish;

        // Display entries
        private int m_currentEntriesPage;
        private Vector2 m_entriesGridScrollView;

        // Search
        private string m_searchID;
        private string m_searchEnglish;
        private List<LocaleEntry> m_searchResults = new List<LocaleEntry>();
        private int m_currentSearchPage;
        private Vector2 m_searchGridScrollView;        

        // Window
        private Vector2 m_windowScrollView;

        // Styles
        private GUIStyle m_boldStyle;
        private GUIStyle m_wordWrapStyle;
        private GUIStyle m_entryStyle;




        //--------------------------------------
        // Open window
        //--------------------------------------

        [MenuItem("Window/DialogueEditor/Localisation")]
        public static DialogueLocaleWindow ShowWindow()
        {
            return EditorWindow.GetWindow<DialogueLocaleWindow>("Dialogue Editor Locale");
        }

        [UnityEditor.Callbacks.OnOpenAsset(1)]
        public static bool OpenDialogue(int assetInstanceID, int line)
        {
            DialogueEditorLocalisationObject locale = EditorUtility.InstanceIDToObject(assetInstanceID) as DialogueEditorLocalisationObject;

            if (locale != null)
            {
                DialogueLocaleWindow window = ShowWindow();
                window.LoadNewAsset(locale);
                return true;
            }
            return false;
        }




        //--------------------------------------
        // Load New Asset
        //--------------------------------------

        public void LoadNewAsset(DialogueEditorLocalisationObject asset)
        {
            if (Application.isPlaying)
            {
                Log("Load new asset aborted. Will not open assets during play.");
                return;
            }

            CurrentAsset = asset;
        }


        //--------------------------------------
        // OnEnable, OnDisable, OnFocus, LostFocus, 
        // Destroy, SelectionChange, ReloadScripts
        //--------------------------------------

        private void OnEnable()
        {

        }

        private void InitGUIStyles()
        {

        }

        private void OnDisable()
        {

        }

        protected void OnFocus()
        {
            if (Application.isPlaying)
            {
                return;
            }
        }

        protected void OnLostFocus()
        {
            if (Application.isPlaying)
            {
                return;
            }
        }

        protected void OnDestroy()
        {
            if (Application.isPlaying)
            {
                return;
            }
        }

        protected void OnSelectionChange()
        {
            if (Application.isPlaying)
            {
                return;
            }

            // Get asset the user is selecting
            Object newlySelectedAsset = Selection.activeObject;

            // If it's not null
            if (newlySelectedAsset != null)
            {
                DialogueEditorLocalisationObject newLocale = newlySelectedAsset as DialogueEditorLocalisationObject;
                if (newLocale != null)
                {
                    LoadNewAsset(newLocale);
                    Repaint();
                    return;
                }
            }

            CurrentAsset = null;
            Repaint();
        }




        //--------------------------------------
        // Update
        //--------------------------------------

        private void Update()
        {

        }



        //--------------------------------------
        // Draw
        //--------------------------------------

        private void ValidateStyles()
        {
            if (m_boldStyle == null)
            {
                m_boldStyle = new GUIStyle();
            }
            m_boldStyle.fontStyle = FontStyle.Bold;
            m_boldStyle.richText = true;
            if (EditorGUIUtility.isProSkin)
            {
                m_boldStyle.normal.textColor = DialogueEditorUtil.ProSkinTextColour;
            }

            if (m_wordWrapStyle == null)
            {
                m_wordWrapStyle = new GUIStyle();
            }
            m_wordWrapStyle.wordWrap = true;
            if (EditorGUIUtility.isProSkin)
            {
                m_wordWrapStyle.normal.textColor = DialogueEditorUtil.ProSkinTextColour;
            }

            if (m_entryStyle == null)
            {
                m_entryStyle = new GUIStyle();
            }
            m_entryStyle.stretchWidth = false;
        }




        //--------------------------------------
        // Draw
        //--------------------------------------

        private void OnGUI()
        {
            if (Application.isPlaying)
            {
                DrawWindowMessage(UNAVAILABLE_DURING_PLAY_TEXT);
                return;
            }

            if (CurrentAsset == null)
            {
                DrawWindowMessage("No Localisation selected");
                return;
            }

            if (CurrentAsset.Database == null)
            {
                CurrentAsset.CreateDatabase();
            }

            // Validate
            ValidateStyles();

            // Draw
            DrawTitleBar();

            // Entire window scroll view start
            m_windowScrollView = GUILayout.BeginScrollView(m_windowScrollView);
            DrawAddNewEntry();
            DrawCurrentEntries();
            DrawSearchEntries();
            DrawSearchResults();
            DrawLanguages();
            DrawLanguageFonts();
            GUILayout.FlexibleSpace();

            // Entire window scroll view end
            GUILayout.EndScrollView();

            if (GUI.changed)
                Repaint();
        }

        private void DrawTitleBar()
        {
            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            if (GUILayout.Button("Export to JSON", EditorStyles.toolbarButton))
            {
                ExportToJson();
            }
            if (GUILayout.Button("Export to CSV", EditorStyles.toolbarButton))
            {
                ExportToCSV();
            }
            GUILayout.Space(5);
            if (GUILayout.Button("Import from JSON", EditorStyles.toolbarButton))
            {
                LoadFromJSON();
            }
            if (GUILayout.Button("Import from CSV", EditorStyles.toolbarButton))
            {
                LoadFromCSV();
            }
            GUILayout.FlexibleSpace();
            // Reset button
            if (GUILayout.Button("Reset", EditorStyles.toolbarButton))
            {
                // Display an extra confirmation window
                if (EditorUtility.DisplayDialog("Warning", 
                    "Are you sure you want to Reset? This will delete all data. It is recommended to save to Json/CSV first to prevent complete data loss.", 
                    "Confim", 
                    "Cancel"))
                {
                    ResetAll();
                }
            }
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Help", EditorStyles.toolbarButton))
            {
                Application.OpenURL(DialogueEditorUtil.HELP_URL);
            }
            GUILayout.EndHorizontal();
        }

        private void DrawLanguages()
        {
            DrawSectionTitle("Supported languages:", m_boldStyle);

            GUILayout.BeginHorizontal(GUILayout.MaxWidth(this.position.width));

            if (GUILayout.Button("Add Language"))
            {
                GenericMenu menu = new GenericMenu();
                var allLanguages = System.Enum.GetValues(typeof(SystemLanguage));
                var currentLanguages = CurrentAsset.Database.GetSupportedLanguages;

                foreach (SystemLanguage lang in allLanguages)
                {
                    if (!currentLanguages.Contains(lang))
                    {
                        menu.AddItem(new GUIContent(lang.ToString()), false, delegate
                        {
                            CurrentAsset.Database.AddLanguage(lang);
                        });
                    }
                }

                menu.ShowAsContext();
            }

            if (GUILayout.Button("Remove Language"))
            {
                GenericMenu menu = new GenericMenu();
                var currentLanguages = CurrentAsset.Database.GetSupportedLanguages;

                foreach (SystemLanguage lang in currentLanguages)
                {
                    if (lang == SystemLanguage.English) { continue; }

                    menu.AddItem(new GUIContent(lang.ToString()), false, delegate
                    {
                        CurrentAsset.Database.RemoveLanguage(lang);
                    });
                }

                menu.ShowAsContext();
            }

            GUILayout.EndHorizontal();

            _supportedLanguages = CurrentAsset.Database.GetSupportedLanguages;
            _supportedLanguages.Sort();

            string supportedString = "";
            for (int i = 0; i < _supportedLanguages.Count; i++)
            {
                supportedString += _supportedLanguages[i].ToString();
                if (i < _supportedLanguages.Count - 1)
                    supportedString += ", ";
            }

            GUILayout.BeginHorizontal(GUILayout.MaxWidth(this.position.width));
            GUILayout.Space(SIDE_PADDING);
            GUILayout.TextArea("Supported languages: " + supportedString, m_wordWrapStyle);
            GUILayout.EndHorizontal();

            // Draw line
            DrawLine();
        }

        private void DrawLanguageFonts()
        {
            DrawSectionTitle("Language Fonts:", m_boldStyle);

            GUILayout.Label("You can specify any override fonts to be used for a specific language here.");

            GUILayout.BeginHorizontal(GUILayout.MaxWidth(this.position.width));

            if (GUILayout.Button("Add Language Font"))
            {
                GenericMenu menu = new GenericMenu();
                var supportedLanguages = CurrentAsset.Database.GetSupportedLanguages;

                foreach (SystemLanguage lang in supportedLanguages)
                {
                    if (!CurrentAsset.HasLanguageFont(lang))
                    {
                        menu.AddItem(new GUIContent(lang.ToString()), false, delegate
                        {
                            CurrentAsset.AddLanguageFont(lang);
                        });
                    }
                }

                menu.ShowAsContext();
            }

            if (GUILayout.Button("Remove Language Font"))
            {
                GenericMenu menu = new GenericMenu();
                var supportedLanguages = CurrentAsset.Database.GetSupportedLanguages;

                foreach (SystemLanguage lang in supportedLanguages)
                {
                    if (CurrentAsset.HasLanguageFont(lang))
                    {
                        menu.AddItem(new GUIContent(lang.ToString()), false, delegate
                        {
                            CurrentAsset.RemoveLanguageFont(lang);
                        });
                    }
                }

                menu.ShowAsContext();
            }

            GUILayout.EndHorizontal();

            for (int i = 0; i < CurrentAsset.LanguageFontList.Count; i++)
            {
                GUILayout.BeginHorizontal(GUILayout.MaxWidth(this.position.width));
                GUILayout.Label(CurrentAsset.LanguageFontList[i].Language.ToString());
                CurrentAsset.LanguageFontList[i].Font = (TMPro.TMP_FontAsset)EditorGUILayout.ObjectField("TMProFont", CurrentAsset.LanguageFontList[i].Font, typeof(TMPro.TMP_FontAsset), allowSceneObjects: false);
                GUILayout.EndHorizontal();
            }

            // Draw line
            DrawLine(); 
        }

        private void DrawAddNewEntry()
        {
            DrawSectionTitle("Add new entry:", m_boldStyle);

            GUILayout.BeginHorizontal(GUILayout.MaxWidth(this.position.width));
            {
                const float LABEL_MAX_WID = 75;
                const float BOX_MAX_WIDTH = 150;

                // ID 
                GUILayout.BeginHorizontal(m_entryStyle, GUILayout.Width(150));
                EditorGUILayout.LabelField("ID:", GUILayout.Height(EditorGUIUtility.singleLineHeight), GUILayout.Width(LABEL_MAX_WID));
                m_newID = EditorGUILayout.TextArea(m_newID, EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight), GUILayout.Width(BOX_MAX_WIDTH));
                GUILayout.EndHorizontal();

                GUILayout.FlexibleSpace();

                // English Text
                GUILayout.BeginHorizontal(m_entryStyle, GUILayout.Width(150));
                EditorGUILayout.LabelField("English text:", GUILayout.Height(EditorGUIUtility.singleLineHeight), GUILayout.Width(LABEL_MAX_WID));
                m_newEnglish = EditorGUILayout.TextArea(m_newEnglish, EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight), GUILayout.Width(BOX_MAX_WIDTH));
                GUILayout.EndHorizontal();


                GUILayout.FlexibleSpace();

                // Button
                if (GUILayout.Button("Add Entry", GUILayout.Width(100)))
                {
                    CurrentAsset.Database.AddNewEntry(m_newID, m_newEnglish);

                    m_newID = "";
                    m_newEnglish = "";

                    ClearSearch();
                }
            }
            GUILayout.EndHorizontal();
            DrawLine();

        }

        private void DrawCurrentEntries()
        {
            DrawSectionTitle("Entries:", m_boldStyle);

            if (CurrentAsset.Database.GetLocalisationEntryCount > 0)
            {
                const float LOCALE_BOX_TOP_PADDING = 20;
                const float LOCALE_BOX_ENTRY_HEIGHT = 22.5f;
                const float LOCALE_BOX_BOTTOM_SCROLLBAR_PADDING = 20;

                int numEntries = CurrentAsset.Database.GetLocalisationEntryCount;
                int startIndex = m_currentEntriesPage * MAX_PER_PAGE;
                int endIndex = startIndex + MAX_PER_PAGE - 1;
                if (endIndex > numEntries - 1)
                    endIndex = numEntries - 1;
                int numToDisplay = (endIndex - startIndex);
                float locale_box_height = LOCALE_BOX_TOP_PADDING + (LOCALE_BOX_ENTRY_HEIGHT * numToDisplay) + LOCALE_BOX_BOTTOM_SCROLLBAR_PADDING;

                m_entriesGridScrollView = EditorGUILayout.BeginScrollView(m_entriesGridScrollView, GUILayout.MaxWidth(this.position.width), GUILayout.Height(locale_box_height));
                {
                    for (int i = startIndex; i <= endIndex; i++)
                    {
                        GUILayout.BeginHorizontal();

                        LocaleEntry entry = CurrentAsset.Database.GetEntryByIndex(i);
                        const float SPACE = 15;

                        // Entry num
                        GUILayout.BeginHorizontal(GUILayout.Width(45));
                        GUILayout.Label("[" + (int)(i + 1) + "]");
                        GUILayout.EndHorizontal();

                        // ID
                        DrawLanguageEntry("ID:", entry.ID);
                        GUILayout.Space(SPACE);

                        // Each language
                        for (int j = 0; j < _supportedLanguages.Count; j++)
                        {
                            SystemLanguage lang = _supportedLanguages[j];

                            DrawLanguageEntry(lang.ToString(), entry.GetLanguageText(lang));
                            GUILayout.Space(SPACE);
                        }

                        // Button
                        if (GUILayout.Button("Delete Entry"))
                        {
                            CurrentAsset.Database.DeleteEntryByIndex(i);

                            if (m_currentEntriesPage > MaxPageIndex)
                                m_currentEntriesPage = MaxPageIndex;

                            ClearSearch();
                            return;
                        }
                        GUILayout.FlexibleSpace();

                        GUILayout.EndHorizontal();
                    }
                }
                EditorGUILayout.EndScrollView();


                // SCROLL PAGE
                GUILayout.Space(SMALL_PADDING);
                EditorGUILayout.BeginHorizontal(GUILayout.MaxWidth(this.position.width));
                GUILayout.Label(string.Format("Displaying entries {0} - {1} of {2}", startIndex + 1, endIndex + 1, numEntries));
                if (GUILayout.Button("|<<"))
                {
                    m_currentEntriesPage = 0;
                }
                if (GUILayout.Button("<"))
                {
                    m_currentEntriesPage--;
                    if (m_currentEntriesPage < 0)
                        m_currentEntriesPage = 0;
                }
                if (GUILayout.Button(">"))
                {
                    m_currentEntriesPage++;
                    if (m_currentEntriesPage >= MaxPageIndex)
                        m_currentEntriesPage = MaxPageIndex;
                }
                if (GUILayout.Button(">>|"))
                {
                    m_currentEntriesPage = MaxPageIndex;
                }
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                GUILayout.Label("Currently no entries.");
            }


            // Draw line
            DrawLine();

        }

        private void DrawSearchEntries()
        {
            DrawSectionTitle("Search:", m_boldStyle);

            GUILayout.BeginHorizontal(GUILayout.MaxWidth(this.position.width));
            {
                const float LABEL_MAX_WID = 65;
                const float BOX_MAX_WIDTH = 125;
                const float SEARCH_BTN_WIDTH = 85;

                // ID 
                GUILayout.BeginHorizontal(m_entryStyle, GUILayout.Width(100));
                EditorGUILayout.LabelField("By ID:", GUILayout.Height(EditorGUIUtility.singleLineHeight), GUILayout.Width(LABEL_MAX_WID));
                m_searchID = EditorGUILayout.TextArea(m_searchID, EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight), GUILayout.Width(BOX_MAX_WIDTH));
                if (GUILayout.Button("Search", GUILayout.Width(SEARCH_BTN_WIDTH)))
                {
                    SearchByID();
                }
                GUILayout.EndHorizontal();

                GUILayout.FlexibleSpace();

                // English
                GUILayout.BeginHorizontal(m_entryStyle, GUILayout.Width(100));
                EditorGUILayout.LabelField("By English:", GUILayout.Height(EditorGUIUtility.singleLineHeight), GUILayout.Width(LABEL_MAX_WID));
                m_searchEnglish = EditorGUILayout.TextArea(m_searchEnglish, EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight), GUILayout.Width(BOX_MAX_WIDTH));
                if (GUILayout.Button("Search", GUILayout.Width(SEARCH_BTN_WIDTH)))
                {
                    SearchByEnglish();
                }
                GUILayout.EndHorizontal();

                GUILayout.FlexibleSpace();

                // Button
                if (GUILayout.Button("Clear search", GUILayout.Width(100)))
                {
                    ClearSearch();
                }
            }
            GUILayout.EndHorizontal();

            if (m_searchResults == null)
                m_searchResults = new List<LocaleEntry>();

            GUILayout.Space(SMALL_PADDING);
        }

        private void DrawSearchResults()
        {
            if (m_searchResults.Count > 0)
            {
                const float LOCALE_BOX_TOP_PADDING = 20;
                const float LOCALE_BOX_ENTRY_HEIGHT = 22.5f;
                const float LOCALE_BOX_BOTTOM_SCROLLBAR_PADDING = 20;

                int search_numEntries = m_searchResults.Count;
                int search_startIndex = m_currentSearchPage * MAX_PER_PAGE;
                int search_endIndex = search_startIndex + MAX_PER_PAGE - 1;
                if (search_endIndex > search_numEntries - 1)
                    search_endIndex = search_numEntries - 1;
                int numToDisplay = (search_endIndex - search_startIndex);
                float locale_box_height = LOCALE_BOX_TOP_PADDING + (LOCALE_BOX_ENTRY_HEIGHT * numToDisplay) + LOCALE_BOX_BOTTOM_SCROLLBAR_PADDING;

                m_searchGridScrollView = EditorGUILayout.BeginScrollView(m_searchGridScrollView, GUILayout.MaxWidth(this.position.width), GUILayout.Height(locale_box_height));
                {
                    for (int i = search_startIndex; i <= search_endIndex; i++)
                    {
                        GUILayout.BeginHorizontal();

                        LocaleEntry entry = m_searchResults[i];
                        const float SPACE = 15;

                        // Entry num
                        GUILayout.BeginHorizontal(GUILayout.Width(45));
                        GUILayout.Label("[" + (int)(i + 1) + "]");
                        GUILayout.EndHorizontal();

                        // ID
                        DrawLanguageEntry("ID:", entry.ID);
                        GUILayout.Space(SPACE);

                        // Each language
                        for (int j = 0; j < _supportedLanguages.Count; j++)
                        {
                            SystemLanguage lang = _supportedLanguages[j];

                            DrawLanguageEntry(lang.ToString(), entry.GetLanguageText(lang));
                            GUILayout.Space(SPACE);
                        }

                        // Button
                        if (GUILayout.Button("Delete Entry"))
                        {
                            CurrentAsset.Database.DeleteEntryByID(entry.ID);
                            m_searchResults.Remove(entry);

                            if (m_currentSearchPage > MaxSearchPageIndex)
                                m_currentSearchPage = MaxSearchPageIndex;
                            return;
                        }
                        GUILayout.FlexibleSpace();

                        GUILayout.EndHorizontal();
                    }
                }
                EditorGUILayout.EndScrollView();


                // SCROLL SEARCH
                GUILayout.Space(SMALL_PADDING);
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label(string.Format("Displaying search results {0} - {1} of {2}", search_startIndex + 1, search_endIndex + 1, search_numEntries));
                if (GUILayout.Button("|<<"))
                {
                    m_currentSearchPage = 0;
                }
                if (GUILayout.Button("<"))
                {
                    m_currentSearchPage--;
                    if (m_currentSearchPage < 0)
                        m_currentSearchPage = 0;
                }
                if (GUILayout.Button(">"))
                {
                    m_currentSearchPage++;
                    if (m_currentSearchPage >= MaxSearchPageIndex)
                        m_currentSearchPage = MaxSearchPageIndex;
                }
                if (GUILayout.Button(">>|"))
                {
                    m_currentSearchPage = MaxSearchPageIndex;
                }
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                GUILayout.Label("Currently no search results.");
            }

            DrawLine();
        }


        // -------- Generic draw functions --------

        private void DrawSectionTitle(string title, GUIStyle style)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(SIDE_PADDING);
            GUILayout.Label(title, style);
            GUILayout.EndHorizontal();
            GUILayout.Space(SMALL_PADDING);
        }

        private void DrawLanguageEntry(string prefixText, string labelText)
        {
            GUILayout.BeginHorizontal(m_entryStyle, GUILayout.Width(150));
            Vector2 textDimensions = GUI.skin.label.CalcSize(new GUIContent(prefixText));
            float wid = textDimensions.x;
            const float MAX_WID = 75;
            if (wid > MAX_WID)
                wid = MAX_WID;
            EditorGUILayout.LabelField(prefixText, GUILayout.Height(EditorGUIUtility.singleLineHeight), GUILayout.Width(wid));
            EditorGUIUtility.labelWidth = 25;
            EditorGUILayout.TextArea(labelText, EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight), GUILayout.MaxWidth(94));
            GUILayout.EndHorizontal();
        }

        private void DrawWindowMessage(string msg)
        {
            float width = this.position.width;
            float centerX = width / 2;
            float height = this.position.height;
            float centerY = height / 2;
            Vector2 textDimensions = GUI.skin.label.CalcSize(new GUIContent(msg));
            Rect textRect = new Rect(centerX - (textDimensions.x / 2), centerY - (textDimensions.y / 2), textDimensions.x, textDimensions.y);
            EditorGUI.LabelField(textRect, msg);
        }

        private void DrawLine()
        {
            GUILayout.Label("", GUI.skin.horizontalSlider);
            GUILayout.Space(LARGE_PADDING);
        }




        //--------------------------------------
        // Misc Util
        //--------------------------------------

        private void ClearSearch()
        {
            m_searchResults = new List<LocaleEntry>();
            m_currentSearchPage = 0;
        }

        private void SearchByID()
        {
            ClearSearch();

            if (string.IsNullOrEmpty(m_searchID)) { return; }

            int numEntries = CurrentAsset.Database.GetLocalisationEntryCount;
            string idToLower = m_searchID.ToLower();

            for (int i = 0; i < numEntries; i++)
            {
                LocaleEntry entry = CurrentAsset.Database.GetEntryByIndex(i);

                if (entry.ID.ToLower().Contains(idToLower))
                {
                    m_searchResults.Add(entry);
                }
            }
        }

        private void SearchByEnglish()
        {
            ClearSearch();

            if (string.IsNullOrEmpty(m_searchEnglish)) { return; }

            int numEntries = CurrentAsset.Database.GetLocalisationEntryCount;
            string engToLower = m_searchEnglish.ToLower();

            for (int i = 0; i < numEntries; i++)
            {
                LocaleEntry entry = CurrentAsset.Database.GetEntryByIndex(i);

                if (entry.GetLanguageText(SystemLanguage.English).ToLower().Contains(engToLower))
                {
                    m_searchResults.Add(entry);
                }
            }
        }

        private int MaxPageIndex
        {
            get
            {
                float maxIndex = CurrentAsset.Database.GetLocalisationEntryCount / MAX_PER_PAGE;
                int pg = Mathf.FloorToInt(maxIndex);
                if (pg > 0 && CurrentAsset.Database.GetLocalisationEntryCount % MAX_PER_PAGE == 0)
                    pg--;
                return pg;
            }
        }

        private int MaxSearchPageIndex
        {
            get
            {
                float maxIndex = m_searchResults.Count / MAX_PER_PAGE;
                int pg = Mathf.FloorToInt(maxIndex);
                if (m_searchResults.Count % MAX_PER_PAGE == 0)
                    pg--;
                return pg;
            }
        }

        private void ResetAll()
        {
            CurrentAsset.CreateDatabase();
            m_currentEntriesPage = 0;
            m_currentSearchPage = 0;
        }

        private static void Log(string str)
        {
#if DIALOGUE_DEBUG
            Debug.Log("[Dialogue Locale]: " + str);
#endif
        }




        //--------------------------------------
        // Read/Write - Save/Load - Json/CSV
        //--------------------------------------

        private void ExportToJson()
        {
            // JSONify
            LocalisationDatabase db = CurrentAsset.Database;

            // Create a new DataBase, only copy over data for the Supported Languages 
            //
            //....

            // Copy languages support
            LocalisationDatabase copy = new LocalisationDatabase();
            List<SystemLanguage> supportedLanguages = db.GetSupportedLanguages;
            for (int i = 0; i < supportedLanguages.Count; i++)
            {
                if (!copy.IsLanguageSupported(supportedLanguages[i]))
                {
                    copy.AddLanguage(supportedLanguages[i]);
                }
            }

            // Copy entries
            int entryCount = db.GetLocalisationEntryCount;
            for (int i = 0; i < entryCount; i++)
            {
                LocaleEntry entry = db.GetEntryByIndex(i);

                copy.AddNewEntry(entry.ID, entry.GetLanguageText(SystemLanguage.English));

                for (int j = 0; j < supportedLanguages.Count; j++)
                {
                    copy.GetEntryByID(entry.ID).SetLanguageText(supportedLanguages[j], entry.GetLanguageText(supportedLanguages[j]));
                }
            }


            // Write to Json
            System.IO.MemoryStream ms = new System.IO.MemoryStream();
            DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(LocalisationDatabase));
            ser.WriteObject(ms, copy);
            byte[] jsonData = ms.ToArray();
            ms.Close();
            string toJson = System.Text.Encoding.UTF8.GetString(jsonData, 0, jsonData.Length);

            // Save to file
            var path = EditorUtility.SaveFilePanel( "Save Localisation as JSON", "", "localisation.json", "json");
            if (path.Length != 0)
            {
                System.IO.File.WriteAllText(path, toJson);
            }

            // Debug
            Debug.Log(toJson);
        }

        private void ExportToCSV()
        {
            var csv = new System.Text.StringBuilder();
            List<SystemLanguage> languages = CurrentAsset.Database.GetSupportedLanguages;

            // First row - ID and Languages (column declaration)
            csv.Append("ID,");
            for (int i = 0; i < languages.Count; i++)
            {
                csv.Append(languages[i].ToString());
                if (i < languages.Count - 1)
                    csv.Append(",");
            }
            csv.AppendLine();

            // Second row and beyond - localisation data
            int numEntries = CurrentAsset.Database.GetLocalisationEntryCount;
            for (int i = 0; i < numEntries; i++)
            {
                LocaleEntry entry = CurrentAsset.Database.GetEntryByIndex(i);

                // Row ID 
                string id = entry.ID;
                csv.Append(id + ",");

                // Localisation per language 
                for (int j = 0; j < languages.Count; j++)
                {
                    string txt_in_lang = entry.GetLanguageText(languages[j]);

                    // If the text contains a comma, wrap the text in 
                    // "" marks to prevent the comma from breaking the csv
                    if (txt_in_lang.Contains(","))
                    {
                        txt_in_lang = txt_in_lang.Insert(0, "\"");
                        txt_in_lang = txt_in_lang.Insert(txt_in_lang.Length, "\"");
                    }

                    csv.Append(txt_in_lang);
                    if (j < languages.Count - 1)
                        csv.Append(",");
                }

                if (i < numEntries - 1)
                    csv.AppendLine();
            }

            // Save to file
            var path = EditorUtility.SaveFilePanel("Save Localisation as CSV", "", "localisation.csv", "csv");
            if (path.Length != 0)
            {
                System.IO.File.WriteAllText(path, csv.ToString());
            }

            // Debug
            Debug.Log(csv.ToString());
        }

        private void LoadFromJSON()
        {
            // Get file, load text
            string path = EditorUtility.OpenFilePanel("Select Localisation json", "", "json");
            string json = "";
            if (path.Length != 0)
            {
                json = System.IO.File.ReadAllText(path);
            }

            if (json == null || json == "")
            {
                return;
            }

            // De-jsonify into Database
            LocalisationDatabase db = new LocalisationDatabase();
            System.IO.MemoryStream ms = new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));
            DataContractJsonSerializer ser = new DataContractJsonSerializer(db.GetType());
            db = ser.ReadObject(ms) as LocalisationDatabase;
            ms.Close();

            if (db == null)
            {
                return;
            }

            // Apply to CurrentAsset
            int entryCount = db.GetLocalisationEntryCount;

            // For each entry...
            for (int i = 0; i < entryCount; i++)
            {
                LocaleEntry entry = db.GetEntryByIndex(i);

                // If entry doesn't exist, add it
                if (!CurrentAsset.Database.DoesIDExist(entry.ID))
                {
                    CurrentAsset.Database.AddNewEntry(entry.ID, entry.GetLanguageText(SystemLanguage.English));
                }

                // For each language...
                foreach (SystemLanguage lang in db.GetSupportedLanguages)
                {
                    // Update the language entry
                    string langText = entry.GetLanguageText(lang);
                    CurrentAsset.Database.GetEntryByID(entry.ID).SetLanguageText(lang, langText);
                }
            }
        }

        private void LoadFromCSV()
        {
            // Get file, load text
            string path = EditorUtility.OpenFilePanel("Select Localisation csv", "", "csv");
            string csv = "";
            if (path.Length != 0)
            {
                csv = System.IO.File.ReadAllText(path);
            }

            if (csv == null || csv == "")
            {
                return;
            }

            // Split the CSV into lines
            string[] lines = csv.Split('\n');

            // Clean the strings
            for (int i = 0; i < lines.Length; i++)
            {
                lines[i] = lines[i].Replace("\r", "");
            }

            // Create a dictionary to store the column index of each language
            Dictionary<int, UnityEngine.SystemLanguage> langColDict = new Dictionary<int, SystemLanguage>();
            string[] keys = lines[0].Split(',');

            bool idColumnFound = false;

            for (int i = 0; i < keys.Length; i++)
            {
                if (keys[i] == "ID")
                {
                    idColumnFound = true;
                    continue;
                }

                int index = i;

                // For each language... 
                foreach (SystemLanguage lang in System.Enum.GetValues(typeof(SystemLanguage)))
                {
                    string langToString = lang.ToString();
                    if (keys[i].Equals(langToString))
                    {
                        langColDict.Add(index, lang);
                    }
                }
            }

            if (!idColumnFound || langColDict.Keys.Count == 0)
            {
                Log("Invalid .csv file.");
                return;
            }

            // Ensure all languages present in the CSV are supported in the database
            foreach (SystemLanguage lang in langColDict.Values)
            {
                CurrentAsset.Database.AddLanguage(lang);
            }

            // Go through the csv, line by line, and update the database 
            for (int i = 1; i <= lines.Length-1; i++)
            {
                string current_line = lines[i];

                List<string> entries = new List<string>();
                bool readingSpeechMarks = false;
                string current_txt = "";

                // Go through the line, character by character
                foreach (char c in current_line)
                {
                    // We've hit a speech mark
                    if (c == '"')
                    {
                        // Speech marks have opened
                        if (!readingSpeechMarks)
                        {
                            readingSpeechMarks = true;
                        }
                        else
                        {
                            readingSpeechMarks = false;
                        }
                    }
                    // We've hit a comma
                    else if (c == ',')
                    {
                        // If the comma isn't in the middle of speechmarks, it marks the end of the column
                        if (!readingSpeechMarks)
                        {
                            entries.Add(current_txt);
                            current_txt = "";
                        }
                    }
                    else
                    {
                        // Add onto the current string
                        current_txt += c;
                    }
                }
                // End of line...
                entries.Add(current_txt);
                current_txt = "";


                // Now we have our comma-split list of each column for this line
                if (entries.Count > 0)
                {
                    string id = entries[0];
                    LocaleEntry localeEntry = CurrentAsset.Database.GetEntryByID(id);

                    if (localeEntry == null)
                    {
                        CurrentAsset.Database.AddNewEntry(id, "");
                        localeEntry = CurrentAsset.Database.GetEntryByID(id);
                    }

                    for (int j = 1; j < entries.Count; j++)
                    {
                        int currentColumnIndex = j;
                        SystemLanguage lang = langColDict[currentColumnIndex];
                        string entryText = entries[currentColumnIndex];

                        // If the entry text is wrapped in "", remove them. 
                        if (entryText[0] == '"' && entryText[entryText.Length - 1] == '"')
                        {
                            entryText.Remove(0, 1);
                            entryText.Remove(entryText.Length - 1, 1);
                        }

                        localeEntry.SetLanguageText(lang, entryText);
                    }
                }
            }
        }
    }
}