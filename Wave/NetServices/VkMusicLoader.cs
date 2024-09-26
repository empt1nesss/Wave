using Newtonsoft.Json;


namespace WAVE
{
  static class VkMusicLoader
  {
    public const string InvalidLoginInfo       = "Invalid login info";
    public const string InvalidUserAgent       = "Invalid user agent";
    public const string InvalidToken           = "Invalid token";
    public const string LogInFailed            = "Login critical error";
    public const string GetResponseFailed      = "Get response";
    public const string RequestFailed          = "No internet conection";
    public const string FailedToGetUserSongs   = "Get users songs critical error";
    public const string UsersAudioAccessDenied = "These users audios are private";


    private class Response
    {
      [JsonProperty("count")]
      public uint count = 0;

      [JsonProperty("items")]
      public List<Song> items = new List<Song>();
    }

    private class VkError
    {
      [JsonProperty("error_code")]
      public int    error_code;

      [JsonProperty("error_msg")]
      public string error_msg;


      public VkError()
      {
        error_code  = 0;
        error_msg   = "";
      }
    }

    private class PostJson
    {
      [JsonProperty("response")]
      public Response response;

      [JsonProperty("error")]
      public VkError  error;
    }


    private class Config
    {
      [JsonProperty("user-agent")]
      public string   UserAgent;

      [JsonProperty("token")]
      public string   Token;

      [JsonProperty("exceptions")]
      public string[] Exceptions;


      public Config()
      {
        UserAgent   = "";
        Token       = "";
        Exceptions  = [];
      }
    }

    private class SignInResponse
    {
      [JsonProperty("access_token")]
      public string accesToken;

      [JsonProperty("error")]
      public string error;

      [JsonProperty("validation_sid")]
      public string sid;

      [JsonProperty("captcha_sid")]
      public string captchaSid;

      [JsonProperty("captcha_img")]
      public string captchaImg;


      public SignInResponse()
      {
        accesToken  = "";
        error       = "";
        sid         = "";
        captchaSid  = "";
        captchaImg  = "";
      }
    }


    private static Config  m_cfg;


    public static bool Logged { get; private set; }


    public delegate string AskLogin   ();
    public delegate string AskPass    ();
    public delegate string Ask2FA     (bool firstTime);
    public delegate string AskCaptcha (string url);


    static VkMusicLoader()
    {
      Logged    = false;
      m_cfg     = new Config();
    }


    public static async Task Auth(
      AskLogin    askLogin,
      AskPass     askPass,
      Ask2FA      ask2FA,
      AskCaptcha  askCaptcha,
      bool        newUser     = false,
      bool        rememberMe  = true,
      string      cfgPath     = "config.json"
    )
    {
      if (Logged && !newUser)
        return;

      if (newUser || !File.Exists(cfgPath))
      {
        string login    = askLogin();
        string password = askPass();

        m_cfg.UserAgent = "KateMobileAndroid/56 lite-460 (Android 4.4.2; SDK 19; x86; unknown Android SDK built for x86; en)";
        
        await signIn(login, password, ask2FA, askCaptcha);

        if (rememberMe)
          File.WriteAllText(
            cfgPath,
            "{\n\t\"user-agent\": \"" + m_cfg.UserAgent + "\",\n\t\"token\": \"" + m_cfg.Token + "\"\n}"
          );
      }
      else
      {
        m_cfg = JsonConvert.DeserializeObject<Config>(
          File.ReadAllText(cfgPath)
        );
      }

      if (m_cfg.Token != "")
      {
        Logged = true;
        return;
      }
      else
        throw new Exception(LogInFailed);
    }


    public static async Task<Playlist> SearchSongs(
      string  songName,
      uint    count,
      uint    offset   = 0
    )
    {
      var parameters = new Dictionary<string, string>()
      {
        { "q",            songName                  },
        { "count",        Convert.ToString(count)   },
        { "offset",       Convert.ToString(offset)  },
        { "sort",         "0"                       },
        { "autocomplete", "1"                       }
      };
      
      var res = await getResponse("search", parameters);
      var name = $"_query={ songName }_count={ count }_offset={ offset }";
      res.SetName(name);
      return res;
    }

    public static async Task<Playlist> GetUsersSongs(uint userId, uint count, uint offset=0)
    {
      if (userId == 0)
        throw new Exception(UsersAudioAccessDenied);

      var parameters = new Dictionary<string, string>()
      {
        { "owner_id", Convert.ToString(userId)  },
        { "count",    Convert.ToString(count)   },
        { "offset",   Convert.ToString(offset)  }
      };

      var res = await getResponse("get", parameters);
      var name = res.Name + $"_user={ userId }_count={ count }_offset={ offset }";
      res.SetName(name);
      return res;
    }

    public static async Task<uint> GetUsersSongsCount(uint userId)
    {
      if (userId == 0)
        throw new Exception(UsersAudioAccessDenied);

      var parameters = new Dictionary<string, string>()
      {
        { "access_token", m_cfg.Token                 },
        { "https",        "1"                       },
        { "lang",         "ru"                      },
        { "extended",     "1"                       },
        { "v",            "5.131"                   },
        { "owner_id",     Convert.ToString(userId)  },
        { "count",        "1"                       },
        { "offset",       "0"                       }
      };
      
      try
      {
        var client = new HttpClient();
        client.DefaultRequestHeaders.Add("User-Agent", m_cfg.UserAgent);
        var postResponse = await client.PostAsync(
          $"https://api.vk.com/method/audio.get", 
          new FormUrlEncodedContent(parameters)
        );

        var content = await postResponse.Content.ReadAsStringAsync();

        var res = JsonConvert.DeserializeObject<PostJson>(content);

        if (res == null)
        {
          File.WriteAllText("response_error.json", content);
          throw new Exception(FailedToGetUserSongs);
        }
        if (res.error != null)
        {
          switch (res.error.error_code)
          {
          case 8:
            throw new Exception(InvalidUserAgent);
          case 201:
            throw new Exception(UsersAudioAccessDenied);
          case 1116:
            throw new Exception(InvalidToken);

          default:
            File.WriteAllText("response_error.json", content);
            throw new Exception(FailedToGetUserSongs);
          }
        }

        return res.response.count;
      }
      catch (HttpRequestException)
      {
        throw new Exception(RequestFailed);
      }
    }

