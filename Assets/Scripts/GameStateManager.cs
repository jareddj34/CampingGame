using UnityEngine;

public class GameStateManager : MonoBehaviour
{
    public static GameStateManager Instance { get; private set; }
    public bool IsPlayerInputEnabled { get; private set; } = true;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void SetInputEnabled(bool enabled)
    {
        IsPlayerInputEnabled = enabled;
    }
}
