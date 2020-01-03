using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QuestManager : MonoBehaviour
{
    public enum Quests
    {
        Tutorial        = 0,
        DragonSlayer    = 1,
    }

    public void BeginQuest(Quests quest)
    {

    }

    public void BeginQuest(Quest i)
    {
        Debug.Log("Quest started: " + i);
    }
}