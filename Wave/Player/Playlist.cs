using System.Runtime.InteropServices;
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
      if (!File.Exists(path) || !path.EndsWith(".plst"))
        throw new Exception(WrongPath);

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

      if (Songs == null)
        Songs = [];
      Songs = Songs.Concat(songs).ToList();
      
      LocalPath = "";
    }


    public void Add(Song song)
    {
      Songs = Songs.Append(song).ToList();
      if (LocalPath != null && LocalPath != "")
        File.WriteAllText(LocalPath, JsonConvert.SerializeObject(this));
    }

    public void Remove(int songIndex)
    {
      if (songIndex < 0 || songIndex >= Songs.Count)
        return;

      Songs.RemoveAt(songIndex);
      if (LocalPath != null && LocalPath != "")
        File.WriteAllText(LocalPath, JsonConvert.SerializeObject(this));
    }

    public void Save(string dir)
    {
      if (dir == null || !Directory.Exists(dir))
        throw new Exception(WrongPath);

      LocalPath = Path.Join(dir, Name, ".plst");
      File.WriteAllText(LocalPath, JsonConvert.SerializeObject(this));
    }

    public void SetName(string newName)
    {
      Name = newName;
      if (LocalPath != null && LocalPath != "")
        File.WriteAllText(LocalPath, JsonConvert.SerializeObject(this));
    }
  }
}