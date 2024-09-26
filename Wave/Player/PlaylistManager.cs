using Newtonsoft.Json;


namespace WAVE
{
  static class PlaylistManager
  {
    public const string WrongPath = "Entered wrong path";


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


    public static void LoadPlaylists(string playlistsDir)
    {
      if (!Path.Exists(playlistsDir))
        throw new Exception(WrongPath);

      string[] playlistsFiles = Directory.GetFiles(playlistsDir, "*.plst");
      foreach (var path in playlistsFiles)
      {
        var plst = JsonConvert.DeserializeObject<Playlist>(File.ReadAllText(path));
        if (plst != null)
          Playlists = Playlists.Append(plst).ToArray();
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
  }
}
