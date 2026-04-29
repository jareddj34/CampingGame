using UnityEngine;
using Yarn.Unity;

public class NPCDialogueAudio : DialoguePresenterBase
{
    public AudioSource audioSource;
    public AudioClip[] talkingSounds;

    public override YarnTask OnDialogueStartedAsync()
    {
        return YarnTask.CompletedTask;
    }

    public override YarnTask OnDialogueCompleteAsync()
    {
        return YarnTask.CompletedTask;
    }

    public override YarnTask RunLineAsync(LocalizedLine line, LineCancellationToken token)
    {
        if (audioSource != null && talkingSounds.Length > 0)
        {
            AudioClip clip = talkingSounds[Random.Range(0, talkingSounds.Length)];
            audioSource.PlayOneShot(clip);
        }
        return YarnTask.CompletedTask; // return immediately, don't block the line display
    }
}