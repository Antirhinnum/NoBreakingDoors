using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace NoBreakingDoors.Common.Configs
{
	public class DoorOptionsConfig : ModConfig
	{
		public override ConfigScope Mode => ConfigScope.ServerSide;

		[DefaultValue(true)]
		[Tooltip("Stop all enemies from opening doors.")]
		public bool StopOpeningDoors { get; set; }
	}
}