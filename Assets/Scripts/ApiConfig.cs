/// <summary>
/// Central configuration for the backend API connection.
/// Change BaseUrl when deploying to production.
/// </summary>
public static class ApiConfig
{
    // Production
    public static string BaseUrl = "https://beatnusa-bertbackend-production.up.railway.app";
    //// Local development
    // public static string BaseUrl = "http://localhost:8000";

    // API endpoint paths
    public static string SongsUpload   => BaseUrl + "/api/songs/upload";
    public static string SongsList     => BaseUrl + "/api/songs";
    public static string SongStatus(string code) => BaseUrl + "/api/songs/" + code + "/status";
    public static string Beatmap(string code)    => BaseUrl + "/api/beatmaps/" + code;
    public static string GameResults   => BaseUrl + "/api/game-results";
    public static string SongAudio(string code)    => BaseUrl + "/api/songs/" + code + "/audio";
    public static string Leaderboard(string code) => BaseUrl + "/api/game-results/" + code + "/leaderboard";
}
