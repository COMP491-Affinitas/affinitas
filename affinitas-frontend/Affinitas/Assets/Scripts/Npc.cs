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

    // First wuest is main quest, others subquests
    List<string> questList;
    List<int> questStatus;
    // One dialogue summary for each day
    List<string> dialogueSummary = new();

    public Npc(int npcId, string npcName, int affinitasValue)
    {
        this.npcId = npcId;
        this.npcName = npcName;
        this.affinitasValue = affinitasValue;
    }

    //public void AddQuestList(string[] lst)
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