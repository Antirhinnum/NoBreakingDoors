using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace NoBreakingDoors.Common.GlobalNPCs
{
	public class GoblinPeonModifier : GlobalNPC
	{
		public override bool Autoload(ref string name)
		{
			IL.Terraria.NPC.AI_003_Fighters += PreventPeonsBreakingDoors;
			return base.Autoload(ref name);
		}

		/// <summary>
		/// Changes the following check in NPC.AI_003_Fighters:
		/// if (type == 26) {
		///		WorldGen.KillTile(num178, num179 - 1);
		///		if (Main.netMode == 2)
		///			NetMessage.SendData(17, -1, -1, null, 0, num178, num179 - 1);
		///		}
		///	else {
		///		if (TileLoader.OpenDoorID(Main.tile[num178, num179 - 1]) >= 0) {
		///
		/// to:
		///
		/// if (type == int.MinValue) {
		///		WorldGen.KillTile(num178, num179 - 1);
		///		if (Main.netMode == 2)
		///			NetMessage.SendData(17, -1, -1, null, 0, num178, num179 - 1);
		///		}
		///	else {
		///		if (TileLoader.OpenDoorID(Main.tile[num178, num179 - 1]) >= 0) {
		///
		/// </summary>
		/// <param name="il"></param>
		private void PreventPeonsBreakingDoors(ILContext il)
		{
			/// Match the following IL:
			/// 	IL_B6B4: ldarg.0
			/// 	IL_B6B5: ldfld     int32 Terraria.NPC::'type'
			/// 	IL_B6BA: ldc.i4.s  26
			/// 	IL_B6BC: bne.un.s  IL_B707

			ILCursor cursor = new ILCursor(il);

			// Find the sequence of instructions. This places the cursor on ldarg.0.
			if (!cursor.TryGotoNext(MoveType.Before,
					i => i.MatchLdarg(0),
					i => i.MatchLdfld(typeof(NPC).GetField(nameof(NPC.type))),
					i => i.MatchLdcI4(NPCID.GoblinPeon),
					i => i.MatchBneUn(out _)
				))
			{
				throw new Exception("Unable to patch Terraria.NPC.AI_003_Fighters: Could not match IL.");
			}

			// Move the cursor to the instruction loading the Goblin Peon's ID (26).
			cursor.Index += 2;

			// Replace 26 with int.MinValue. The code now checks if npc.type  == int.MinValue.
			cursor.Remove();
			cursor.Emit(OpCodes.Ldc_I4, int.MinValue);
		}
	}
}