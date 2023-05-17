using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace NoBreakingDoors.Common.Configs;

public sealed class DoorOptionsConfig : ModConfig
{
	public override ConfigScope Mode => ConfigScope.ServerSide;

	[DefaultValue(true)]
	[Label("$Mods.NoBreakingDoors.Config.StopOpeningDoors.Label")]
	[Tooltip("$Mods.NoBreakingDoors.Config.StopOpeningDoors.Tooltip")]
	public bool StopOpeningDoors { get; set; }
}