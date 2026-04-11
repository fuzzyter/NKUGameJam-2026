
using UnityEngine;
using UnityEngine.UI;

public class GameHUD : MonoBehaviour
{
    public GameManager gameManager;
    public Slider staminaSlider;
    public Text dangerCountText;
    public Camera worldCamera;

    void Update()
    {
        if (!gameManager)
            gameManager = GameManager.Instance;
        if (!gameManager) return;

        if (staminaSlider)
        {
            staminaSlider.maxValue = gameManager.staminaMax;
            staminaSlider.value = gameManager.stamina;
        }

        Camera cam = worldCamera ? worldCamera : Camera.main;
        if (!dangerCountText || !cam || gameManager.encounterManager == null)
            return;

        int n = 0;
        foreach (EncounterObject e in gameManager.encounterManager.GetEncounters())
        {
            if (e.Collected || e.data == null) continue;
            if (e.data.type != EncounterType.Treasure && e.data.type != EncounterType.Hunter)
                continue;

            Vector3 v = cam.WorldToViewportPoint(e.transform.position);
            if (v.z > 0f && v.x > 0f && v.x < 1f && v.y > 0f && v.y < 1f)
                n++;
        }

        dangerCountText.text = n.ToString();
    }
}
