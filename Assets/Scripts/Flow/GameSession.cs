
using UnityEngine;

public static class GameSession
{
    private const string CharacterKey = "SelectedCharacter";

    public static PlayerCharacter SelectedCharacter { get; private set; } = Load();

    public static void SelectCharacter(PlayerCharacter c)
    {
        SelectedCharacter = c;
        PlayerPrefs.SetInt(CharacterKey, (int)c);
        PlayerPrefs.Save();
    }

    private static PlayerCharacter Load() =>
        (PlayerCharacter)PlayerPrefs.GetInt(CharacterKey, (int)PlayerCharacter.Girl);
}