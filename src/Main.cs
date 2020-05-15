using System;
using System.IO;
using System.Net;

using System.Collections.Generic;

using HBS.Logging;

using Harmony;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using System.Reflection;

using PathfindingFixes.Config;
using PathfindingFixes.Utils;

namespace PathfindingFixes {
  public class Main {
    public static ILog Logger;
    public static Config.Settings Settings { get; private set; }
    public static Assembly PathfindingFixesAssembly { get; set; }
    public static string Path { get; private set; }

    public static void InitLogger(string modDirectory) {
      Dictionary<string, LogLevel> logLevels = new Dictionary<string, LogLevel> {
        ["PathfindingFixes"] = LogLevel.Debug
      };
      LogManager.Setup(modDirectory + "/output.log", logLevels);
      Logger = LogManager.GetLogger("PathfindingFixes");
      Path = modDirectory;
    }

    public static void LogDebug(string message) {
      if (Main.Settings.DebugMode) Main.Logger.LogDebug(message);
    }

    public static void LogDebugWarning(string message) {
      if (Main.Settings.DebugMode) Main.Logger.LogWarning(message);
    }

    // Entry point into the mod, specified in the `mod.json`
    public static void Init(string modDirectory, string modSettings) {
      try {
        InitLogger(modDirectory);
        LoadSettings(modDirectory);
        //LoadData(modDirectory);
        //VersionCheck();
      } catch (Exception e) {
        Logger.LogError(e);
        Logger.Log("Error loading mod settings - using defaults.");
        Settings = new Config.Settings();
      }

      HarmonyInstance harmony = HarmonyInstance.Create("com.hamsterboo.PathfindingFixes");
      harmony.PatchAll(Assembly.GetExecutingAssembly());
    }

    private static void VersionCheck() {
      try {
        string modJson = new WebClient().DownloadString("https://raw.githubusercontent.com/HamsterBoo/PathfindingFixes/master/mod.json");
        JObject json = JObject.Parse(modJson);
        string version = (string)json["Version"];
        Main.Settings.GithubVersion = version;
      } catch (WebException) {
        // Do nothing if there's a problem getting the version from Github
      }
    }

    private static void LoadSettings(string modDirectory) {
      Logger.Log("Loading PathfindingFixes settings");
      JsonSerializerSettings serialiserSettings = new JsonSerializerSettings() {
        TypeNameHandling = TypeNameHandling.All
      };

      string settingsJsonString = File.ReadAllText($"{modDirectory}/settings.json");
      Settings = JsonConvert.DeserializeObject<Config.Settings>(settingsJsonString, serialiserSettings);

      string modJsonString = File.ReadAllText($"{modDirectory}/mod.json");
      JObject json = JObject.Parse(modJsonString);
      string version = (string)json["Version"];
      Settings.Version = version;
    }
  }
}