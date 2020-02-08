using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace DialogueEditor
{
    public class ConversationManager : MonoBehaviour
    {
        public static ConversationManager Instance { get; private set; }

        public delegate void ConversationStartEvent();
        public delegate void ConversationEndEvent();

        public static ConversationStartEvent OnConversationStarted;
        public static ConversationEndEvent OnConversationEnded;

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

        private Conversation m_conversation;
        private List<UIConversationButton> m_uiOptions;

        private string m_currentScrollText;
        private string m_targetScrollText;
        private bool m_scrollingText;
        private float m_elapsedScrollTime;
        private int m_scrollIndex;



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
            if (m_scrollingText)
            {
                UpdateScrollingText();
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
                    m_scrollingText = false;
                }
            }
        }




        //--------------------------------------
        // Start Conversation
        //--------------------------------------

        public void StartConversation(NPCConversation conversation)
        {
            TurnOnUI();

            m_conversation = conversation.Deserialize();

            if (OnConversationStarted != null)
                OnConversationStarted.Invoke();

            DoAction(m_conversation.Root);
        }




        //--------------------------------------
        // Set action
        //--------------------------------------

        public void DoAction(DialogueNode action)
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

            // Set the button sprite
            for (int i = 0; i < m_uiOptions.Count; i++)
            {
                m_uiOptions[i].SetImage(OptionImage, OptionImageSliced);
            }
        }

        public void OptionSelected(OptionNode option)
        {
            // Clear all current options
            ClearOptions();

            if (option == null)
            {
                EndConversation();
                return;
            }

            DialogueNode nextAction = option.Dialogue;
            if (nextAction == null)
            {
                EndConversation();
            }
            else
            {
                DoAction(nextAction);
            }
        }




        //--------------------------------------
        // End Conversation
        //--------------------------------------

        private void EndConversation()
        {
            TurnOffUI();

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