namespace Anatolia_Fov;

/// <summary>
/// Configuration for SLAYER_UnrestrictedFOV plugin
/// </summary>
public sealed class PluginConfig
{
	/// <summary>
	/// Whether the plugin is enabled
	/// </summary>
	public bool PluginEnabled { get; set; } = true;

	/// <summary>
	/// Minimum allowed FOV value
	/// </summary>
	public int FOVMin { get; set; } = 80;

	/// <summary>
	/// Maximum allowed FOV value
	/// </summary>
	public int FOVMax { get; set; } = 130;
}
