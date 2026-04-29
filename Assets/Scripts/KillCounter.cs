using UnityEngine;
using TMPro;

public class KillCounter : MonoBehaviour
{

    public static KillCounter Instance;

    public int killCount = 0;

    public TextMeshProUGUI killCountText;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void AddKill()
    {
        killCount++;
        killCountText.text = "Wings: " + killCount;
    }
}
