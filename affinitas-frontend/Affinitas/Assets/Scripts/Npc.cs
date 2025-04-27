using System;
using System.Collections.Generic;

public class Npc
{
    public int npcId;
    public string npcName;

    public event Action<int> OnAffinitasChanged;

    private int _affinitasValue;
    public int affinitasValue
    {
        get => _affinitasValue;
        set
        {
            // If value unchanged do nothing
            if (_affinitasValue == value)
                return;

            // If value changed, also update UI
            _affinitasValue = value;
            OnAffinitasChanged?.Invoke(_affinitasValue);
        }
    }

    // Can be moved later
    public class Quest { 
        public string name;
        public int status; // 0 for incomplete, 1 for complete
        public string description;
        public string reward; 
    }

    // First quest is main quest, others subquests
    List<Quest> questList;
    List<int> questStatus;
    // One dialogue summary for each day
    List<string> dialogueSummary = new();

    public Npc(int npcId, string npcName, int affinitasValue, List<Quest> questList)
    {
        this.npcId = npcId;
        this.npcName = npcName;
        this.affinitasValue = affinitasValue;
        this.questList = questList;
    }

    //public void AddQuestList(Quest[] lst)
    //{
    //    for (int i = 0; i < lst.Length; i++)
    //    {
    //        questList.Add(lst[i]);
    //    }
    //}

    //public void AddToDialogues(string dialogue)
    //{
    //    dialogueSummary.Add(dialogue);
    //}
}