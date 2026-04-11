using UnityEngine;
using UnityEngine.UI;

public class EndRunUI : MonoBehaviour
{
    public Text timeText;
    public Text titleText;

    void Start()
    {
        float t = PlayerPrefs.GetFloat("LastRunTime", 0f);
        bool win = PlayerPrefs.GetInt("LastRunVictory", 0) != 0;

        if (titleText)
            titleText.text = win ? "Victory" : "Game Over";

        if (timeText)
        {
            int totalSec = Mathf.FloorToInt(t);
            int m = totalSec / 60;
            int s = totalSec % 60;
            int frac = Mathf.FloorToInt((t - totalSec) * 100f);
            timeText.text = $"{m:00}:{s:00}.{frac:00}";
        }
    }
}
