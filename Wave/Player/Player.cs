using NAudio.Wave;


namespace WAVE
{
  static class Player
  {
    public const string SongNotFound    = "Incorrect song link";
    public const string WrongFileFormat = "Wrong file format";


    static private Mp3FileReader          ? m_mp3Reader;
    static private WaveFileReader         ? m_wavReader;
    static private MediaFoundationReader  ? m_audioNetReader;
    static private WaveOutEvent           ? m_audioStream;
    static private float                    m_volume;
    
    static public Song    CurrentSong   { get; private set; }
    static public string  TrackFormat   { get; private set; }
    static public bool    IsPause       { get; private set; }
    static public bool    IsPlayBackEnd
    {
      get
      {
        if (CurrentSong == null)
          return false;

        return m_audioStream.PlaybackState == PlaybackState.Stopped;
      }
    }
    static public float Volume
    {
      get
      {
        return m_volume * 100;
      }
      set
      {
        m_volume = value / 100.0f;
        if (m_volume > 1.0f)
          m_volume = 1.0f;
        else if (m_volume < 0.0f)
          m_volume = 0.0f;

        if (CurrentSong != null)
          m_audioStream.Volume = m_volume;
      }
    }
    static public double Time
    {
      get
      {
        if (CurrentSong == null)
          return 0.0;
        if (TrackFormat == "net")
          return m_audioNetReader.CurrentTime.TotalSeconds;
        else if (TrackFormat == "mp3")
          return m_mp3Reader.CurrentTime.TotalSeconds;
        else if (TrackFormat == "wav")
          return m_wavReader.CurrentTime.TotalSeconds;
        
        return 0.0;
      }
      set
      {
        if (CurrentSong != null)
        {
          if (value >= CurrentSong.Duration)
            StopPlayBack();
          else if (value < 0.0)
          {
            if (TrackFormat == "net")
              m_audioNetReader.CurrentTime = TimeSpan.FromSeconds(0.0);
            else if (TrackFormat == "mp3")
              m_mp3Reader.CurrentTime = TimeSpan.FromSeconds(0.0);
            else if (TrackFormat == "wav")
              m_wavReader.CurrentTime = TimeSpan.FromSeconds(0.0);
          }
          else
            if (TrackFormat == "net")
              m_audioNetReader.CurrentTime = TimeSpan.FromSeconds(value);
            if (TrackFormat == "mp3")
              m_mp3Reader.CurrentTime = TimeSpan.FromSeconds(value);
            else if (TrackFormat == "wav")
              m_wavReader.CurrentTime = TimeSpan.FromSeconds(value);
        }
      }
    }


    static Player()
    {
      IsPause       = true;
      TrackFormat   = "";
      Volume        = 15.0f;

      m_audioStream = new WaveOutEvent();
    }


    public static void PlayBack(Song song)
    {
      StopPlayBack();
      
      if (song.LocalPath == null)
      {
        if (song.Url == null)
          throw new Exception(SongNotFound);

        m_audioNetReader = new MediaFoundationReader(song.Url);
        m_audioStream.Init(m_audioNetReader);
        TrackFormat = "net";
      }
      else if (song.LocalPath.EndsWith(".mp3"))
      {
        m_mp3Reader = new Mp3FileReader(song.LocalPath);
        m_audioStream.Init(m_mp3Reader); 
        TrackFormat = "mp3";
      }
      else if (song.LocalPath.EndsWith(".wav"))
      {
        m_wavReader = new WaveFileReader(song.LocalPath);
        m_audioStream.Init(m_wavReader);
        TrackFormat = "wav";
      }
      else
        throw new Exception(WrongFileFormat);
      

      CurrentSong = song;

      m_audioStream.Volume = Volume / 100.0f;
      m_audioStream.Play();

      IsPause = false;
    }

    public static void StopPlayBack()
    {
      if (CurrentSong != null)
      {
        m_audioStream.Stop();
        IsPause = true;

        if (TrackFormat == "net")
          m_audioNetReader.Close();
        if (TrackFormat == "mp3")
          m_mp3Reader.Close();
        else if (TrackFormat == "wav")
          m_wavReader.Close();

        CurrentSong = null;
      }
    }

    public static void PauseOrResumePlayBack()
    {
      if (CurrentSong == null)
        return;

      if (IsPause)
      {
        m_audioStream.Play();
        IsPause = false;
      }
      else
      {
        m_audioStream.Pause();
        IsPause = true;
      }
    }

    static public void Pause()
    {
      if (!IsPause && CurrentSong != null)
      {
        m_audioStream.Pause();
        IsPause = true;
      }
    }

    static public void Resume()
    {
      if (IsPause && CurrentSong != null)
      {
        m_audioStream.Play();
        IsPause = false;
      }
    }
  }
}
