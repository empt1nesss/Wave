using Newtonsoft.Json;


namespace WAVE
{
  static class PlaylistManager
  {
    public const string WrongPath = "Entered wrong path";


    public class Playlist
    {
      [JsonProperty("name")]
      public string     Name;

      [JsonProperty("songs")]
      public List<Song> Songs;

      [JsonProperty("path")]
      public string     LocalPath;
    }


    private static int m_currentPlaylistIndex;
    private static int m_currentSongIndex;

    public static Playlist[]  Playlists { get; private set; }
    public static int         CurrentPlaylistIndex
    {
      get
      {
        return m_currentPlaylistIndex;
      }
      set
      {
        if (value >= 0 && value < Playlists.Length)
        {
          m_currentPlaylistIndex  = value;
          m_currentSongIndex      = -1;
        }
      }
    }
    public static int         CurrentSongIndex
    {
      get
      {
        return m_currentSongIndex;
      }
      set
      {
        if (m_currentPlaylistIndex < 0)
          return;

        if (value >= 0 && value < Playlists[m_currentPlaylistIndex].Songs.Count)
          m_currentSongIndex  = value;
      }
    }



    static PlaylistManager()
    {
      Playlists               = [];
      m_currentPlaylistIndex  = -1;
      m_currentSongIndex      = -1;
    }


    public static void LoadPlaylist(string playlistsDir)
    {
      if (!Path.Exists(playlistsDir))
        throw new Exception(WrongPath);

      string[] playlistsFiles = Directory.GetFiles(playlistsDir, "*.plst");
      foreach (var path in playlistsFiles)
      {
        Playlists = Playlists.Append(JsonConvert.DeserializeObject<Playlist>(File.ReadAllText(path))).ToArray();
      }

      if (Playlists.Length > 0)
        m_currentPlaylistIndex = 0;
    }


    public static void PlayBack()
    {
      if (m_currentPlaylistIndex == -1)
        return;

      if (m_currentPlaylistIndex >= Playlists.Length)
      {
        m_currentPlaylistIndex  = -1;
        m_currentSongIndex      = -1;
        return;
      }

      if (m_currentSongIndex == -1)
        return;

      if (m_currentSongIndex >= Playlists[m_currentPlaylistIndex].Songs.Count)
      {
        m_currentSongIndex = -1;
        return;
      }

      Player.PlayBack(Playlists[m_currentPlaylistIndex].Songs[m_currentSongIndex]);
    }

    public static void NextSong()
    {
      if (m_currentSongIndex == -1)
        return;

      if (++m_currentSongIndex >= Playlists[m_currentPlaylistIndex].Songs.Count)
      {
        Player.StopPlayBack();
        m_currentSongIndex = -1;
        return;
      }

      Player.PlayBack(Playlists[m_currentPlaylistIndex].Songs[m_currentSongIndex]);
    }

    public static void PrevSong()
    {
      if (m_currentSongIndex == -1)
        return;

      if (--m_currentSongIndex < 0)
      {
        Player.StopPlayBack();
        m_currentSongIndex = -1;
        return;
      }

      Player.PlayBack(Playlists[m_currentPlaylistIndex].Songs[m_currentSongIndex]);
    }


    public static void AddToPlaylist(Song song, int[] playlistsIndexes)
    {
      if (Playlists.Length == 0)
      {
        // CreatePlaylist("All Music", [ song ]);
        return;
      }

      Playlists[0].Songs = Playlists[0].Songs.Append(song).ToList();
      File.WriteAllText(Playlists[0].LocalPath, JsonConvert.SerializeObject(Playlists[0]));
      foreach (var i in playlistsIndexes)
        if (i >= 1 && i < Playlists.Length)
        {
          Playlists[i].Songs = Playlists[i].Songs.Append(song).ToList();
          File.WriteAllText(Playlists[i].LocalPath, JsonConvert.SerializeObject(Playlists[i]));
        }
    }
  }


}
