using Newtonsoft.Json;


namespace WAVE
{
  public class Playlist
  {
    [JsonIgnore]
    public const string WrongPath = "Entered wrong path";


    [JsonProperty("name")]
    public string     Name      { get; private set; }

    [JsonProperty("songs")]
    public List<Song> Songs     { get; private set; }

    [JsonProperty("path")]
    public string     LocalPath { get; private set; }


    public Playlist(string path)
    {
      var pl = JsonConvert.DeserializeObject<Playlist>(File.ReadAllText(path));
      if (pl == null)
        throw new Exception(WrongPath);

      Name      = pl.Name;
      Songs     = pl.Songs;
      LocalPath = pl.LocalPath;
    }

    public Playlist(string name, List<Song> songs)
    {
      Name  = name;
      Songs = Songs.Concat(songs).ToList();
    }


    public void Add(Song song)
    {
      Songs = Songs.Append(song).ToList();
      File.WriteAllText(LocalPath, JsonConvert.SerializeObject(this));
    }

    public void Save(string path)
    {
      LocalPath = path;
      if (path != null && path != "")
        File.WriteAllText(path, JsonConvert.SerializeObject(this));
    }

    public void SetName(string newName)
    {
      Name = newName;
      if (LocalPath != null && LocalPath != "")
        File.WriteAllText(LocalPath, JsonConvert.SerializeObject(this));
    }
  }
}