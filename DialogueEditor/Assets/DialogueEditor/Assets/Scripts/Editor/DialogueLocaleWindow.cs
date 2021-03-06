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
        private const string HELP_URL = "https://josephbarber96.github.io/dialogueeditor.html";
        private const string CONTROL_NAME = "DEFAULT_LOCALE_CONTROL";
        private const string UNAVAILABLE_DURING_PLAY_TEXT = "Dialogue Editor unavaiable during play mode.";


        private const int MAX_PER_PAGE = 10;

        private const int SMALL_PADDING = 5;
        private const int LARGE_PADDING = 15;


        // Private variables:     
        private DialogueEditorLocalisationObject CurrentAsset; // The Localisation scriptable object that is currently being viewed/edited



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

        private string _newID;
        private string _newEnglish;

        private Vector2 m_gridScrollView;
        private int m_currentPage;

        private Vector2 m_langScrollView;

        private Vector2 m_windowScrollView;

        private const float SIDE_PADDING = 5;


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




            //-------------------------
            // STYLES SETUP

            GUIStyle _boldStyle = new GUIStyle();
            _boldStyle.fontStyle = FontStyle.Bold;
            if (EditorGUIUtility.isProSkin)
            {
                _boldStyle.normal.textColor = DialogueEditorUtil.ProSkinTextColour;
            }

            GUIStyle _wordWrapStyle = new GUIStyle();
            _wordWrapStyle.wordWrap = true;
            if (EditorGUIUtility.isProSkin)
            {
                _wordWrapStyle.normal.textColor = DialogueEditorUtil.ProSkinTextColour;
            }




            //-------------------------
            // TITLE 

            DrawTitleBar();

            m_windowScrollView = GUILayout.BeginScrollView(m_windowScrollView);



            //-------------------------
            // SUPPORTED LANGUAGES

            GUILayout.BeginHorizontal();
            GUILayout.Space(SIDE_PADDING);
            GUILayout.Label("Choose languages: ", _boldStyle);
            GUILayout.EndHorizontal();

            GUILayout.Space(SMALL_PADDING);

            GUIStyle paddingStyle = new GUIStyle();
            int offx = 15;
            paddingStyle.margin = new RectOffset(offx, offx, 0, offx);
            m_langScrollView = GUILayout.BeginScrollView(m_langScrollView, paddingStyle, GUILayout.Height(100));
            {
                var allLanguages = System.Enum.GetValues(typeof(SystemLanguage));

                int _colCounter = 0;
                bool openedHorizontal = false;
                float size = this.position.width;
                float thirdSize = size / 4;

                for (int i = 0; i < allLanguages.Length; i++)
                {
                    SystemLanguage lang = (SystemLanguage)allLanguages.GetValue(i);
                    // Skip "Unknown". Skip "Chinese" because there is both Traditional and Simplified Chinese as separate entries
                    if (lang == SystemLanguage.Unknown || lang == SystemLanguage.Chinese)
                    {
                        continue;
                    }
                    bool isSupported = CurrentAsset.Database.IsLanguageSupported(lang);

                    // Handle opening horizontal layout
                    if (!openedHorizontal)
                    {
                        GUILayout.BeginHorizontal();
                        openedHorizontal = true;
                    }
                    _colCounter++;

                    // Label and button
                    GUILayout.BeginHorizontal(GUILayout.Width(thirdSize));
                    EditorGUILayout.LabelField(lang.ToString(), GUILayout.Width(76));
                    if (isSupported)
                    {
                        if (GUILayout.Button("Remove"))
                        {
                            CurrentAsset.Database.RemoveLanguage(lang);
                        }
                    }
                    else
                    {
                        if (GUILayout.Button("Add"))
                        {
                            CurrentAsset.Database.AddLanguage(lang);
                        }
                    }
                    GUILayout.EndHorizontal();

                    GUILayout.FlexibleSpace();

                    // Handle ending horizontal layout
                    if (_colCounter % 3 == 0)
                    {
                        GUILayout.EndHorizontal();
                        openedHorizontal = false;
                    }
                }

                // At end of loop, ensure horizontal is closed
                if (openedHorizontal)
                {
                    GUILayout.EndHorizontal();
                    openedHorizontal = false;
                }
            }
            GUILayout.Space(SMALL_PADDING);



            List<SystemLanguage> supportedLanguages = CurrentAsset.Database.GetSupportedLanguages;
            supportedLanguages.Sort();

            string supportedString = "";
            for (int i = 0; i < supportedLanguages.Count; i++)
            {
                supportedString += supportedLanguages[i].ToString();
                if (i < supportedLanguages.Count - 1)
                    supportedString += ", ";
            }
            GUILayout.EndScrollView();
            GUILayout.Space(SMALL_PADDING);
            GUILayout.TextArea("Supported languages: " + supportedString, _wordWrapStyle);
           
            // Draw line
            DrawLine();


            //-------------------------
            // ADDING NEW ENTRIES

            GUILayout.Label("Add new entry: ", _boldStyle);
            GUILayout.BeginHorizontal(GUILayout.MaxWidth(this.position.width));
            {
                const float LABEL_MAX_WID = 75;
                const float BOX_MAX_WIDTH = 150;
                GUIStyle style = new GUIStyle();
                style.stretchWidth = false;


                // ID 
                GUILayout.BeginHorizontal(style, GUILayout.Width(150));             
                EditorGUILayout.LabelField("ID:", GUILayout.Height(EditorGUIUtility.singleLineHeight), GUILayout.Width(LABEL_MAX_WID));
                _newID = EditorGUILayout.TextArea(_newID, EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight), GUILayout.Width(BOX_MAX_WIDTH));
                GUILayout.EndHorizontal();

                GUILayout.FlexibleSpace();

                // English Text
                GUILayout.BeginHorizontal(style, GUILayout.Width(150));
                EditorGUILayout.LabelField("English text:", GUILayout.Height(EditorGUIUtility.singleLineHeight), GUILayout.Width(LABEL_MAX_WID));
                _newEnglish = EditorGUILayout.TextArea(_newEnglish, EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight), GUILayout.Width(BOX_MAX_WIDTH));
                GUILayout.EndHorizontal();

                
                GUILayout.FlexibleSpace();

                // Button
                if (GUILayout.Button("Add Entry", GUILayout.Width(100)))
                {
                    CurrentAsset.Database.AddNewEntry(_newID, _newEnglish);

                    _newID = "";
                    _newEnglish = "";
                }
            }
            GUILayout.EndHorizontal();
            DrawLine();



            //-------------------------
            // DRAWING CURRENT ENTRIES

            GUILayout.Label("Entries:", _boldStyle);

            const float LOCALE_BOX_TOP_PADDING = 20;
            const float LOCALE_BOX_ENTRY_HEIGHT = 22.5f;

            int numEntries = CurrentAsset.Database.GetLocalisationEntryCount;
            int startIndex = m_currentPage * MAX_PER_PAGE;
            int endIndex = startIndex + MAX_PER_PAGE - 1;
            if (endIndex > numEntries - 1)
                endIndex = numEntries - 1;
            int numToDisplay = (endIndex - startIndex);
            float locale_box_height = LOCALE_BOX_TOP_PADDING + (LOCALE_BOX_ENTRY_HEIGHT * numToDisplay);

            m_gridScrollView = EditorGUILayout.BeginScrollView(m_gridScrollView, GUILayout.Height(locale_box_height));         
            {
                for (int i = startIndex; i <= endIndex; i++)
                {
                    GUILayout.BeginHorizontal();

                    LocaleEntry entry = CurrentAsset.Database.GetEntryByIndex(i);
                    const float SPACE = 15;

                    // Entry num
                    GUILayout.BeginHorizontal(GUILayout.Width(45));
                    GUILayout.Label("[" + (int)(i+1) + "]");
                    GUILayout.EndHorizontal();

                    // ID
                    DrawLanguageEntry("ID:", entry.ID);
                    GUILayout.Space(SPACE);

                    // Each language
                    for (int j = 0; j < supportedLanguages.Count; j++)
                    {
                        SystemLanguage lang = supportedLanguages[j];

                        DrawLanguageEntry(lang.ToString(), entry.GetLanguageText(lang));
                        GUILayout.Space(SPACE);
                    }
                
                    // Button
                    if (GUILayout.Button("Delete Entry"))
                    {
                        CurrentAsset.Database.DeleteEntry(i);

                        if (m_currentPage > MaxPage)
                            m_currentPage = MaxPage;
                        return;
                    }
                    GUILayout.FlexibleSpace();

                    GUILayout.EndHorizontal();
                }
            }
            EditorGUILayout.EndScrollView();


            // SCROLL PAGE
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(string.Format("Displaying entries {0} - {1} of {2}", startIndex+1, endIndex+1, numEntries));
            if (GUILayout.Button("|<<"))
            {
                m_currentPage = 0;
            }
            if (GUILayout.Button("<"))
            {
                m_currentPage--;
                if (m_currentPage < 0)
                    m_currentPage = 0;
            }
            if (GUILayout.Button(">"))
            {
                m_currentPage++;
                if (m_currentPage >= MaxPage)
                    m_currentPage = MaxPage;
            }
            if (GUILayout.Button(">>|"))
            {
                m_currentPage = MaxPage;
            }
            EditorGUILayout.EndHorizontal();

            // Draw line
            DrawLine();


            GUILayout.FlexibleSpace();

            // Entire window scroll view end
            GUILayout.EndScrollView();
        }

        private int MaxPage
        {
            get
            {
                float numPages = CurrentAsset.Database.GetLocalisationEntryCount / MAX_PER_PAGE;

                int pg = Mathf.CeilToInt(numPages);

                return pg;
            }
        }

        private void DrawLanguageEntry(string prefixText, string labelText)
        {
            GUIStyle style = new GUIStyle();
            style.stretchWidth = false;

            GUILayout.BeginHorizontal(style, GUILayout.Width(150));
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
            //-------------------------
            // DEBUG DEBUG DEBUG
            if (GUILayout.Button("DEBUG Reset"))
            {
                CurrentAsset.CreateDatabase();
            }
            // END END END
            //-------------------------
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Help", EditorStyles.toolbarButton))
            {
                Application.OpenURL(HELP_URL);
            }
            GUILayout.EndHorizontal();
        }




        //--------------------------------------
        // Misc
        //--------------------------------------

        private static void Log(string str)
        {
#if DIALOGUE_DEBUG
            Debug.Log("[Dialogue Locale]: " + str);
#endif
        }

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

                string id = entry.ID;
                csv.Append(id + ",");

                for (int j = 0; j < languages.Count; j++)
                {
                    string txt_in_lang = entry.GetLanguageText(languages[j]);
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
            LocalisationDatabase db = new LocalisationDatabase ();
            System.IO.MemoryStream ms = new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));
            DataContractJsonSerializer ser = new DataContractJsonSerializer(db.GetType());
            db = ser.ReadObject(ms) as LocalisationDatabase;
            ms.Close();

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

            for (int i = 0; i < keys.Length; i++)
            {
                if (keys[i] == "ID")
                {
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

            // Go through the csv and update the database 
            for (int i = 1; i < lines.Length; i++)
            {
                string[] entries = lines[i].Split(',');

                string id = entries[0];
                LocaleEntry localeEntry = CurrentAsset.Database.GetEntryByID(id);

                for (int j = 1; j < entries.Length; j++)
                {
                    int currentColumnIndex = j;
                    SystemLanguage lang = langColDict[currentColumnIndex];
                    string txt = entries[currentColumnIndex];

                    localeEntry.SetLanguageText(lang, txt);
                }
            }
        }
    }
}