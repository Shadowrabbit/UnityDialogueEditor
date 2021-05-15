using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DialogueEditor
{
    public class ExampleInputManager : MonoBehaviour
    {
        public KeyCode m_UpKey = KeyCode.UpArrow;
        public KeyCode m_DownKey = KeyCode.DownArrow;
        public KeyCode m_SelectKey = KeyCode.F;

        private void Update()
        {
            if (ConversationManager.Instance != null)
            {
                UpdateConversationInput();
            }
        }

        private void UpdateConversationInput()
        {
            if (ConversationManager.Instance.IsConversationActive)
            {
                if (Input.GetKeyDown(m_UpKey))
                    ConversationManager.Instance.SelectPreviousOption();
                else if (Input.GetKeyDown(m_DownKey))
                    ConversationManager.Instance.SelectNextOption();
                else if (Input.GetKeyDown(m_SelectKey))
                    ConversationManager.Instance.PressSelectedOption();
            }
        }
    }
}