    public static bool IsSongInExceptions(Song song)
    {
      if (m_cfg.Exceptions.Contains(song.FullName))
        return true;

      return false;
    }

    public static void Clear()
    {
      m_cfg.UserAgent   = "";
      m_cfg.Token       = "";
      m_cfg.Exceptions  = [];

      Logged = false;
    }


    private static async Task signIn(
      string      login,
      string      password,
      Ask2FA      ask2FA,
      AskCaptcha  askCaptcha
    )
    {
      var queryParams = new Dictionary<string, string>()
      {
        { "grant_type",     "password"              },
        { "client_id",      "2685278"               },
        { "client_secret",  "lxhD8OD7dMsqtXIm5IUY"  },
        { "username",       login                   },
        { "password",       password                },
        { "scope",          "audio,offline"         },
        { "2fa_supported",  "1"                     },
        { "force_sms",      "1"                     },
        { "v",              "5.131"                 }
      };

      string  responseJson  = "";
      var     response      = new SignInResponse();

      try
      {
        do
        {
          var client = new HttpClient();
          client.DefaultRequestHeaders.Add("User-Agent", m_cfg.UserAgent);
          var postResponse = await client.PostAsync(
            "https://oauth.vk.com/token",
            new FormUrlEncodedContent(queryParams)
          );
          responseJson  = await postResponse.Content.ReadAsStringAsync();
          response      = JsonConvert.DeserializeObject<SignInResponse>(responseJson);

          if (response == null)
          {
            File.WriteAllText("response_error.json", responseJson);
            throw new Exception(LogInFailed);
          }

          if (responseJson.Contains("error"))
          {
            switch(response.error)
            {
            case "need_validation":
              await requestCode(response.sid);
              queryParams.Add("code", ask2FA(true));
              break;
            
            case "invalid_request":
              queryParams["code"] = ask2FA(false);
              break;

            case "need_captcha":
              queryParams.Add("captcha_sid", response.captchaSid);
              queryParams.Add("captcha_key", askCaptcha(response.captchaImg));
              break;

            case "invalid_client":
              Clear();
              throw new Exception(InvalidLoginInfo);
            
            default:
              File.WriteAllText("response_error.json", responseJson);
              throw new Exception(LogInFailed);
            }
          }
          else if (responseJson.Contains("access_token"))
          {
            m_cfg.Token = response.accesToken;
            return;
          }
        } while(responseJson.Contains("error"));
      }
      catch (HttpRequestException)
      {
        File.WriteAllText("response_error.json", responseJson);
        throw new Exception(RequestFailed);
      }

      Clear();
      throw new Exception(LogInFailed);
    }

    private static async Task<string> requestCode(string sid)
    {
      var queryParams = new Dictionary<string, string>()
      {
        { "sid",  sid     },
        { "v" ,   "5.131" }
      };

      var client = new HttpClient();
      client.DefaultRequestHeaders.Add("User-Agent", m_cfg.UserAgent);
      
      try
      {
        var postResponse = await client.PostAsync(
          "https://api.vk.com/method/auth.validatePhone",
          new FormUrlEncodedContent(queryParams)
        );

        return await postResponse.Content.ReadAsStringAsync();
      }
      catch (HttpRequestException)
      {
        throw new Exception(RequestFailed);
      }
    }

    private static async Task<Playlist> getResponse(
      string                      method,
      Dictionary<string, string>  parameters
    )
    {
      var _parameters = new Dictionary<string, string>()
      {
        { "access_token", m_cfg.Token },
        { "https",        "1"         },
        { "lang",         "ru"        },
        { "extended",     "1"         },
        { "v",            "5.131"     }
      };
      _parameters = _parameters.Concat(parameters).ToDictionary(x => x.Key, x => x.Value);
      
      var client = new HttpClient();
      client.DefaultRequestHeaders.Add("User-Agent", m_cfg.UserAgent);
      try
      {
        var postResponse = await client.PostAsync(
          $"https://api.vk.com/method/audio.{ method }", 
          new FormUrlEncodedContent(_parameters)
        );
        var content = await postResponse.Content.ReadAsStringAsync();
        var res = JsonConvert.DeserializeObject<PostJson>(content);
        if (res == null)
        {
          File.WriteAllText("response_error.json", content);
          throw new Exception(GetResponseFailed);
        }
        if (res.error != null)
        {
          switch (res.error.error_code)
          {
          case 1116:
            throw new Exception(InvalidToken);
          
          default:
            File.WriteAllText("response_error.json", content);
            throw new Exception(GetResponseFailed);
          }
        }

        if (res.response.items.Count == 1)
          if (res.response.items[0].Url == "https://vk.com/mp3/audio_api_unavailable.mp3")
            throw new Exception(InvalidUserAgent);

        return new Playlist("vk", res.response.items);
      }
      catch (HttpRequestException)
      {
        throw new Exception(RequestFailed);
      }
    }
  }
}
