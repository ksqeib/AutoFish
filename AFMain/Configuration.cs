using Newtonsoft.Json;
using TShockAPI;

namespace AutoFish.AFMain;

/// <summary>
/// 插件配置模型，负责序列化与默认值初始化。
/// </summary>
internal class Configuration
{
    /// <summary>
    /// 初始化默认的 Buff、鱼饵和额外渔获设置。
    /// </summary>
    public void Ints()
    {
        BuffID = new Dictionary<int, int>
        {
            // { 80,10 },
            // { 122,240 }
        };

        BaitType = new List<int>
        {
            2002, 2675, 2676, 3191, 3194
        };

        DoorItems = new List<int>
        {
            // 29,3093,4345
        };
    }

    [JsonProperty("插件开关", Order = -13)] public bool Enabled { get; set; } = true;

    [JsonProperty("多钩钓鱼", Order = -12)] public bool MoreHook { get; set; } = true;

    [JsonProperty("随机物品", Order = -11)] public bool Random { get; set; }

    [JsonProperty("多钩上限", Order = -10)] public int HookMax { get; set; } = 5;

    [JsonProperty("Buff开关", Order = -9)] public bool BuffEnabled { get; set; } = false;

    [JsonProperty("Buff表", Order = -6)] public Dictionary<int, int> BuffID { get; set; } = new();

    [JsonProperty("消耗模式", Order = -5)] public bool ConMod { get; set; }

    [JsonProperty("消耗数量", Order = -4)] public int BaitStack { get; set; } = 10;

    [JsonProperty("奖励时长", Order = -3)] public int timer { get; set; } = 12;

    [JsonProperty("消耗物品", Order = -2)] public List<int> BaitType { get; set; } = new();

    [JsonProperty("额外渔获", Order = -1)] public List<int> DoorItems = new();

    [JsonProperty("禁止衍生弹幕", Order = 10)]
    public int[] DisableProjectile { get; set; } =
        new[] { 623, 625, 626, 627, 628, 831, 832, 833, 834, 835, 963, 970 };

    /// <summary>配置文件路径。</summary>
    public static readonly string FilePath = Path.Combine(TShock.SavePath, "AutoFish.json");

    /// <summary>
    /// 将当前配置写入磁盘。
    /// </summary>
    public void Write()
    {
        var json = JsonConvert.SerializeObject(this, Formatting.Indented);
        File.WriteAllText(FilePath, json);
    }

    /// <summary>
    /// 读取配置文件，若不存在则创建默认配置。
    /// </summary>
    public static Configuration Read()
    {
        if (!File.Exists(FilePath))
        {
            var NewConfig = new Configuration();
            NewConfig.Ints();
            new Configuration().Write();
            return NewConfig;
        }

        var jsonContent = File.ReadAllText(FilePath);
        return JsonConvert.DeserializeObject<Configuration>(jsonContent)!;
    }
}