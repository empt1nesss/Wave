using System.Text.RegularExpressions;
using NAudio.Wave;
using Newtonsoft.Json;


namespace WAVE
{
  public class Song
  {
    [JsonIgnore]
    public const string FileAlreadyExists = "File already exists";

    [JsonIgnore]
    public const string WrongPath         = "Entered wrong path";

    [JsonIgnore]
    public const string WrongFormat       = "Format doesn't support";

    [JsonIgnore]
    public const string RequestFailed     = "No internet conection";


    [JsonProperty("title")]
    public string Title     { get; private set; }

    [JsonProperty("artist")]
    public string Artist    { get; private set; }

    [JsonProperty("url")]
    public string Url       { get; private set; }

    [JsonProperty("path")]
    public string LocalPath { get; private set; }

    [JsonProperty("duration")]
    public uint   Duration  { get; private set; }

    // imageUrl;
    

    [JsonIgnore]
    public string FullName  { get { return $"{ Artist } - { Title }"; } }
    
    [JsonIgnore]
    public string FileName
    {
      get
      {
        string format = ".mp3";
        if (Url.EndsWith(".wav"))
          format = ".wav";

        return Regex.Replace($"{ Artist } - { Title }" + format, "[<>:\"\\/|?*]", "_");
      }
    }
    

    public Song(string path)
    {
      if (!File.Exists(path))
        throw new Exception(WrongPath);

      string trackFormat = "mp3";
      if (path.EndsWith(".wav"))
        trackFormat = "wav";
      else if (!path.EndsWith(".mp3"))
        throw new Exception(WrongFormat);

      LocalPath = path;
      Url       = "";

      var song = TagLib.File.Create(Path.Join(path, FileName));

      Title   = song.Tag.Title;
      if (Title == "")
        Title = Path.GetFileName(path[..^4]);

      Artist = "";
      for (int i = 0; i < song.Tag.Performers.Length; ++i)
      {
        if (i == 0)
          Artist += song.Tag.Performers[0];
        else if (i == 1)
          Artist += "feat. " + song.Tag.Performers[0];
        else
          Artist += ", " + song.Tag.Performers[0];
      }

      if (trackFormat == "mp3")
      {
        var mp3Reader = new Mp3FileReader(path);
        Duration = (uint)mp3Reader.TotalTime.TotalSeconds;
      }
      else
      {
        var wavReader = new WaveFileReader(path);
        Duration = (uint)wavReader.TotalTime.TotalSeconds;
      }
    }

    public Song(string title, string artist, string url, string path)
    {
      Title       = title;
      Artist      = artist;
      Url         = url;
      LocalPath   = path;
    }


    public async Task Download(string path)
    {
      try
      {
        var client = new HttpClient();
        var get_response = await client.GetAsync(Url);

        if (get_response.IsSuccessStatusCode)
        {
          try
          {
            if (!Directory.Exists(path))
              Directory.CreateDirectory(path);
          }
          catch
          {
            throw new Exception(WrongPath);
          }

          if (File.Exists(Path.Join(path, FileName)))
            throw new Exception(FileAlreadyExists);
        }

        var file = new FileStream(Path.Join(path, FileName), FileMode.Create, FileAccess.Write);
        await get_response.Content.CopyToAsync(file);
        file.Close();
      }
      catch (HttpRequestException)
      {
        throw new Exception(RequestFailed);
      }

      LocalPath = Path.Join(path, FileName);

      var song = TagLib.File.Create(Path.Join(path, FileName));

      song.Tag.Title = Title;
      song.Tag.Performers = [ Artist ];
      song.Save();
    }
  }
}
