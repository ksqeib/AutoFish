using System;
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
    ///     触发自动钓鱼，处理浮漂 AI 更新与收杆逻辑。原理：每次AI更新后尝试为玩家把鱼钓起来，并生成一个新的同样的弹射物
    /// </summary>
    private void ProjectAiUpdate(ProjectileAiUpdateEventArgs args)
    {
        if (args.Projectile.owner < 0) return;
        if (args.Projectile.owner > Main.maxPlayers) return;
        if (!args.Projectile.active) return;
        if (!args.Projectile.bobber) return;
        if (!Config.PluginEnabled) return;
        if (!Config.GlobalAutoFishFeatureEnabled) return;

        var player = TShock.Players[args.Projectile.owner];
        if (player == null) return;
        if (!player.Active) return;

        // 从数据表中获取与玩家名字匹配的配置项
        var playerData = PlayerData.GetOrCreatePlayerData(player.Name, CreateDefaultPlayerData);
        if (!playerData.AutoFishEnabled) return;

        // 正常状态下与消耗模式下启用自动钓鱼
        if (Config.GlobalConsumptionModeEnabled && !playerData.ConsumptionEnabled) return;

        //检测是不是生成，是生成boss就不钓起来
        if (!(args.Projectile.ai[1] < 0)) return;

        args.Projectile.ai[0] = 1.0f;

        var fishingConditions = player.TPlayer.GetFishingConditions();
        //松露虫 判断一下玩家是否在海边
        if (fishingConditions.BaitItemType == 2673 && player.X / 16 == Main.oceanBG && player.Y / 16 == Main.oceanBG)
        {
            args.Projectile.ai[1] = 0;
            player.SendData(PacketTypes.ProjectileNew, "", args.Projectile.whoAmI);
            return;
        }

        //修改钓鱼得到的东西
        //获得钓鱼物品方法
        var hasCatch = false;
        var activePlayerCount = TShock.Players.Count(p => p != null && p.Active && p.IsLoggedIn);
        var dropLimit = Tools.GetLimit(activePlayerCount); //根据人数动态调整Limit
        for (var count = 0; !hasCatch && count < dropLimit; count++)
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

            if (Config.RandomLootEnabled) args.Projectile.localAI[1] = Random.Shared.Next(1, ItemID.Count);

            //ai[1] = localAI[1]
            args.Projectile.ai[1] = args.Projectile.localAI[1];

            // 如果额外渔获有任何1个物品ID，则参与AI[1]
            if (Config.ExtraCatchItemIds.Any())
                if (args.Projectile.ai[1] <= 0) //额外渔获这里。。负数应该是boss
                    args.Projectile.ai[1] = Config.ExtraCatchItemIds[Main.rand.Next(Config.ExtraCatchItemIds.Count)];

            hasCatch = args.Projectile.ai[1] > 0;
        }

        if (!hasCatch) return; //小于0不加新的
        // 原版给东西的代码，在kill函数，会把ai[1]给玩家
        // if (Main.myPlayer == this.owner && this.bobber)
        // {
        //     PopupText.ClearSonarText();
        //     if ((double) this.ai[1] > 0.0 && (double) this.ai[1] < (double) ItemID.Count)
        //         this.AI_061_FishingBobber_GiveItemToPlayer(Main.player[this.owner], (int) this.ai[1]);
        //     this.ai[1] = 0.0f;
        // }
        // 这里发的是连续弹幕 避免线断 因为弹幕是不需要玩家物理点击来触发收杆的，但是服务端和客户端概率测算不一样，会导致服务器扣了饵料，但是客户端没扣
        player.SendData(PacketTypes.ProjectileNew, "", args.Projectile.whoAmI);

        // 让服务器扣饵料
        var locate = LocateBait(player, fishingConditions.BaitItemType);
        player.TPlayer.ItemCheck_CheckFishingBobber_PickAndConsumeBait(args.Projectile, out var pull,
            out var baitUsed);
        if (pull)
        {
            //原版收杆函数，这里会使得  bobber.ai[1] = bobber.localAI[1];
            player.TPlayer.ItemCheck_CheckFishingBobber_PullBobber(args.Projectile, baitUsed);
            player.SendData(PacketTypes.PlayerSlot, "", player.Index, locate);
        }

        var index = SpawnProjectile.NewProjectile(
            Main.projectile[args.Projectile.whoAmI].GetProjectileSource_FromThis(),
            args.Projectile.position, args.Projectile.velocity, args.Projectile.type, 0, 0,
            args.Projectile.owner);
        player.SendData(PacketTypes.ProjectileNew, "", index);
    }

    private static int LocateBait(TSPlayer player, int baitUsed)
    {
        // 更新玩家背包 使用饵料信息
        for (var i = 0; i < player.TPlayer.inventory.Length; i++)
        {
            var inventorySlot = player.TPlayer.inventory[i];
            // 玩家饵料（指的是你手上鱼竿上的那个数字），使用的饵料是背包里的物品
            if (inventorySlot.bait <= 0 || baitUsed != inventorySlot.type) continue;
            return i;
        }

        return 0;
    }
}