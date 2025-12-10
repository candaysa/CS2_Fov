using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Commands;
using SwiftlyS2.Shared.Events;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.Plugins;
using SwiftlyS2.Shared.SchemaDefinitions;

namespace Anatolia_Fov;

[PluginMetadata(
	Id = "CS2_FOV",
	Version = "1.0.2",
	Name = "CS2 - FOV",
	Author = "CanDaysa",
	Description = "Oyuncuların kendi FOV ayarlarını seçmelerine olanak tanır"
)]
public sealed class Plugin(ISwiftlyCore core) : BasePlugin(core)
{
	public static new ISwiftlyCore Core { get; private set; } = null!;

	private PluginConfig _config = null!;
	private readonly Dictionary<int, int> _playerFOVSettings = new();
	
	private string _dataFilePath = null!;
	private Dictionary<string, int> _persistentFOVData = new(); // SteamID -> FOV

	public Plugin() : this(null!)
	{
	}

	public override void Load(bool hotReload)
	{
		Core = base.Core;
		LoadConfiguration();
		InitializeDataFile();
		LoadPeristentData();
		RegisterClientPutInServerHook();
		RegisterPlayerDisconnectCleanup();
	}

	public override void Unload()
	{
		_playerFOVSettings.Clear();
		SavePersistentData();
	}

	private void InitializeDataFile()
	{
		// Game klasöründe plugin veri dosyasını depola
		// /game/csgo/addons/swiftlys2/data/CS2_FOV/
		string gameRoot = Directory.GetCurrentDirectory();
		string dataDir = Path.Combine(
			gameRoot,
			"addons",
			"swiftlys2",
			"data",
			"CS2_FOV"
		);
		Directory.CreateDirectory(dataDir);
		_dataFilePath = Path.Combine(dataDir, "player_fov.json");
	}

	private void LoadPeristentData()
	{
		try
		{
			if (File.Exists(_dataFilePath))
			{
				string json = File.ReadAllText(_dataFilePath);
				_persistentFOVData = JsonSerializer.Deserialize<Dictionary<string, int>>(json) ?? new();
			}
		}
		catch
		{
			_persistentFOVData = new();
		}
	}

	private void SavePersistentData()
	{
		try
		{
			var options = new JsonSerializerOptions { WriteIndented = true };
			string json = JsonSerializer.Serialize(_persistentFOVData, options);
			File.WriteAllText(_dataFilePath, json);
		}
		catch
		{
			// Sessizce başarısız ol
		}
	}

	private void LoadConfiguration()
	{
		const string ConfigFileName = "config.jsonc";
		const string ConfigSection = "Anatolia_Fov";

		Core.Configuration
			.InitializeJsonWithModel<PluginConfig>(ConfigFileName, ConfigSection)
			.Configure(cfg => cfg.AddJsonFile(Core.Configuration.GetConfigPath(ConfigFileName), optional: false, reloadOnChange: false));

		_config = Core.Configuration.Manager.GetSection(ConfigSection).Get<PluginConfig>() ?? new();
	}

	private void RegisterPlayerDisconnectCleanup()
	{
		// Oyuncu çıktığında FOV ayarlarını temizle
		Core.Event.OnClientDisconnected += (disconnectEvent) =>
		{
			int playerId = disconnectEvent.PlayerId;
			_playerFOVSettings.Remove(playerId);
		};
	}

	private void RegisterClientPutInServerHook()
	{
		// Oyuncu spawn olduğunda (client put in server) FOV ayarını uygula
		Core.Event.OnClientPutInServer += (clientPutInServerEvent) =>
		{
			try
			{
				var player = Core.PlayerManager.GetPlayer(clientPutInServerEvent.PlayerId);
				if (player == null || !player.IsValid)
					return;

				// SteamID'den kaydedilmiş FOV'u ara
				string steamIdStr = player.SteamID.ToString();
				if (_persistentFOVData.TryGetValue(steamIdStr, out int savedFOV))
				{
					// Kaydedilmiş FOV'u uygula
					ApplyFOVToPlayer(player, savedFOV);
				}
				else
				{
					// İlk kez bağlanıyorsa default FOV ayarla
					SetPlayerFOV(player, 90);
				}
			}
			catch
			{
				// Hata sessizce yoksayıl
			}
		};
	}

	[Command("fov")]
	public void CmdFov(ICommandContext context)
	{
		var player = context.Sender;

		if (player == null || player.IsFakeClient)
		{
			context.Reply("[FOV] This command can only be used by players in-game");
			return;
		}

		if (!_config.PluginEnabled)
		{
			player.SendChat("[FOV] Plugin is Disabled!");
			return;
		}

		var args = context.Args;

		if (args.Length == 0)
		{
			// Reset to default FOV
			SetPlayerFOV(player, 90);
			player.SendChat("[FOV] Your FOV has been reset to 90");
			return;
		}

		string fovStr = args[0];

		if (!int.TryParse(fovStr, out int fovValue))
		{
			player.SendChat("[FOV] Invalid value. Please use !fov <value> (e.g., !fov 100)");
			return;
		}

		if (fovValue < _config.FOVMin)
		{
			player.SendChat($"[FOV] The minimum FOV you can set is: {_config.FOVMin}");
			return;
		}

		if (fovValue > _config.FOVMax)
		{
			player.SendChat($"[FOV] The maximum FOV you can set is: {_config.FOVMax}");
			return;
		}

		SetPlayerFOV(player, fovValue);
		player.SendChat($"[FOV] Your FOV has been set to {fovValue}");
	}

	private void SetPlayerFOV(IPlayer player, int fov)
	{
		int playerId = player.Slot;
		int clamped = Math.Clamp(fov, _config.FOVMin, _config.FOVMax);

		// Geçici sözlüğe kaydet (session süresi)
		_playerFOVSettings[playerId] = clamped;
		
		// Kalıcı veri dosyasına kaydet (SteamID ile)
		string steamIdStr = player.SteamID.ToString();
		_persistentFOVData[steamIdStr] = clamped;
		SavePersistentData();

		// Hemen uygula
		ApplyFOVToPlayer(player, clamped);
	}

	private void ApplyFOVToPlayer(IPlayer player, int fov)
	{
		try
		{
			// Oyuncu kontrolörü ve pawn'ı al
			var controller = player.Controller;
			var pawn = player.PlayerPawn;

			if (controller == null || pawn == null)
				return;

			uint fovUint = (uint)fov;

			// FOV'u kontrolörde ayarla (ana mekanizma)
			controller.DesiredFOV = fovUint;
			
			// Engine'e property değiştiğini bildir
			controller.DesiredFOVUpdated();
		}
		catch
		{
			// Hata sessizce yoksayıl
		}
	}
}
