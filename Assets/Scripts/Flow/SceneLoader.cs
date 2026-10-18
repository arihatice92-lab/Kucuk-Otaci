// SceneLoader.cs
using UnityEngine.SceneManagement;

public static class SceneLoader
{
    public const string MainMenu = "MainMenu";
    public const string CharacterSelect = "CharacterSelect";
    public const string Village = "LokmaneliScene"; 

    public static void Load(string sceneName) => SceneManager.LoadScene(sceneName);
    public const string Intro = "IntroScene";
}