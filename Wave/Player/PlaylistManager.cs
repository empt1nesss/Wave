using Newtonsoft.Json;


namespace WAVE
{
  static class PlaylistManager
  {
    public const string WrongPath = "Entered wrong path";


    public class Playlist
    {
      [JsonProperty("name")]
      public string     Name  { get; private set; }

      [JsonProperty("count")]
      public uint       Count { get; private set; }

      [JsonProperty("songs")]
      public List<Song> Songs { get; private set; }
    }
    private static Playlist[] m_playlists;


    static PlaylistManager()
    {
      m_playlists = [];
    }


    public static void LoadPlaylist(string playlistsDir)
    {
      if (!Path.Exists(playlistsDir))
        throw new Exception(WrongPath);

      string[] playlistsFiles = Directory.GetFiles(playlistsDir, "*.plst");
      foreach (var path in playlistsFiles)
      {
        m_playlists = m_playlists.Append(JsonConvert.DeserializeObject<Playlist>(File.ReadAllText(path))).ToArray();
      }
    }
  }
}
