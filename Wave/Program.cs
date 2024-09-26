using System.Text.RegularExpressions;


namespace WAVE
{
  class Program
  {
    static async Task Main(string[] args)
    {
      PlaylistManager.LoadPlaylists(".");
      System.Console.WriteLine("Name: " + PlaylistManager.Playlists[0].Name);
      foreach (var song in PlaylistManager.Playlists[0].Songs)
        System.Console.WriteLine(song.FullName);

      await DownloadSong();

      PlaylistManager.CurrentSongIndex = 3;
      PlaylistManager.PlayBack();
      Thread.Sleep(5000);
      PlaylistManager.NextSong();
      Thread.Sleep(5000);
      PlaylistManager.NextSong();
      Thread.Sleep(5000);
      PlaylistManager.NextSong();
      Thread.Sleep(5000);
      PlaylistManager.NextSong();
      Thread.Sleep(5000);

    }


    static async Task DownloadUsersSong()
    {
      Console.InputEncoding   = System.Text.Encoding.UTF8;
      Console.OutputEncoding  = System.Text.Encoding.UTF8;

      await VkMusicLoader.Auth(AskLogin, AskPass, Ask2FA, AskCaptcha);
      if (!VkMusicLoader.Logged)
      {
        Console.WriteLine("Failed to sign in");
        return;
      }

      Console.Write("Enter user id\n > ");
      uint userId = Convert.ToUInt32(Console.ReadLine());

      Console.Write("Enter path to download directory\n > ");
      string dir = Console.ReadLine();

      Console.Write("How much tracks do you need (0 = All)\n > ");
      uint count = Convert.ToUInt32(Console.ReadLine());
      uint overallCount = await VkMusicLoader.GetUsersSongsCount(userId);
      if (count == 0 || count > overallCount)
        count = overallCount;

      uint page = count / 32 + 1;
      
      while (page > 0)
      {
        Playlist songs;
        if (page - 1 == count / 32)
          songs = await VkMusicLoader.GetUsersSongs(userId, count % 32, (page - 1) * 32);
        else
          songs = await VkMusicLoader.GetUsersSongs(userId, 32, (page - 1) * 32);

        if (songs.Songs.Count == 0)
        {
          --page;
          continue;
        }

        for (int i = songs.Songs.Count - 1; i >= 0; --i)
        {
          if (VkMusicLoader.IsSongInExceptions(songs.Songs[i]))
            Console.WriteLine($"[!] Skipped exception { songs.Songs[i].FullName }.");

          try
          {
            await songs.Songs[i].Download(dir);
          }
          catch (Exception exc)
          {
            System.Console.WriteLine("[-] Error:\n" + exc.Message);
          }

        }

        --page;
      }
    }

    static async Task DownloadSong()
    {
      Console.InputEncoding   = System.Text.Encoding.UTF8;
      Console.OutputEncoding  = System.Text.Encoding.UTF8;

      await VkMusicLoader.Auth(AskLogin, AskPass, Ask2FA, AskCaptcha);
      if (!VkMusicLoader.Logged)
      {
        Console.WriteLine("Failed to sign[1..] in");
        return;
      }

      Console.Write("Enter song name\n > ");
      string songName = Console.ReadLine();

      uint page = 0;
      Task[] dlTasks = [];
      var playBackSong = new Song();
      while (true)
      {
        Console.Clear();

        var songs = await VkMusicLoader.SearchSongs(songName, 10, page * 10);
        for (int i = 0; i < songs.Songs.Count; ++i)
          Console.WriteLine($"{ i + 1 }:\t{ songs.Songs[i].FullName }");

        Console.Write("\n > ");
        string cmd = Console.ReadLine();

        if (Regex.IsMatch(cmd, @"^\d+$"))
        {
          int index = Convert.ToInt32(cmd) - 1;
          if (index < 0 || index >= songs.Songs.Count)
            continue;

          dlTasks = dlTasks.Append(songs.Songs[index].Download("Music")).ToArray();
          PlaylistManager.Playlists[0].Add(songs.Songs[index]);
        }
        else if (cmd == "q")
          break;
        else if (cmd == "n")
        {
          if (songs.Songs.Count == 10)
            ++page;
        }
        else if (cmd == "p")
        {
          if (page > 0)
            --page;
        }
        else if (cmd[0] == '/')
        {
          songName = cmd[1..];
          page = 0;
        }
      }
      if (dlTasks.Length > 0)
      {
        Console.Clear();
        Console.WriteLine("Some traks are still downloading. Please wait.");
      }
        
      foreach (var task in dlTasks)
        await task;
    }


    static string AskLogin()
    {
      Console.Write("Enter login\n > ");
      return Console.ReadLine();
    }

    static string AskPass()
    {
      Console.Write("Enter password\n > ");
      return Console.ReadLine();
    }

    static string Ask2FA(bool firstTime)
    {
      if (firstTime)
        Console.Write("Enter last 6 digits of number what calling you\n > ");
      else
        Console.Write("Wrong code! Enter it again\n > ");
      return Console.ReadLine();
    }

    static string AskCaptcha(string url)
    {
      Console.Write("Enter captcha, image: { url }\n > ");
      return Console.ReadLine();
    }
  }
}
