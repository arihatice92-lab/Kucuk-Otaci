using UnityEngine;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    [SerializeField] private Button startButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private GameObject settingsPanel;   // bir sonraki adımda bağlanacak, şimdilik boş kalabilir

    private void Start()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    private void OnEnable()
    {
        startButton.onClick.AddListener(OnStartClicked);
        settingsButton.onClick.AddListener(OnSettingsClicked);
        quitButton.onClick.AddListener(OnQuitClicked);
    }

    private void OnDisable()
    {
        startButton.onClick.RemoveListener(OnStartClicked);
        settingsButton.onClick.RemoveListener(OnSettingsClicked);
        quitButton.onClick.RemoveListener(OnQuitClicked);
    }

    private void OnStartClicked() => SceneLoader.Load(SceneLoader.CharacterSelect);

    private void OnSettingsClicked()
    {
        if (settingsPanel != null) settingsPanel.SetActive(!settingsPanel.activeSelf);
    }

    private void OnQuitClicked()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}