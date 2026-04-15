using UnityEngine;

public class UiSoundOnClick : MonoBehaviour
{
    public void PlayClick() => GameAudioManager.I?.PlayUiClick();
}
