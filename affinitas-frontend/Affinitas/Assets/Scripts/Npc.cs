using System.Collections.Generic;

public class Npc
{
    public int idNo;
    public string npcName;
    public int affinitasValue;

    List<string> questList = new();
    List<string> previousDialogues = new();

    public Npc(int idNo, string npcName, int affinitasValue)
    {
        this.idNo = idNo;
        this.npcName = npcName;
        this.affinitasValue = affinitasValue;
    }

    public void AddQuestList(string[] lst)
    {
        for (int i = 0; i < lst.Length; i++)
        {
            questList.Add(lst[i]);
        }
    }

    public void AddToDialogues(string dialogue)
    {
        previousDialogues.Add(dialogue);
    }
}