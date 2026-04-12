using UnityEngine;

public static class RunTimeFormat
{
    
    public static string AsMinutesSeconds(float seconds)
    {
        int t = Mathf.Max(0, Mathf.FloorToInt(seconds));
        int m = t / 60;
        int s = t % 60;
        return $"{m:00}:{s:00}";
    }
}
