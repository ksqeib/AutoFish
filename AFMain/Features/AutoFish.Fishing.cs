using System.Linq;
using AutoFish.Utils;
using Terraria;
using Terraria.ID;
using TerrariaApi.Server;
using TShockAPI;

namespace AutoFish.AFMain;

public partial class AutoFish
{
    /// <summary>
    /// 触发自动钓鱼，处理浮漂 AI 更新与收杆逻辑。原理：每次AI更新后尝试为玩家把鱼钓起来，并生成一个新的同样的弹射物
    /// </summary>
    private void ProjectAiUpdate(ProjectileAiUpdateEventArgs args)
    {
        if (args.Projectile.owner is < 0 or > Main.maxPlayers ||
            !args.Projectile.active ||
            !args.Projectile.bobber ||
            !Config.Enabled)
            return;

        var plr = TShock.Players[args.Projectile.owner];
        if (plr == null || !plr.Active)
            return;

        // 从数据表中获取与玩家名字匹配的配置项
        var list = PlayerData.GetOrCreatePlayerData(plr.Name, CreateDefaultPlayerData);
        if (!list.Enabled) return;

        // 正常状态下与消耗模式下启用自动钓鱼
        if (Config.ConMod && (!Config.ConMod || !list.Mod)) return;

        //检测是不是生成，是生成boss就不钓起来
        if (!(args.Projectile.ai[1] < 0)) return;

        args.Projectile.ai[0] = 1.0f;

        var baitItem = new Item();

        // 检查并选择消耗饵料
        // 模拟玩家收杆
        plr.TPlayer.ItemCheck_CheckFishingBobber_PickAndConsumeBait(args.Projectile, out var pull,
            out var BaitUsed);
        if (pull)
        {
            //原版收杆函数
            plr.TPlayer.ItemCheck_CheckFishingBobber_PullBobber(args.Projectile, BaitUsed);
            //这里会使得  bobber.ai[1] = bobber.localAI[1];

            // 更新玩家背包 使用饵料信息
            for (var i = 0; i < plr.TPlayer.inventory.Length; i++)
            {
                var inv = plr.TPlayer.inventory[i];

                //玩家饵料（指的是你手上鱼竿上的那个数字），使用的饵料是背包里的物品
                if (inv.bait <= 0 || BaitUsed != inv.type) continue;
                //当物品数量正常则开始进入钓鱼检查
                if (inv.stack > 1)
                {
                    //发包到对应饵料的格子内
                    plr.SendData(PacketTypes.PlayerSlot, "", plr.Index, i);
                    break;
                }

                //当前物品数量为1则移除（避免选中的饵不会主动消失 变成无限饵 或 卡住线程）
                if (inv.stack > 1 && inv.bait > 1) continue;
                
                inv.TurnToAir();
                plr.SendData(PacketTypes.PlayerSlot, "", plr.Index, i);
                break;
            }
        }

        //松露虫 判断一下玩家是否在海边
        if (baitItem.type == 2673 && plr.X / 16 == Main.oceanBG && plr.Y / 16 == Main.oceanBG)
        {
            args.Projectile.ai[1] = 0;
            plr.SendData(PacketTypes.ProjectileNew, "", args.Projectile.whoAmI);
            return;
        }

        //修改钓鱼得到的东西
        //获得钓鱼物品方法
        var flag = false;
        var ActiveCount = TShock.Players.Where(plr => plr != null && plr.Active && plr.IsLoggedIn).Count();
        var Limit = Tools.GetLimit(ActiveCount); //根据人数动态调整Limit
        for (var count = 0; !flag && count < Limit; count++)
        {
            //61就是直接调用AI_061_FishingBobber
            //原版方法，获取物品啥的
            args.Projectile.FishingCheck();
            
            // FishingCheck_RollDropLevels - 会得出玩家得到的物品稀有度
            // FishingCheck_ProbeForQuestFish - 任务🐟概率
            // FishingCheck_RollEnemySpawns - 生成敌怪 -> fisher.rolledEnemySpawn -> -localAI[1]
            // FishingCheck_RollItemDrop roll出敌怪就不会得到 -> fisher.rolledItemDrop -> localAI[1]
            // fishingLevel 鱼力
            // localAI[1]- 钓上来的东西
            // AI[1]- 鱼力

            if (Config.Random) args.Projectile.localAI[1] = Random.Shared.Next(1, ItemID.Count);

            //ai[1] = localAI[1]
            args.Projectile.ai[1] = args.Projectile.localAI[1];

            // 如果额外渔获有任何1个物品ID，则参与AI[1]
            if (Config.DoorItems.Any())
                if (args.Projectile.ai[1] <= 0) //额外渔获这里。。负数应该是boss
                    args.Projectile.ai[1] = Config.DoorItems[Main.rand.Next(Config.DoorItems.Count)];

            flag = args.Projectile.ai[1] > 0;
        }

        if (!flag) return;//小于0不加新的
        // 原版给东西的代码，在kill函数，会把ai[1]给玩家
        // if (Main.myPlayer == this.owner && this.bobber)
        // {
        //     PopupText.ClearSonarText();
        //     if ((double) this.ai[1] > 0.0 && (double) this.ai[1] < (double) ItemID.Count)
        //         this.AI_061_FishingBobber_GiveItemToPlayer(Main.player[this.owner], (int) this.ai[1]);
        //     this.ai[1] = 0.0f;
        // }
        // 这里发的是连续弹幕 避免线断 因为弹幕是不需要玩家物理点击来触发收杆的
        plr.SendData(PacketTypes.ProjectileNew, "", args.Projectile.whoAmI);
        var index = SpawnProjectile.NewProjectile(
            Main.projectile[args.Projectile.whoAmI].GetProjectileSource_FromThis(),
            args.Projectile.position, args.Projectile.velocity, args.Projectile.type, 0, 0,
            args.Projectile.owner);
        plr.SendData(PacketTypes.ProjectileNew, "", index);
    }
}