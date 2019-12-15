using UnityEngine;

namespace DialogueEditor
{
    public class UIConversationButton : MonoBehaviour
    {
        public TMPro.TextMeshProUGUI TextMesh;

        private ConversationOption m_option;

        public void SetOption(ConversationOption option)
        {
            m_option = option;
            TextMesh.text = option.Text;
        }

        public void SetAsEndConversation()
        {
            m_option = null;
            TextMesh.text = "End.";
        }

        public void OnOptionSelected()
        {
            ConversationManager.Instance.OptionSelected(m_option);
        }
    }
}