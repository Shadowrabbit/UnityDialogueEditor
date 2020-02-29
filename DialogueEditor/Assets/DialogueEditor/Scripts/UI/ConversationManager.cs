using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace DialogueEditor
{
    public class ConversationManager : MonoBehaviour
    {
        private const float TRANS_TIME = 0.25f; // Transition time for fades

        public static ConversationManager Instance { get; private set; }

        public delegate void ConversationStartEvent();
        public delegate void ConversationEndEvent();

        public static ConversationStartEvent OnConversationStarted;
        public static ConversationEndEvent OnConversationEnded;

        private enum eState
        {
            TransitioningDialogueBoxOn,
            ScrollingText,
            TransitioningOptionsOn,
            Idle,
            TransitioningOptionsOff,
            TransitioningDialogueOff,
            Off,
            NONE,
        }

        // User-Facing options
        // Drawn by custom inspector
        public bool ScrollText;
        public float ScrollSpeed = 1;
        public Sprite BackgroundImage;
        public bool BackgroundImageSliced;
        public Sprite OptionImage;
        public bool OptionImageSliced;

        // Non-User facing 
        // Not exposed via custom inspector
        //
        // Base panels
        public RectTransform DialoguePanel;
        public RectTransform OptionsPanel;
        // Dialogue UI
        public Image DialogueBackground;
        public Image NpcIcon;
        public TMPro.TextMeshProUGUI DialogueText;
        // Components
        public AudioSource AudioPlayer;
        // Prefabs
        public UIConversationButton ButtonPrefab;


        // Private
        private string m_currentScrollText;
        private string m_targetScrollText;
        private bool m_scrollingText;
        private float m_elapsedScrollTime;
        private int m_scrollIndex;
        private eState m_state;
        private float m_stateTime;
        private Conversation m_conversation;
        private List<UIConversationButton> m_uiOptions;


        //--------------------------------------
        // Awake, Start, Destroy
        //--------------------------------------

        private void Awake()
        {
            // Destroy myself if I am not the singleton
            if (Instance != null && Instance != this)
            {
                GameObject.Destroy(this.gameObject);
            }
            Instance = this;

            m_uiOptions = new List<UIConversationButton>();
        }

        private void Start()
        {
            TurnOffUI();
        }

        private void OnDestroy()
        {
            Instance = null;
        }




        //--------------------------------------
        // Update
        //--------------------------------------

        private void Update()
        {
            switch (m_state)
            {
                case eState.TransitioningDialogueBoxOn:
                    {
                        m_stateTime += Time.deltaTime;
                        float t = m_stateTime / TRANS_TIME;

                        if (t > 1)
                        {
                            DoAction(m_pendingDialogue);
                            return;
                        }

                        Color c_dialogue = DialogueBackground.color;
                        Color c_icon = NpcIcon.color;
                        c_dialogue.a = t;
                        c_icon.a = t;
                        DialogueBackground.color = c_dialogue;
                        NpcIcon.color = c_icon;
                    }
                    break;

                case eState.ScrollingText:
                    UpdateScrollingText();
                    break;

                case eState.TransitioningOptionsOn:
                    {
                        m_stateTime += Time.deltaTime;
                        float t = m_stateTime / TRANS_TIME;

                        if (t > 1)
                        {
                            SetState(eState.Idle);
                            return;
                        }

                        for (int i = 0; i < m_uiOptions.Count; i++)
                            m_uiOptions[i].SetAlpha(t);
                    }
                    break;

                case eState.Idle:
                    break;

                case eState.TransitioningOptionsOff:
                    {
                        m_stateTime += Time.deltaTime;
                        float t = m_stateTime / TRANS_TIME;

                        if (t > 1)
                        {
                            ClearOptions();

                            if (m_selectedOption == null)
                            {
                                EndConversation();
                                return;
                            }

                            SpeechNode nextAction = m_selectedOption.Dialogue;
                            if (nextAction == null)
                            {
                                EndConversation();
                            }
                            else
                            {
                                DoAction(nextAction);
                            }
                            return;
                        }


                        for (int i = 0; i < m_uiOptions.Count; i++)
                            m_uiOptions[i].SetAlpha(1 - t);

                        Color c_text = DialogueText.color;
                        c_text.a = 1 - t;
                        DialogueText.color = c_text;
                    }
                    break;

                case eState.TransitioningDialogueOff:
                    {
                        m_stateTime += Time.deltaTime;
                        float t = m_stateTime / TRANS_TIME;

                        if (t > 1)
                        {
                            TurnOffUI();
                            return;
                        }

                        Color c_dialogue = DialogueBackground.color;
                        Color c_icon = NpcIcon.color;
                        c_dialogue.a = 1 - t;
                        c_icon.a = 1 - t;
                        DialogueBackground.color = c_dialogue;
                        NpcIcon.color = c_icon;
                    }
                    break;
            }
        }

        private void UpdateScrollingText()
        {
            const float charactersPerSecond = 1500;
            float timePerChar = (60.0f / charactersPerSecond);
            timePerChar *= ScrollSpeed;

            m_elapsedScrollTime += Time.deltaTime;

            if (m_elapsedScrollTime > timePerChar)
            {
                m_elapsedScrollTime = 0f;

                m_currentScrollText += m_targetScrollText[m_scrollIndex];
                m_scrollIndex++;

                DialogueText.text = m_currentScrollText;

                // Finished?
                if (m_scrollIndex >= m_targetScrollText.Length)
                {
                    SetState(eState.TransitioningOptionsOn);
                }
            }
        }




        //--------------------------------------
        // Set state
        //--------------------------------------

        private void SetState(eState newState)
        {
            m_state = newState;
            m_stateTime = 0f;

            switch (m_state)
            {
                case eState.TransitioningDialogueBoxOn:
                    {
                        Color c_dialogue = DialogueBackground.color;
                        Color c_icon = NpcIcon.color;
                        c_dialogue.a = 0;
                        c_icon.a = 0;
                        DialogueBackground.color = c_dialogue;
                        NpcIcon.color = c_icon;

                        DialogueText.text = "";
                    }
                    break;

                case eState.ScrollingText:
                    {
                        Color c_text = DialogueText.color;
                        c_text.a = 1;
                        DialogueText.color = c_text;
                    }
                    break;

                case eState.TransitioningOptionsOn:
                    {
                        for (int i = 0; i < m_uiOptions.Count; i++)
                        {
                            m_uiOptions[i].gameObject.SetActive(true);
                        }
                    }
                    break;
            }     
        }




        //--------------------------------------
        // Start Conversation
        //--------------------------------------

        private SpeechNode m_pendingDialogue;

        public void StartConversation(NPCConversation conversation)
        {
            m_conversation = conversation.Deserialize();
            if (OnConversationStarted != null)
                OnConversationStarted.Invoke();

            TurnOnUI();
            m_pendingDialogue = m_conversation.Root;
            SetState(eState.TransitioningDialogueBoxOn);
        }




        //--------------------------------------
        // Set action
        //--------------------------------------

        public void DoAction(SpeechNode action)
        {
            if (action == null)
            {
                EndConversation();
                return;
            }

            // Clear current options
            ClearOptions();

            // Set sprite and font
            NpcIcon.sprite = action.Icon;
            if (action.TMPFont != null)
            {
                DialogueText.font = action.TMPFont;
            }
            else if (m_conversation.DefaultTMPFont != null)
            {
                DialogueText.font = m_conversation.DefaultTMPFont;
            }
            else
            {
                DialogueText.font = null;
            }

            // Set text
            if (ScrollText)
            {
                m_scrollingText = true;
                m_currentScrollText = "";
                DialogueText.text = "";
                m_targetScrollText = action.Text;
                m_elapsedScrollTime = 0f;
                m_scrollIndex = 0;
            }
            else
            {
                DialogueText.text = action.Text;
            }

            // Call the event
            if (action.Event != null)
                action.Event.Invoke();

            // Play the audio
            if (action.Audio != null)
            {
                AudioPlayer.clip = action.Audio;
                AudioPlayer.Play();
            }

            // Display new options
            if (action.Options.Count > 0)
            {
                for (int i = 0; i < action.Options.Count; i++)
                {
                    UIConversationButton option = GameObject.Instantiate(ButtonPrefab, OptionsPanel);
                    option.SetOption(action.Options[i]);
                    m_uiOptions.Add(option);
                }
            }
            // Else display "continue" button to go to following dialogue
            else if (action.Dialogue != null)
            {
                UIConversationButton option = GameObject.Instantiate(ButtonPrefab, OptionsPanel);
                option.SetAction(action.Dialogue);
                m_uiOptions.Add(option);
            }
            // Else display "end" button
            else
            {
                UIConversationButton option = GameObject.Instantiate(ButtonPrefab, OptionsPanel);
                option.SetAsEndConversation();
                m_uiOptions.Add(option);
            }

            // Set the button sprite and alpha
            for (int i = 0; i < m_uiOptions.Count; i++)
            {
                m_uiOptions[i].SetImage(OptionImage, OptionImageSliced);
                m_uiOptions[i].SetAlpha(0);
                m_uiOptions[i].gameObject.SetActive(false);
            }

            SetState(eState.ScrollingText);
        }



        //--------------------------------------
        // Option Selected
        //--------------------------------------

        private OptionNode m_selectedOption;

        public void OptionSelected(OptionNode option)
        {
            m_selectedOption = option;
            SetState(eState.TransitioningOptionsOff);
        }




        //--------------------------------------
        // End Conversation
        //--------------------------------------

        private void EndConversation()
        {
            SetState(eState.TransitioningDialogueOff);

            if (OnConversationEnded != null)
                OnConversationEnded.Invoke();
        }




        //--------------------------------------
        // Util
        //--------------------------------------

        private void TurnOnUI()
        {
            DialoguePanel.gameObject.SetActive(true);
            OptionsPanel.gameObject.SetActive(true);

            if (BackgroundImage != null)
            {
                DialogueBackground.sprite = BackgroundImage;

                if (BackgroundImageSliced)
                    DialogueBackground.type = Image.Type.Sliced;
                else
                    DialogueBackground.type = Image.Type.Simple;
            }
        }

        private void TurnOffUI()
        {
            DialoguePanel.gameObject.SetActive(false);
            OptionsPanel.gameObject.SetActive(false);
            SetState(eState.Off);
        }

        private void ClearOptions()
        {
            while (m_uiOptions.Count != 0)
            {
                GameObject.Destroy(m_uiOptions[0].gameObject);
                m_uiOptions.RemoveAt(0);
            }
        }
    }
}