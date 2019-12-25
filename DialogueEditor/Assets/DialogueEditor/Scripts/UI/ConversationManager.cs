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
        public bool PersistThrougoutScenes;

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

        private void Awake()
        {
            // Destroy myself if I am not the singleton
            if (Instance != null && Instance != this)
            {
                GameObject.Destroy(this.gameObject);
            }

            Instance = this;

            if (PersistThrougoutScenes)
            {
                GameObject.DontDestroyOnLoad(this.gameObject);
            }
        }

        private void Start()
        {
            TurnOffUI();
            m_options = new List<UIConversationButton>();
        }



        //--------------------------------------
        // Conversation
        //--------------------------------------

        public void StartConversation(NPCConversation conversation)
        {
            TurnOnUI();

            m_currentConversationData = conversation;
            m_currentConversation = conversation.Deserialize();

            DoAction(m_currentConversation.GetRootNode());
        }

        public void DoAction(ConversationAction action)
        {
            if (action == null)
            {
                TurnOffUI();
                return;
            }

            // Clear current options
            ClearOptions();

            // Set text, icon
            NpcIcon.sprite = action.Icon;
            DialogueText.text = action.Text;
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