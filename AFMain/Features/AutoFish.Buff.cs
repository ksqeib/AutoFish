using AutoFish.Utils;
using TShockAPI;

namespace AutoFish.AFMain;

public partial class AutoFish
{
    /// <summary>
    ///     为满足条件的玩家在钓鱼时施加 Buff。
    /// </summary>
    public void BuffUpdate(object sender, GetDataHandlers.NewProjectileEventArgs e)
    {
        var player = e.Player;

        if (player == null) return;
        if (!player.Active) return;
        if (!player.IsLoggedIn) return;
        if (!Config.Enabled) return;
        if (!Config.BuffEnabled) return;

        // 从数据表中获取与玩家名字匹配的配置项
        var playerData = PlayerData.GetOrCreatePlayerData(player.Name, CreateDefaultPlayerData);
        if (!playerData.Buff) return;

        //出现鱼钩摆动就给玩家施加buff
        if (!playerData.Enabled) return;
        if (!Tools.BobbersActive(e.Owner)) return;

        foreach (var buff in Config.BuffID)
            player.SetBuff(buff.Key, buff.Value);
    }
}