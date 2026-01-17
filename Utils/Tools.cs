using Terraria;

namespace AutoFish.Utils;

public class Tools
{
    /// <summary>
    ///     检查指定玩家是否有任意活跃的浮漂。
    /// </summary>
    public static bool BobbersActive(int whoAmI)
    {
        return Main.projectile.Any(p => p.active && p.owner == whoAmI && p.bobber);
    }

    /// <summary>
    ///     根据在线玩家数量动态调整钓鱼次数上限。
    /// </summary>
    public static int GetLimit(int plrs)
    {
        return plrs <= 5 ? 100 : plrs <= 10 ? 50 : plrs <= 20 ? 25 : 10;
    }
}