// Canvas 안 구성 예:
// Canvas (항상 활성, Canvas Scaler 권장)
//  └ Panel "AreaChoice" (처음 비활성) ← panel 로 연결 (이 스크립트는 Canvas에 붙이는 걸 권장)
//      ├ Text
//      ├ Button Yes → yesButton
//      └ Button No  → noButton
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
        _yes?.Invoke();
    }

    void OnNoClicked()
    {
        _no?.Invoke();
    }
}
