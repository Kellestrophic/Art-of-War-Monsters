// Assets/NEWSCRIPTS/Multiplayer/MatchContext.cs
using UnityEngine;

public static class MatchContext
{
    public enum Path { None, Direct, AI }

    // How this match was started (hub choice)
    public static Path CurrentPath = Path.None;

    // Player selections made in the menus
    public static string LocalSelectedCharacter = "dracula";  // set by Character Select
    public static string LocalSelectedFrameKey = "bronze_frame"; // optional cosmetic
    public static string LocalSelectedTitleKey = "scaredbaby_title"; // optional

    // Relay
    public static string LastJoinCode;           // host displays this / client typed this

    // Scene names (override if yours differ)
    public static string MultiplayerSceneName = "Multiplayer";
    public static string ArenaSceneName        = "MP_Arena_1";

    public static void ResetForNewMatch()
    {
        // don’t wipe cosmetics/character unless you want to
        LastJoinCode = null;
        CurrentPath = Path.None;
    }
}
