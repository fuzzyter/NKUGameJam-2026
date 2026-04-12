
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameHUD : MonoBehaviour
{
    public GameManager gameManager;
    public Slider staminaSlider;

    [Tooltip("Camera viewport inside Treasure + Hunter count (no distinction)")]
    public TextMeshProUGUI viewportTreasureHunterCountTMP;

    public Camera worldCamera;
    public Image[] hunterHearts;

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
        if (viewportTreasureHunterCountTMP && cam && gameManager.encounterManager != null)
        {
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

            viewportTreasureHunterCountTMP.text = n.ToString();
        }

        if (hunterHearts == null || hunterHearts.Length == 0)
            return;

        int lost = Mathf.Clamp(gameManager.hunterCount, 0, gameManager.maxHunter);
        int visible = Mathf.Clamp(gameManager.maxHunter - lost, 0, hunterHearts.Length);
        for (int i = 0; i < hunterHearts.Length; i++)
        {
            if (!hunterHearts[i]) continue;
            hunterHearts[i].enabled = i < visible;
        }
    }
}
