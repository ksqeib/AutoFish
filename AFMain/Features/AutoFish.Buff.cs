using TerrariaApi.Server;
using TShockAPI;

namespace AutoFish.AFMain;

public partial class AutoFish
{
    /// <summary>
    /// 为满足条件的玩家在钓鱼时施加 Buff。
    /// </summary>
    public void BuffUpdate(object sender, GetDataHandlers.NewProjectileEventArgs e)
    {
        var plr = e.Player;

        if (plr == null || !plr.Active || !plr.IsLoggedIn || !Config.Enabled || !plr.HasPermission("autofish")) return;

        // 从数据表中获取与玩家名字匹配的配置项
        var list = PlayerData.GetOrCreatePlayerData(plr.Name, CreateDefaultPlayerData);
        if (!list.Buff) return;

        //出现鱼钩摆动就给玩家施加buff
        if (list.Enabled)
            if (Utils.Tools.BobbersActive(e.Owner))
                foreach (var buff in Config.BuffID)
                    plr.SetBuff(buff.Key, buff.Value);
    }
}
