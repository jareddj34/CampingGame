using UnityEngine;
using System;
using UnityEngine.SceneManagement;

public class GameStateManager : MonoBehaviour
{
    public static GameStateManager Instance { get; private set; }
    public bool IsPlayerInputEnabled { get; private set; } = true;

    // Pause
    public bool IsPaused { get; private set; } = false;
    public event Action<bool> OnPauseChanged;

    public bool IsInDialogue { get; private set; }

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        // DontDestroyOnLoad(gameObject);
    }

    public void SetInputEnabled(bool enabled)
    {
        IsPlayerInputEnabled = enabled;
    }

    public void SetDialogueActive(bool active)
    {
        IsInDialogue = active;
        if (!IsPaused)
            SetInputEnabled(!active);
    }

    public void TogglePause()
    {
        IsPaused = !IsPaused;
        Time.timeScale = IsPaused ? 0f : 1f;

        if (!IsPaused)
            SetInputEnabled(!IsInDialogue);
        else
            SetInputEnabled(false);

        OnPauseChanged?.Invoke(IsPaused);

        Cursor.lockState = IsPaused ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = IsPaused;
    }

    public void LoadMainMenu()
    {
        if (IsPaused)
            TogglePause();
            SceneManager.LoadScene("StartScreen");
    }

    public void RestartLevel()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
