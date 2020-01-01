using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace DialogueEditor
{
    public class ConversationManager : MonoBehaviour
    {
        public static ConversationManager Instance { get; private set; }

        [Header("User-facing Options")]
        public bool ScrollText;
        public float ScrollSpeed = 1;

        [Header("Base")]
        public RectTransform DialoguePanel;
        public RectTransform OptionsPanel;

        [Header("Dialogue UI")]
        public Image NpcIcon;
        public TMPro.TextMeshProUGUI DialogueText;

        [Header("Components")]
        public AudioSource AudioPlayer;

        [Header("Prefabs")]
        public UIConversationButton ButtonPrefab;

        private NPCConversation m_currentConversationData;
        private Conversation m_currentConversation;
        private List<UIConversationButton> m_options;

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

            m_options = new List<UIConversationButton>();
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

            m_currentConversationData = conversation;
            m_currentConversation = conversation.Deserialize();

            DoAction(m_currentConversation.GetRootNode());
        }



        //--------------------------------------
        // Set action
        //--------------------------------------

        public void DoAction(ConversationAction action)
        {
            if (action == null)
            {
                TurnOffUI();
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
            else if (m_currentConversationData.DefaultFont != null)
            {
                DialogueText.font = m_currentConversationData.DefaultFont;
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
            UnityEvent actionEvent = m_currentConversationData.GetEventHolderForID(action.ID).Event;
            if (actionEvent != null)
                actionEvent.Invoke();

            // Play the audio
            if (action.Audio != null)
            {
                AudioPlayer.clip = action.Audio;
                AudioPlayer.Play();
            }

            // Display new options
            if (action.OptionUIDs != null && action.OptionUIDs.Count > 0)
            {
                for (int i = 0; i < action.OptionUIDs.Count; i++)
                {
                    UIConversationButton option = GameObject.Instantiate(ButtonPrefab, OptionsPanel);
                    option.SetOption(m_currentConversation.GetOptionByUID(action.OptionUIDs[i]));
                    m_options.Add(option);
                }
            }
            // Display "continue" button to go to the following dialogue
            else if (action.ActionUID != Conversation.INVALID_UID)
            {
                UIConversationButton option = GameObject.Instantiate(ButtonPrefab, OptionsPanel);
                option.SetAction(m_currentConversation.GetActionByUID(action.ActionUID));
                m_options.Add(option);
            }
            // Display "end" button
            else
            {
                UIConversationButton option = GameObject.Instantiate(ButtonPrefab, OptionsPanel);
                option.SetAsEndConversation();
                m_options.Add(option);
            }
        }

        public void OptionSelected(ConversationOption option)
        {
            // Clear all current options
            ClearOptions();

            if (option == null)
            {
                TurnOffUI();
                return;
            }

            ConversationAction nextAction = m_currentConversation.GetActionByUID(option.ActionUID);
            if (nextAction == null)
                TurnOffUI();
            else
                DoAction(nextAction);
        }




        //--------------------------------------
        // Util
        //--------------------------------------

        private void TurnOnUI()
        {
            DialoguePanel.gameObject.SetActive(true);
            OptionsPanel.gameObject.SetActive(true);
        }

        private void TurnOffUI()
        {
            DialoguePanel.gameObject.SetActive(false);
            OptionsPanel.gameObject.SetActive(false);
        }

        private void ClearOptions()
        {
            while (m_options.Count != 0)
            {
                GameObject.Destroy(m_options[0].gameObject);
                m_options.RemoveAt(0);
            }
        }
    }
}