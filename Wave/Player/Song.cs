using System.Text.RegularExpressions;
using Newtonsoft.Json;


namespace WAVE
{
  public class Song
  {
    public const string WrongPath     = "Entered wrong path";
    public const string RequestFailed = "No internet conection";


    [JsonProperty("title")]
    public string Title     { get; private set; }

    [JsonProperty("artist")]
    public string Artist    { get; private set; }

    [JsonProperty("url")]
    public string Url       { get; private set; }

    [JsonProperty("duration")]
    public uint   Duration  { get; private set; }

    // imageUrl;
    

    public string FullName  { get { return $"{ Artist } - { Title }"; } }
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


    public Song(string title="", string artist="", string url="")
    {
      Title       = title;
      Artist      = artist;
      Url         = url;
    }


    public async Task<string> Download(string path)
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
            return $"File { FileName } already exists";
        }

        var file = new FileStream(Path.Join(path, FileName), FileMode.Create, FileAccess.Write);
        await get_response.Content.CopyToAsync(file);
        file.Close();
      }
      catch (HttpRequestException)
      {
        throw new Exception(RequestFailed);
      }

      var song = TagLib.File.Create(Path.Join(path, FileName));

      song.Tag.Title = Title;
      song.Tag.Performers = [ Artist ];
      song.Save();

      return "";
    }
  }
}
