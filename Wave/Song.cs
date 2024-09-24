using System.Text.RegularExpressions;
using Newtonsoft.Json;


namespace WAVE
{
  public class Song
  {
    public const string WrongPath     = "Entered wrong path";
    public const string RequestFailed = "No internet conection";


    [JsonProperty("title")]
    private string m_title;

    [JsonProperty("artist")]
    private string m_artist;

    [JsonProperty("url")]
    private string m_url;

    // private string imageUrl;


    public string Title     { get { return m_title; } }
    public string Artist    { get { return m_artist; } }
    public string Url       { get { return m_url; } }

    public string FullName  { get { return $"{ m_artist } - { m_title }"; } }
    public string FileName  { get { return Regex.Replace($"{ m_artist } - { m_title }.mp3", "[<>:\"\\/|?*]", "_"); } }


    public Song(string title="", string artist="", string url="")
    {
      m_title       = title;
      m_artist      = artist;
      m_url         = url;
    }


    public async Task<string> Download(string path)
    {
      try
      {
        var client = new HttpClient();
        var get_response = await client.GetAsync(m_url);

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

      song.Tag.Title = m_title;
      song.Tag.Performers = [ m_artist ];
      song.Save();

      return "";
    }
  }
}
