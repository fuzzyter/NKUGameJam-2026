
using System;
using UnityEngine;
using UnityEngine.UI;

public class AreaConfirmUI : MonoBehaviour
{
    public GameObject panel;
    public Button yesButton;
    public Button noButton;

    Action _yes;
    Action _no;
    CanvasGroup _panelGroup;

    void Awake()
    {
        if (yesButton)
            yesButton.onClick.AddListener(OnYesClicked);
        if (noButton)
            noButton.onClick.AddListener(OnNoClicked);

        if (panel == null)
            return;

        if (panel == gameObject)
        {
            _panelGroup = panel.GetComponent<CanvasGroup>() ?? panel.AddComponent<CanvasGroup>();
            SetPanelVisible(false);
        }
        else
            panel.SetActive(false);
    }

    void SetPanelVisible(bool on)
    {
        if (panel == null) return;
        if (panel == gameObject && _panelGroup != null)
        {
            _panelGroup.alpha = on ? 1f : 0f;
            _panelGroup.interactable = on;
            _panelGroup.blocksRaycasts = on;
        }
        else
            panel.SetActive(on);
    }

    public void Show(Action onYes, Action onNo)
    {
        _yes = onYes;
        _no = onNo;
        transform.SetAsLastSibling();
        var root = GetComponentInParent<Canvas>();
        if (root)
            root.transform.SetAsLastSibling();
        SetPanelVisible(true);
    }

    public void Hide()
    {
        SetPanelVisible(false);
        _yes = null;
        _no = null;
    }

    void OnYesClicked()
    {
        GameAudioManager.I?.PlayUiClick();
        _yes?.Invoke();
    }

    void OnNoClicked()
    {
        GameAudioManager.I?.PlayUiClick();
        _no?.Invoke();
    }
}
