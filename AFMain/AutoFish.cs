using System;
using System.Linq;
using AutoFish.Data;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.Hooks;

namespace AutoFish.AFMain;

[ApiVersion(2, 1)]
public partial class AutoFish : TerrariaPlugin
{
    /// <summary>全局配置实例。</summary>
    internal static Configuration Config = new();

    /// <summary>玩家数据集合。</summary>
    internal static AFPlayerData PlayerData = new();

    /// <summary>
    ///     创建插件实例。
    /// </summary>
    public AutoFish(Main game) : base(game)
    {
    }

    /// <summary>插件名称。</summary>
    public override string Name => "自动钓鱼";

    /// <summary>插件作者。</summary>
    public override string Author => "羽学 少司命 ksqeib";

    /// <summary>插件版本。</summary>
    public override Version Version => new(1, 3, 3);

    /// <summary>插件描述。</summary>
    public override string Description => "青山常伴绿水，燕雀已是南飞";

    /// <summary>
    ///     默认玩家数据工厂，基于当前配置初始化。
    /// </summary>
    internal static AFPlayerData.ItemData CreateDefaultPlayerData(string playerName)
    {
        // Attempt to resolve current player to seed defaults from permissions
        var player = TShock.Players.FirstOrDefault(p => p != null && p.Active &&
                                p.Name.Equals(playerName, StringComparison.OrdinalIgnoreCase));

        var canBuff = player != null && (player.HasPermission("autofish.buff") || player.HasPermission("autofish.admin"));
        var canMulti = player != null && (player.HasPermission("autofish.multihook") || player.HasPermission("autofish.admin"));

        return new AFPlayerData.ItemData
        {
            Name = playerName,
            AutoFishEnabled = true,
            BuffEnabled = canBuff,
            ConsumptionEnabled = false,
            HookMaxNum = Config.HookMax,
            MultiHookEnabled = canMulti
        };
    }

    /// <summary>
    ///     插件初始化，注册事件和命令。
    /// </summary>
    public override void Initialize()
    {
        LoadConfig();
        GeneralHooks.ReloadEvent += ReloadConfig;
        GetDataHandlers.NewProjectile += ProjectNew!;
        GetDataHandlers.NewProjectile += BuffUpdate!;
        GetDataHandlers.PlayerUpdate.Register(OnPlayerUpdate);
        ServerApi.Hooks.ProjectileAIUpdate.Register(this, ProjectAiUpdate);
        TShockAPI.Commands.ChatCommands.Add(new Command("autofish", Commands.Afs, "af", "autofish"));
    }

    /// <summary>
    ///     释放插件，注销事件与命令。
    /// </summary>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            GeneralHooks.ReloadEvent -= ReloadConfig;
            GetDataHandlers.NewProjectile -= ProjectNew!;
            GetDataHandlers.NewProjectile -= BuffUpdate!;
            GetDataHandlers.PlayerUpdate.UnRegister(OnPlayerUpdate);
            ServerApi.Hooks.ProjectileAIUpdate.Deregister(this, ProjectAiUpdate);
            TShockAPI.Commands.ChatCommands.RemoveAll(x => x.CommandDelegate == Commands.Afs);
        }

        base.Dispose(disposing);
    }

    /// <summary>
    ///     处理 /reload 触发的配置重载。
    /// </summary>
    private static void ReloadConfig(ReloadEventArgs args)
    {
        LoadConfig();
        args.Player.SendInfoMessage("[自动钓鱼]重新加载配置完毕。");
    }

    /// <summary>
    ///     读取并落盘配置。
    /// </summary>
    private static void LoadConfig()
    {
        Config = Configuration.Read();
        Config.Write();
    }
}