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
            timeText.text = RunTimeFormat.AsMinutesSeconds(t);
    }
}
