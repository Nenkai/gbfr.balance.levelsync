using gbfr.balance.levelsync.Configuration;
using gbfr.balance.levelsync.Template;

using gbfrelink.utility.manager.Interfaces;

using NenTools.Reloaded.ScanManager.Interfaces;

using Reloaded.Hooks.ReloadedII.Interfaces;
using Reloaded.Memory.Interfaces;
using Reloaded.Memory.SigScan.ReloadedII.Interfaces;
using Reloaded.Mod.Interfaces;

using System.Buffers.Binary;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace gbfr.balance.levelsync;

/// <summary>
/// Your mod logic goes here.
/// </summary>
public class Mod : ModBase // <= Do not Remove.
{
    /// <summary>
    /// Provides access to the mod loader API.
    /// </summary>
    private readonly IModLoader _modLoader;

    /// <summary>
    /// Provides access to the Reloaded.Hooks API.
    /// </summary>
    /// <remarks>This is null if you remove dependency on Reloaded.SharedLib.Hooks in your mod.</remarks>
    private readonly IReloadedHooks? _hooks;

    /// <summary>
    /// Provides access to the Reloaded logger.
    /// </summary>
    private readonly ILogger _logger;

    /// <summary>
    /// Entry point into the mod, instance that created this class.
    /// </summary>
    private readonly IMod _owner;

    /// <summary>
    /// Provides access to this mod's configuration.
    /// </summary>
    private Config _configuration;

    /// <summary>
    /// The configuration of the currently executing mod.
    /// </summary>
    private readonly IModConfig _modConfig;

    private readonly IStartupScanner? _startupScanner;

    private readonly IUserDefinedParams _userDefinedParams;

    private nint _quickQuestCheckAddr;

    private List<byte> _checkOriginalBytes = [];
    private bool _saveOriginalBytes = true;

    public Mod(ModContext context)
    {
        _modLoader = context.ModLoader;
        _hooks = context.Hooks;
        _logger = context.Logger;
        _owner = context.Owner;
        _configuration = context.Configuration;
        _modConfig = context.ModConfig;

#if DEBUG
        // Attaches debugger in debug mode; ignored in release.
        Debugger.Launch();
#endif
        var startupScannerController = _modLoader.GetController<IStartupScanner>();
        if (startupScannerController == null || !startupScannerController.TryGetTarget(out _startupScanner))
        {
            _logger.WriteLine($"[{_modConfig.ModId}] IStartupScanner not found?  Rich presence will not load!", System.Drawing.Color.Red);
            return;
        }

        var userDefinedParamsRef = _modLoader.GetController<IUserDefinedParams>();
        if (startupScannerController == null || !userDefinedParamsRef.TryGetTarget(out _userDefinedParams))
        {
            _logger.WriteLine($"[{_modConfig.ModId}] IUserDefinedParams not found?  Rich presence will not load!", System.Drawing.Color.Red);
            return;
        }

        var scanManagerRef = _modLoader.GetController<IScanManager>();
        if (startupScannerController == null || !scanManagerRef.TryGetTarget(out IScanManager? scanManager))
        {
            _logger.WriteLine($"[{_modConfig.ModId}] IScanManager not found?  Rich presence will not load!", System.Drawing.Color.Red);
            return;
        }
        scanManager.InitializeScans(Path.Combine(_modLoader.GetDirectoryForModId(_modConfig.ModId), "Signatures"), _modConfig.ModId);

        if (!_configuration.EnableLevelSync)
        {
            _logger.Write($"[{_modConfig.ModId}] Power adjustment is currently disabled in settings.\n", _logger.ColorYellow);
        }

        string signatureGroup = _userDefinedParams.IsEndlessRagnarok() ? "granblue_fantasy_relink_er" : "granblue_fantasy_relink";
        scanManager.AddScan("QuickQuestCheckAddr", signatureGroup, address =>
        {
            _quickQuestCheckAddr = address;
            Apply(_configuration.EnableLevelSync);
        });
    }

    public unsafe void Apply(bool state)
    {
        nint addr = _quickQuestCheckAddr;
        // Remove quick quest check
        if (state)
        {
            if (_userDefinedParams.IsEndlessRagnarok())
            {
                int fieldOffset = *(int*)(addr + 3);
                Span<byte> bytes = stackalloc byte[7];
                bytes[0] = 0xC6;
                bytes[1] = 0x83;
                BinaryPrimitives.WriteInt32LittleEndian(bytes[2..], fieldOffset);
                bytes[6] = 1;
                WriteBytes(ref addr, bytes);       // 0F 94 83 ?? ?? ?? ?? 48 8B 35 - "setz byte ptr [rbx+63A84h]" -> mov byte ptr [rbx+63A84h], 1 - r9 is quest system/manager, 0x93F84 is "IsQuickQuest"
            }
            else
            {
                // Can't use that in ER. Crashes.
                WriteBytes(ref addr, [0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90]); // 41 80 B9 84 3F 09 00 00 - "cmp byte ptr [r9+93F84h], 0" -> nop - r9 is quest system/manager, 0x93F84 is "IsQuickQuest"
                WriteBytes(ref addr, [0x90, 0x90, 0x90, 0x90, 0x90, 0x90]);             // 0F 84 4F DC FF FF       - "jz      loc_7FF68DB319A2"    -> nop
            }

            _saveOriginalBytes = false;
            _logger.Write($"[{_modConfig.ModId}] Power adjustment is now applied to all quests.\n", _logger.ColorGreen);
        }
        else
        {
            if (_checkOriginalBytes.Count == 0)
                return;

            Reloaded.Memory.Memory.Instance.SafeWrite((nuint)addr, CollectionsMarshal.AsSpan(_checkOriginalBytes));
            _logger.Write($"[{_modConfig.ModId}] Power adjustment is now only available in Quick Quest (default).\n", _logger.ColorGreen);
        }
    }

    private void WriteBytes(ref nint currentAddress, Span<byte> bytes)
    {
        if (_saveOriginalBytes)
        {
            byte[] orig = new byte[bytes.Length];
            Reloaded.Memory.Memory.Instance.SafeRead((nuint)currentAddress, orig);
            _checkOriginalBytes.AddRange(orig);
        }

        Reloaded.Memory.Memory.Instance.SafeWrite((nuint)currentAddress, bytes);
        currentAddress += bytes.Length;
    }

    #region Standard Overrides
    public override void ConfigurationUpdated(Config configuration)
    {
        _configuration = configuration;
        _logger.WriteLine($"[{_modConfig.ModId}] Config Updated: Applying");

        Apply(configuration.EnableLevelSync);
    }
    #endregion

    #region For Exports, Serialization etc.
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public Mod() { }
#pragma warning restore CS8618
    #endregion
}