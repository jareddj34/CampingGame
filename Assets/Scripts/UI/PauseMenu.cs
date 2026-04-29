using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    [SerializeField] private GameObject pausePanel;

    private void Start()
    {
        GameStateManager.Instance.OnPauseChanged += HandlePauseChanged;
    }

    private void OnDestroy()
    {
        if (GameStateManager.Instance != null)
            GameStateManager.Instance.OnPauseChanged -= HandlePauseChanged;
    }

    private void HandlePauseChanged(bool isPaused)
    {
        pausePanel.SetActive(isPaused);
    }

    public void OnResumeButton()
    {
        if (GameStateManager.Instance.IsPaused)
            GameStateManager.Instance.TogglePause();
    }

    public void OnQuitButton()
    {
        Application.Quit();
    }

    public void OnMainMenuButton()
    {
        GameStateManager.Instance.LoadMainMenu();
    }
}