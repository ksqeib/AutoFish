using System;

namespace AutoFish.Data;

public class PlayerData
{
    //玩家数据表（使用字典以便按玩家名快速检索）
    private Dictionary<string, ItemData> Items { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// 获取或创建玩家数据入口，内部使用私有工厂方法。
    /// </summary>
    public ItemData GetOrCreatePlayerData(string name, Func<string, ItemData> factory)
    {
        return GetOrCreate(name, factory);
    }

    /// <summary>
    /// 私有的获取或创建逻辑，隐藏底层字典实现。
    /// </summary>
    private ItemData GetOrCreate(string name, Func<string, ItemData> factory)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Player name is required.", nameof(name));
        if (factory == null) throw new ArgumentNullException(nameof(factory));

        if (!Items.TryGetValue(name, out var data) || data == null)
        {
            data = factory(name);
            Items[name] = data;
        }

        return data;
    }

    /// <summary>
    /// 玩家自动钓鱼相关配置。
    /// </summary>
    public class ItemData
    {
        public ItemData(string name = "", bool enabled = true, bool mod = true, bool buff = true, int hookMax = 3,
            bool moreHook = true)
        {
            Name = name ?? "";
            Enabled = enabled;
            Mod = mod;
            Buff = buff;
            HookMax = hookMax;
            MoreHook = moreHook;
        }

        //玩家名字
        public string Name { get; set; }

        //玩家开关
        public bool Enabled { get; set; }

        //消耗模式开关
        public bool Mod { get; set; }

        //BUFF开关
        public bool Buff { get; set; }

        //鱼线数量
        public int HookMax { get; set; } = 3;


        //鱼线数量
        public bool MoreHook { get; set; } = true;

        //记录时间
        public DateTime LogTime { get; set; }
    }
}