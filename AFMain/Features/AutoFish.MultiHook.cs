using AutoFish.Utils;
using Terraria;
using TShockAPI;

namespace AutoFish.AFMain;

public partial class AutoFish
{
    /// <summary>
    ///     处理多线钓鱼，派生额外的鱼线弹幕。
    /// </summary>
    public void ProjectNew(object sender, GetDataHandlers.NewProjectileEventArgs e)
    {
        var plr = e.Player;
        var guid = Guid.NewGuid().ToString();
        var HookCount = Main.projectile.Count(p => p.active && p.owner == e.Owner && p.bobber); // 浮漂计数

        if (plr == null) return;
        if (!plr.Active) return;
        if (!plr.IsLoggedIn) return;
        if (!Config.Enabled) return;
        if (!Config.MoreHook) return;
        if (HookCount > Config.HookMax - 1) return;

        // 从数据表中获取与玩家名字匹配的配置项
        var list = PlayerData.GetOrCreatePlayerData(plr.Name, CreateDefaultPlayerData);
        if (!list.Enabled) return;

        // 正常状态下与消耗模式下启用多线钓鱼
        if (!Config.ConMod || (Config.ConMod && list.Mod))
            // 检查是否上钩
            if (Tools.BobbersActive(e.Owner))
            {
                var index = SpawnProjectile.NewProjectile(Main.projectile[e.Index].GetProjectileSource_FromThis(),
                    e.Position, e.Velocity, e.Type, e.Damage, e.Knockback, e.Owner, 0, 0, 0, -1, guid);
                plr.SendData(PacketTypes.ProjectileNew, "", index);

                // 更新多线计数
                HookCount++;
            }
    }
}