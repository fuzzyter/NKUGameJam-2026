using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class MenuSceneButtons : MonoBehaviour
{
    [Tooltip("play scene name")]
    public string mapSceneName = "MapScene";

    public void StartGame() => LoadMapScene();

    public void RestartGame() => LoadMapScene();

    public void LoadMapScene()
    {
        if (string.IsNullOrWhiteSpace(mapSceneName))
        {
            Debug.LogWarning("MenuSceneButtons: mapSceneName is empty.");
            return;
        }

        SceneManager.LoadScene(mapSceneName);
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
