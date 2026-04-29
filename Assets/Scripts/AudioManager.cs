using UnityEngine;
using System.Collections;

public class AudioManager : MonoBehaviour
{


    [Header("Music")]
    public AudioSource musicSource;
    public AudioClip[] musicClips;
    private int currentTrackIndex = 0;

    [Header("Ambient")]
    public AudioSource ambientSource;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.OnPauseChanged += HandlePauseChanged;
        }

        if(musicSource != null && musicClips.Length > 0)
        {
            StartCoroutine(PlayPlaylist());
        }
    }

    void OnDisable()
    {
        GameStateManager.Instance.OnPauseChanged -= HandlePauseChanged;
    }

    void HandlePauseChanged(bool isPaused)
    {
        AudioListener.pause = isPaused;
    }

    IEnumerator PlayPlaylist()
    {
        while (true)
        {
            AudioClip clip = musicClips[currentTrackIndex % musicClips.Length];

            musicSource.clip = clip;
            musicSource.Play();

            yield return new WaitForSeconds(clip.length);

            currentTrackIndex++;
        }
    }
}
