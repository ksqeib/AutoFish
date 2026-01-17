using System;
using System.Linq;
using System.Text;
using Terraria.ID;
using TerrariaApi.Server;
using TShockAPI;

namespace AutoFish.AFMain;

public partial class AutoFish
{
    /// <summary>需要关闭钓鱼权限的玩家计数。</summary>
    private static int ClearCount; //需要关闭钓鱼权限的玩家计数

    /// <summary>
    /// 消耗模式下根据玩家物品开启或关闭自动钓鱼。
    /// </summary>
    private void OnPlayerUpdate(object? sender, GetDataHandlers.PlayerUpdateEventArgs e)
    {
        var plr = e.Player;
        if (!Config.Enabled || !Config.ConMod || e == null ||
            plr == null || !plr.IsLoggedIn || !plr.Active)
            return;

        var data = PlayerData.GetOrCreatePlayerData(plr.Name, CreateDefaultPlayerData);
        if (!data.Enabled) return;

        // 播报玩家消耗鱼饵用的
        var mess = new StringBuilder();

        //当玩家的自动钓鱼没开启时
        if (!data.Mod)
        {
            //初始化一个消耗值
            var sun = Config.BaitStack;

            // 统计背包中指定鱼饵的总数量(不包含手上物品)
            var TotalBait = plr.TPlayer.inventory.Sum(inv =>
                Config.BaitType.Contains(inv.type) &&
                inv.type != plr.TPlayer.inventory[plr.TPlayer.selectedItem].type
                    ? inv.stack
                    : 0);

            // 如果背包中有足够的鱼饵数量 和消耗值相等
            if (TotalBait >= sun)
            {
                // 遍历背包58格
                for (var i = 0; i < plr.TPlayer.inventory.Length && sun > 0; i++)
                {
                    var inv = plr.TPlayer.inventory[i];

                    // 是Config里指定的鱼饵,不是手上的物品
                    if (Config.BaitType.Contains(inv.type))
                    {
                        var BaitStack = Math.Min(sun, inv.stack); // 计算需要消耗的鱼饵数量

                        inv.stack -= BaitStack; // 从背包中扣除鱼饵
                        sun -= BaitStack; // 减少消耗值

                        // 记录消耗的鱼饵数量到播报
                        mess.AppendFormat(" [c/F25156:{0}]([c/AECDD1:{1}]) ", TShock.Utils.GetItemById(inv.type).Name,
                            BaitStack);

                        // 如果背包中的鱼饵数量为0，清空该格子
                        if (inv.stack < 1) inv.TurnToAir();

                        // 发包给背包里对应格子的鱼饵
                        plr.SendData(PacketTypes.PlayerSlot, "", plr.Index, PlayerItemSlotID.Inventory0 + i);
                    }
                }

                // 消耗值清空时，开启自动钓鱼开关
                if (sun <= 0)
                {
                    data.Mod = true;
                    data.LogTime = DateTime.Now;
                    plr.SendMessage($"玩家 [c/46C2D4:{plr.Name}] 已开启[c/F5F251:自动钓鱼] 消耗物品为:{mess}", 247, 244, 150);
                }
            }
        }

        else //当 data.Mod 开启时
        {
            //由它判断关闭自动钓鱼
            ExitMod(plr, data);
        }
    }

    /// <summary>
    /// 消耗模式下检测超时并关闭自动钓鱼权限。
    /// </summary>
    private static void ExitMod(TSPlayer plr, Data.AFPlayerData.ItemData data)
    {
        var mess2 = new StringBuilder();
        mess2.AppendLine("[i:3455][c/AD89D5:自][c/D68ACA:动][c/DF909A:钓][c/E5A894:鱼][i:3454]");
        mess2.AppendLine($"以下玩家超过 [c/E17D8C:{Config.timer}] 分钟 已关闭[c/76D5B4:自动钓鱼]权限：");

        // 只显示分钟
        var Minutes = (DateTime.Now - data.LogTime).TotalMinutes;

        // 时间过期 关闭自动钓鱼权限
        if (Minutes >= Config.timer)
        {
            ClearCount++;
            data.Mod = false;
            data.LogTime = default; // 清空记录时间
            mess2.AppendFormat("[c/A7DDF0:{0}]:[c/74F3C9:{1}分钟]", data.Name, Math.Floor(Minutes));
        }

        // 确保有一个玩家计数，只播报一次
        if (ClearCount > 0 && mess2.Length > 0)
        {
            plr.SendMessage(mess2.ToString(), 247, 244, 150);
            ClearCount = 0;
        }
    }
}
