using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DialogueEditor;

public class LanguageSetter : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        ConversationManager.Instance.SetLanguage(SystemLanguage.Arabic);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
