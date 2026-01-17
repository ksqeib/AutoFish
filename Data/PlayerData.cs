namespace AutoFish.Data;

public class PlayerData
{
    //玩家数据表（使用字典以便按玩家名快速检索）
    public Dictionary<string, ItemData> Items { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    #region 数据结构

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

    #endregion
}