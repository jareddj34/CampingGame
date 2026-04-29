using UnityEngine;

public class ToolSound : MonoBehaviour
{
    public AudioSource useSound;

    public void PlayUseSound()
    {
        useSound.Play();
    }
}
