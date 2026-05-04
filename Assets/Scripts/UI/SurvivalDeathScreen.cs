using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class SurvivalDeathScreen : MonoBehaviour
{
    // ── PlayerPrefs keys ───────────────────────────────────────────────────────
    private const string KEY_BEST_WAVES = "SurvivalBestWaves";
    private const string KEY_BEST_KILLS = "SurvivalBestKills";

    // ── UI References ──────────────────────────────────────────────────────────
    [Header("Run Stats")]
    [Tooltip("Shows the wave the player reached this run (e.g. 'Wave 4').")]
    [SerializeField] private TextMeshProUGUI wavesText;

    [Tooltip("Shows how many chickens were killed this run.")]
    [SerializeField] private TextMeshProUGUI killsText;

    [Header("High Score")]
    [Tooltip("Shows the all-time best wave reached.")]
    [SerializeField] private TextMeshProUGUI bestWavesText;

    [Tooltip("Shows the all-time most kills in a single run.")]
    [SerializeField] private TextMeshProUGUI bestKillsText;

    [Tooltip("'NEW BEST!' label — shown briefly when the player sets a new record. Can be null.")]
    [SerializeField] private GameObject newBestBadge;

    // ── Internal ───────────────────────────────────────────────────────────────
    private int _bestWaves;
    private int _bestKills;

    // ══════════════════════════════════════════════════════════════════════════

    private void Awake()
    {
        // Load saved high scores
        _bestWaves = PlayerPrefs.GetInt(KEY_BEST_WAVES, 0);
        _bestKills = PlayerPrefs.GetInt(KEY_BEST_KILLS, 0);

        // Start hidden
        gameObject.SetActive(false);
        if (newBestBadge != null) newBestBadge.SetActive(false);
    }

    // ── Public API ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Called by SurvivalModeManager when the player dies.
    /// Pass the wave they reached and their total kill count for the run.
    /// </summary>
    public void Show(int wavesReached, int totalKills)
    {
        bool newRecord = false;

        // Check and save high scores
        if (wavesReached > _bestWaves || (wavesReached == _bestWaves && totalKills > _bestKills))
        {
            newRecord = true;
        }

        if (wavesReached > _bestWaves)
        {
            _bestWaves = wavesReached;
            PlayerPrefs.SetInt(KEY_BEST_WAVES, _bestWaves);
        }

        if (totalKills > _bestKills)
        {
            _bestKills = totalKills;
            PlayerPrefs.SetInt(KEY_BEST_KILLS, _bestKills);
        }

        PlayerPrefs.Save();

        // Populate UI
        if (wavesText  != null) wavesText.text  = "Wave " + wavesReached;
        if (killsText  != null) killsText.text  = totalKills + (totalKills == 1 ? " Chicken Slain" : " Chickens Slain");
        if (bestWavesText != null) bestWavesText.text = "Best Wave: " + _bestWaves;
        if (bestKillsText != null) bestKillsText.text = "Best Kills: " + _bestKills;

        if (newBestBadge != null) newBestBadge.SetActive(newRecord);

        // Unlock cursor for the UI buttons
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;

        gameObject.SetActive(true);
    }

    // ── Buttons ────────────────────────────────────────────────────────────────

    /// <summary>Wire to your "Play Again" button.</summary>
    public void OnPlayAgainButton()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    /// <summary>Wire to your "Main Menu" button.</summary>
    public void OnMainMenuButton()
    {
        SceneManager.LoadScene("StartScreen");
    }

    // ── Utility ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Wipes all saved survival high scores.
    /// Hook to a "Reset Scores" button in settings if you want one.
    /// </summary>
    public static void ClearHighScores()
    {
        PlayerPrefs.DeleteKey(KEY_BEST_WAVES);
        PlayerPrefs.DeleteKey(KEY_BEST_KILLS);
        PlayerPrefs.Save();
        Debug.Log("[SurvivalDeathScreen] High scores cleared.");
    }
}
