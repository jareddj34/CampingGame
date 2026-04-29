using UnityEngine;
using Yarn.Unity;

public class NPC : AInteractable
{

    public DialogueRunner dialogueRunner;
    public GameObject dialogueUI;
    public KillCounter killCounter;
    public Animator animator;

    public Item boomerang;
    public Item bow;
    public Item watch;

    public void Start()
    {
        dialogueUI.SetActive(false);

        if (dialogueRunner != null)
        {
            dialogueRunner.AddFunction("GetChickenKillCount", GetChickenKillCount);
            dialogueRunner.AddFunction("GiveBoomerang", GiveBoomerang);
            dialogueRunner.AddFunction("GiveBow", GiveBow);
            dialogueRunner.AddFunction("GiveReward", GiveReward);
        }
    }

    public override void Interact()
    {
        if(dialogueRunner != null)
        {
            dialogueUI.SetActive(true);
            
            dialogueRunner.StartDialogue("ChickenGuy");

            SetTalking(true);
        }
    }

    public void SetTalking(bool isTalking)
    {
        if (animator != null)
        {
            animator.SetBool("Talking", isTalking);
        }
    }

    public float GetChickenKillCount()
    {
        return killCounter.killCount;
    }

    public string GiveBoomerang()
    {
        var inventoryManager = FindObjectOfType<InventoryManager>();
        inventoryManager.AddItem(boomerang);

        return "";
    }

    public string GiveBow()
    {
        var inventoryManager = FindObjectOfType<InventoryManager>();
        inventoryManager.AddItem(bow);

        return "";
    }

    public string GiveReward()
    {
        var inventoryManager = FindObjectOfType<InventoryManager>();
        inventoryManager.AddItem(watch);

        return "";
    }

}
