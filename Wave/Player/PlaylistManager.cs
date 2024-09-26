using Newtonsoft.Json;


namespace WAVE
{
  static class PlaylistManager
  {
    public const string WrongPath = "Entered wrong path";


    public static int CurrentPlaylistIndex  { get; private set; }
    public static int CurrentSongIndex      { get; private set; }
    public static int CurrentQueueSongIndex { get; private set; }

    public static List<Playlist>  Playlists   { get; private set; }
    public static Playlist        Queue       { get; private set; }
    public static bool            IsQueueNow  { get; private set; }


    static PlaylistManager()
    {
      Playlists             = [];
      Queue                 = new Playlist("queue", []);
      IsQueueNow            = false;
      CurrentPlaylistIndex  = -1;
      CurrentSongIndex      = -1;
      CurrentQueueSongIndex = -1;
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
          Playlists = Playlists.Append(plst).ToList();
      }

      if (Playlists.Count > 0)
        CurrentPlaylistIndex = 0;
    }

    public static void RemoveSongFromPlaylist(int playlistIndex, int songIndex)
    {
      if (playlistIndex < 0 || playlistIndex >= Playlists.Count)
        return;

      if (songIndex < 0 || songIndex >= Playlists[playlistIndex].Songs.Count)
        return;

      Playlists[playlistIndex].Remove(songIndex);
      if (playlistIndex == CurrentPlaylistIndex)
      {
        if (CurrentSongIndex == songIndex)
          Player.StopPlayBack();

        if (CurrentSongIndex == Playlists[playlistIndex].Songs.Count)
          CurrentSongIndex = -1;
        else if (CurrentSongIndex > songIndex)
          --CurrentSongIndex;
      }
    }

    public static void RemoveSongFromQueue(int songIndex)
    {
      if (songIndex < 0 || songIndex >= Queue.Songs.Count)
        return;


      Queue.Remove(songIndex);
      if (IsQueueNow)
      {
        if (songIndex == CurrentQueueSongIndex)
        {
          NextSong();
          return;
        }
        else if (CurrentSongIndex == Queue.Songs.Count)
          CurrentSongIndex = -1;
        else if (CurrentSongIndex > songIndex)
          --CurrentSongIndex;
      }
    }

    public static void RemovePlaylist(int playlistIndex)
    {
      if (playlistIndex < 0 || playlistIndex >= Playlists.Count)
        return;

      Playlists.RemoveAt(playlistIndex);
      if (playlistIndex == CurrentPlaylistIndex && !IsQueueNow)
      {
        Player.StopPlayBack();
        CurrentSongIndex      = -1;
        CurrentPlaylistIndex  = -1;
      }
      else if (CurrentPlaylistIndex > playlistIndex)
        --CurrentPlaylistIndex;
      
    }


    public static void PlayBack(int playlistIndex, int songIndex)
    {
      if (playlistIndex < 0 || playlistIndex >= Playlists.Count)
        return;

      if (songIndex < 0 || songIndex >= Playlists[playlistIndex].Songs.Count)
        return;

      Player.PlayBack(Playlists[playlistIndex].Songs[songIndex]);
      IsQueueNow           = false;
      CurrentPlaylistIndex = playlistIndex;
      CurrentSongIndex     = songIndex;
    }

    public static void PlayBackFromQueue(int songIndex)
    {
      if (songIndex < 0 || songIndex >= Queue.Songs.Count)
        return;

      Player.PlayBack(Queue.Songs[songIndex]);
      IsQueueNow             = true;
      CurrentQueueSongIndex  = songIndex;
    }

    public static void NextSong()
    {
      if (IsQueueNow)
      {
        if (CurrentQueueSongIndex < 0)
          return;

        if (CurrentQueueSongIndex >= Queue.Songs.Count)
        {
          Player.StopPlayBack();
          Queue.Remove(CurrentQueueSongIndex);
          CurrentQueueSongIndex = -1;
          return;
        }

        Queue.Remove(CurrentQueueSongIndex);
        Player.PlayBack(Queue.Songs[CurrentQueueSongIndex]);
      }
      else
      {
        if (CurrentSongIndex < 0)
          return;

        if (++CurrentSongIndex >= Playlists[CurrentPlaylistIndex].Songs.Count)
        {
          Player.StopPlayBack();
          CurrentSongIndex = -1;
          return;
        }

        Player.PlayBack(Playlists[CurrentPlaylistIndex].Songs[CurrentSongIndex]);
      }
    }

    public static void PrevSong()
    {
      if (IsQueueNow)
      {
        if (CurrentQueueSongIndex < 0)
          return;

        if (--CurrentQueueSongIndex < 0)
        {
          Player.StopPlayBack();
          CurrentQueueSongIndex = -1;
          return;
        }

        Player.PlayBack(Queue.Songs[CurrentQueueSongIndex]);
      }
      else
      {
        if (CurrentSongIndex < 0)
          return;

        if (--CurrentSongIndex < 0)
        {
          Player.StopPlayBack();
          CurrentSongIndex = -1;
          return;
        }

        Player.PlayBack(Playlists[CurrentPlaylistIndex].Songs[CurrentSongIndex]);
      }
    }
  }
}
