namespace MiitsuColorController.Models
{
    /// <summary>
    /// Enum for API-related errors.
    /// </summary>
    public enum ResourceKey : int
    {
        VTSAuthToken = 1,
        VTSWebsocketURI = 6,
        TwitchUserName = 2,
        TwitchAuthToken = 11,

        ConnectVTSOnStart = 4,
        ConnectTwitchOnStart = 5,
        ReconnectVTSOnError = 24,
        ReconnectTwitchOnError = 25,

        WindowHeight = 7,
        WindowWidth = 8,
        WindowPositionX = 9,
        WindowPositionY = 10,

        Activated = 14,
        SelectedArtmesh = 12,
        SelectedTags = 13,
        MessageCount = 15,
        ColoringMethod = 16,
        RedEmote = 17,
        GreenEmote = 18,
        BlueEmote = 19,
        Interpolation = 20,
        Duration = 21,
        VValue = 22,
        SValue = 23,
    }
}