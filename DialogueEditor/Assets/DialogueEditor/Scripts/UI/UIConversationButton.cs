using UnityEngine;

namespace DialogueEditor
{
    public class UIConversationButton : MonoBehaviour
    {
        public TMPro.TextMeshProUGUI TextMesh;

        private ConversationOption m_option;
        private ConversationAction m_action;

        public void SetOption(ConversationOption option)
        {
            m_option = option;
            TextMesh.text = option.Text;
        }

        public void SetAction(ConversationAction action)
        {
            m_action = action;
            TextMesh.text = "Continue.";
        }

        public void SetAsEndConversation()
        {
            m_option = null;
            TextMesh.text = "End.";
        }

        public void OnOptionSelected()
        {
            if (m_action != null)
                ConversationManager.Instance.DoAction(m_action);
            else
                ConversationManager.Instance.OptionSelected(m_option);
        }
    }
}