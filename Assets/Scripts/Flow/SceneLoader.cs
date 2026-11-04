public static class SceneLoader
{
    public const string MainMenu = "MainMenu";
    public const string CharacterSelect = "CharacterSelect";
    public const string Village = "LokmaneliScene";
    public const string Festival = "FestivalScene";

    // Geçişler kararıp açılarak yapılır (SceneTransition).
    public static void Load(string sceneName) => SceneTransition.Load(sceneName);
}