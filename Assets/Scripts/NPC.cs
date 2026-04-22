using UnityEngine;
using Yarn.Unity;

public class NPC : AInteractable
{

    public DialogueRunner dialogueRunner;
    public KillCounter killCounter;

    public void Start()
    {
        if (dialogueRunner != null)
        {
            dialogueRunner.AddFunction("GetChickenKillCount", GetChickenKillCount);
        }
    }

    public override void Interact()
    {
        if(dialogueRunner != null)
        {
            dialogueRunner.StartDialogue("ChickenGuy");
        }
    }

    public float GetChickenKillCount()
    {
        return killCounter.killCount;
    }
}
