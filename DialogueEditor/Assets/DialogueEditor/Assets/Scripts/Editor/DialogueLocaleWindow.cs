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

        private const int LOCALE_GRID_HEIGHT = 225;
        private const int MAX_PER_PAGE = 10;

        private const int SMALL_PADDING = 5;
        private const int LARGE_PADDING = 15;


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
            NPCConversation conversation = EditorUtility.InstanceIDToObject(assetInstanceID) as NPCConversation;

            if (conversation != null)
            {
                DialogueLocaleWindow window = ShowWindow();
                window.LoadNewAsset(conversation);
                return true;
            }
            return false;
        }




        //--------------------------------------
        // Load New Asset
        //--------------------------------------

        public void LoadNewAsset(NPCConversation asset)
        {
            if (Application.isPlaying)
            {
                Log("Load new asset aborted. Will not open assets during play.");
                return;
            }
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
        }

        [UnityEditor.Callbacks.DidReloadScripts]
        private static void OnScriptsReloaded()
        {

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


        private void OnGUI()
        {
            if (Application.isPlaying)
            {
                DrawMessage(UNAVAILABLE_DURING_PLAY_TEXT);
                return;
            }

            if (DialogueEditorLocalisation.Instance == null)
            {
                DrawMessage("No database exists.");
                return;
            }

            if (DialogueEditorLocalisation.Instance.Database == null)
            {
                DialogueEditorLocalisation.Instance.CreateDatabase();
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

            GUILayout.Label("Choose languages: ", _boldStyle);
            GUILayout.Space(SMALL_PADDING);
            m_langScrollView = GUILayout.BeginScrollView(m_langScrollView, GUILayout.Height(100));
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
                    bool isSupported = DialogueEditorLocalisation.Instance.Database.IsLanguageSupported(lang);

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
                            DialogueEditorLocalisation.Instance.Database.RemoveLanguage(lang);
                        }
                    }
                    else
                    {
                        if (GUILayout.Button("Add"))
                        {
                            DialogueEditorLocalisation.Instance.Database.AddLanguage(lang);
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

            List<SystemLanguage> supportedLanguages = DialogueEditorLocalisation.Instance.Database.GetSupportedLanguages;
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
                    DialogueEditorLocalisation.Instance.Database.AddNewEntry(_newID, _newEnglish);

                    _newID = "";
                    _newEnglish = "";
                }
            }
            GUILayout.EndHorizontal();
            DrawLine();



            //-------------------------
            // DRAWING CURRENT ENTRIES

            GUILayout.Label("Entries:", _boldStyle);


            m_gridScrollView = EditorGUILayout.BeginScrollView(m_gridScrollView, GUILayout.Height(LOCALE_GRID_HEIGHT));

            int numEntries = DialogueEditorLocalisation.Instance.Database.GetLocalisationEntryCount;
            int startIndex = m_currentPage * MAX_PER_PAGE;
            int endIndex = startIndex + MAX_PER_PAGE - 1;
            if (endIndex > numEntries - 1)
                endIndex = numEntries - 1;

            for (int i = startIndex; i <= endIndex; i++)
            {
                GUILayout.BeginHorizontal();

                LocaleEntry entry = DialogueEditorLocalisation.Instance.Database.GetEntryByIndex(i);
                const float SPACE = 15;

                // Entry num
                GUILayout.BeginHorizontal(GUILayout.Width(45));
                GUILayout.Label("[" + (int)(i+1) + "]");
                GUILayout.EndHorizontal();

                // ID
                DrawEntry("ID:", entry.ID);
                GUILayout.Space(SPACE);

                // Each language
                for (int j = 0; j < supportedLanguages.Count; j++)
                {
                    SystemLanguage lang = supportedLanguages[j];

                    DrawEntry(lang.ToString(), entry.GetLanguageText(lang));
                    GUILayout.Space(SPACE);
                }
                
                // Button
                if (GUILayout.Button("Delete Entry"))
                {
                    DialogueEditorLocalisation.Instance.Database.DeleteEntry(i);

                    if (m_currentPage > MaxPage)
                        m_currentPage = MaxPage;
                    return;
                }
                GUILayout.FlexibleSpace();

                GUILayout.EndHorizontal();
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



            GUILayout.EndScrollView();



            //-------------------------
            // DEBUG DEBUG DEBUG

            GUILayout.FlexibleSpace();
            GUILayout.Label("Debug area:");
            if (GUILayout.Button("DEBUG Reset"))
            {
                DialogueEditorLocalisation.Instance.CreateDatabase();
            }
        }

        private int MaxPage
        {
            get
            {
                float numPages = DialogueEditorLocalisation.Instance.Database.GetLocalisationEntryCount / MAX_PER_PAGE;

                int pg = Mathf.CeilToInt(numPages);

                return pg;
            }
        }

        private void DrawEntry(string prefixText, string labelText)
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

        private void DrawMessage(string msg)
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

            }
            if (GUILayout.Button("Import from CSV", EditorStyles.toolbarButton))
            {

            }
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
            LocalisationDatabase db = DialogueEditorLocalisation.Instance.Database;

            // Create a new DataBase, only copy over data for the Supported Languages 
            //
            //....

            // Copy languages support
            LocalisationDatabase copy = new LocalisationDatabase();
            List<SystemLanguage> supportedLanguages = db.GetSupportedLanguages;
            for (int i = 0; i < supportedLanguages.Count; i++)
            {
                copy.AddLanguage(supportedLanguages[i]);
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

            Debug.Log(toJson);
        }

        private void ExportToCSV()
        {
            var csv = new System.Text.StringBuilder();
            List<SystemLanguage> languages = DialogueEditorLocalisation.Instance.Database.GetSupportedLanguages;

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
            int numEntries = DialogueEditorLocalisation.Instance.Database.GetLocalisationEntryCount;
            for (int i = 0; i < numEntries; i++)
            {
                LocaleEntry entry = DialogueEditorLocalisation.Instance.Database.GetEntryByIndex(i);

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

            Debug.Log(csv.ToString());

            // File.WriteAllText(filePath, csv.ToString());
        }





        private EditableConversation Dejsonify()
        {
            string json = "";

            if (json == null || json == "")
                return null;

            EditableConversation conversation = new EditableConversation();
            System.IO.MemoryStream ms = new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));
            DataContractJsonSerializer ser = new DataContractJsonSerializer(conversation.GetType());
            conversation = ser.ReadObject(ms) as EditableConversation;
            ms.Close();

            return conversation;
        }
    }
}