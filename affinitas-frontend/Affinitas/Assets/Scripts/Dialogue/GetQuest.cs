using UnityEngine;

namespace MainGame
{
    public class GetQuest : MonoBehaviour
    {
        AddDialogueBox addDialogueBox;
        ScrollRectHelper scrollRectHelper;

        private void Start()
        {
            //addDialogueBox = gameObject.GetComponent<AddDialogueBox>();
            //scrollRectHelper = gameObject.GetComponent<ScrollRectHelper>();
        }

        //Call when Get Quest is pressed
        public void GetQuestButton()
        {
            //TODO: SERVER SYSTEM CALL
            addDialogueBox.AddNpcDialogueBox("Hey your quest is to go adrgasd");
        }
    }
}

