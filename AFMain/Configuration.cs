using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using TShockAPI;

namespace AutoFish.AFMain;

/// <summary>
///     插件配置模型，负责序列化与默认值初始化。
/// </summary>
internal class Configuration
{
    /// <summary>配置文件路径。</summary>
    public static readonly string FilePath = Path.Combine(TShock.SavePath, "AutoFish.json");

    [JsonProperty("额外渔获", Order = -1)] public List<int> ExtraCatchItemIds = new();

    [JsonProperty("插件开关", Order = -13)] public bool PluginEnabled { get; set; } = true;

    [JsonProperty("自动钓鱼开关", Order = -12)] public bool GlobalAutoFishFeatureEnabled { get; set; }
    [JsonProperty("多钩钓鱼", Order = -11)] public bool GlobalMultiHookFeatureEnabled { get; set; }
    [JsonProperty("Buff开关", Order = -8)] public bool GlobalBuffFeatureEnabled { get; set; }
    [JsonProperty("多钩上限", Order = -9)] public int GlobalMultiHookMaxNum { get; set; } = 5;

    [JsonProperty("随机物品", Order = -10)] public bool RandomLootEnabled { get; set; }


    [JsonProperty("Buff表", Order = -5)] public Dictionary<int, int> BuffDurations { get; set; } = new();

    [JsonProperty("消耗模式", Order = -4)] public bool GlobalConsumptionModeEnabled { get; set; }

    [JsonProperty("消耗数量", Order = -3)] public int BaitConsumeCount { get; set; } = 10;

    [JsonProperty("奖励时长", Order = -2)] public int RewardDurationMinutes { get; set; } = 12;

    [JsonProperty("消耗物品", Order = -1)] public List<int> BaitItemIds { get; set; } = new();

    [JsonProperty("禁止衍生弹幕", Order = 10)]
    public int[] DisabledProjectileIds { get; set; } =
        new[] { 623, 625, 626, 627, 628, 831, 832, 833, 834, 835, 963, 970 };

    /// <summary>
    ///     初始化默认的 Buff、鱼饵和额外渔获设置。
    /// </summary>
    public void Ints()
    {
        BuffDurations = new Dictionary<int, int>
        {
            // { 80,10 },
            // { 122,240 }
        };

        BaitItemIds = new List<int>
        {
            2002, 2675, 2676, 3191, 3194
        };

        ExtraCatchItemIds = new List<int>
        {
            // 29,3093,4345
        };
    }

    /// <summary>
    ///     将当前配置写入磁盘。
    /// </summary>
    public void Write()
    {
        var json = JsonConvert.SerializeObject(this, Formatting.Indented);
        File.WriteAllText(FilePath, json);
    }

    /// <summary>
    ///     读取配置文件，若不存在则创建默认配置。
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