using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DialogueEditor;

public class ShopWorker : MonoBehaviour
{
    public NPCConversation conversation;

    private void OnMouseDown()
    {
        ConversationManager.Instance.StartConversation(conversation);
    }
}
