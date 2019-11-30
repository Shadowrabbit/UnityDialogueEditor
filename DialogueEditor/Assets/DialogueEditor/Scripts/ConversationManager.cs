using System.Collections.Generic;
using UnityEngine;

namespace DialogueEditor
{
    public class ConversationManager : MonoBehaviour
    {
        public static ConversationManager Instance { get; private set; }

        [Header("Options")]
        public bool PersistThrougoutScenes;

        [Header("UI")]
        public RectTransform BasePanel;
        public TMPro.TextMeshProUGUI DialogueTextMesh;

        [Header("Transforms")]
        public Transform OptionsPanel;

        [Header("Assets")]
        public UIConversationButton ButtonPrefab;

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
            BasePanel.gameObject.SetActive(false);
            m_options = new List<UIConversationButton>();
        }



        //--------------------------------------
        // Conversation
        //--------------------------------------

        public void StartConversation(NPCConversation conversation)
        {
            BasePanel.gameObject.SetActive(true);

            m_currentConversation = conversation.GetDeserialized();
            DoAction(m_currentConversation.GetRootNode());
        }

        private void DoAction(ConversationAction action)
        {
            if (action == null)
            {
                EndConversation();
                return;
            }

            DialogueTextMesh.text = action.Text;

            ClearOptions();

            if (action.OptionUIDs == null || action.OptionUIDs.Count == 0)
            {
                UIConversationButton option = GameObject.Instantiate(ButtonPrefab, OptionsPanel);
                option.SetAsEndConversation();
                m_options.Add(option);
            }
            else
            {
                for (int i = 0; i < action.OptionUIDs.Count; i++)
                {
                    UIConversationButton option = GameObject.Instantiate(ButtonPrefab, OptionsPanel);
                    option.SetOption(m_currentConversation.GetOptionByUID(action.OptionUIDs[i]));
                    m_options.Add(option);
                }
            }
        }

        public void OptionSelected(ConversationOption option)
        {
            // Clear all current options
            ClearOptions();

            if (option == null)
            {
                EndConversation();
                return;
            }

            ConversationAction nextAction = m_currentConversation.GetActionByUID(option.ActionUID);
            if (nextAction == null)
                EndConversation();
            else
                DoAction(nextAction);
        }

        private void EndConversation()
        {
            BasePanel.gameObject.SetActive(false);
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