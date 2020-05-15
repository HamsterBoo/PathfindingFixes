using Newtonsoft.Json;

using System.Collections.Generic;

namespace PathfindingFixes.Config {
  public class Settings {
    [JsonProperty("DebugMode")]
    public bool DebugMode { get; set; } = false;

    [JsonProperty("VersionCheck")]
    public bool VersionCheck { get; set; } = true;

    [JsonIgnore]
    public string Version { get; set; } = "0.0.0";

    [JsonIgnore]
    public string GithubVersion { get; set; } = "0.0.0";

    [JsonProperty("NumNodesPerFrame")]
    public int NumNodesPerFrame { get; set; } = 25;

    [JsonProperty("NumNodesPerFrameWhileBlocked")]
    public int NumNodesPerFrameWhileBlocked { get; set; } = 250;
  }
}