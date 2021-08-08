using Mono.Cecil.Cil;
using MonoMod.Cil;
using NoBreakingDoors.Common.Configs;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace NoBreakingDoors.Common.GlobalNPCs
{
	public class FighterAIDoorBehaviourModifier : GlobalNPC
	{
		public override bool Autoload(ref string name)
		{
			IL.Terraria.NPC.AI_003_Fighters += PreventDoorInteractions;
			return base.Autoload(ref name);
		}

		/// <summary>
		/// Changes the following check in NPC.AI_003_Fighters:
		///
		/// WorldGen.KillTile(num178, num179 - 1, fail: true);
		/// if ((Main.netMode != 1 || !flag23) && flag23 && Main.netMode != 1) {
		/// 	if (type == 26) {
		///			WorldGen.KillTile(num178, num179 - 1);
		///
		/// to:
		///
		/// WorldGen.KillTile(num178, num179 - 1, fail: true);
		/// if ((Main.netMode != 1 || !flag23) && flag23 && !ModContent.GetInstance<DoorOptionsConfig>().StopOpeningDoors && Main.netMode != 1) {
		/// 	if (type == int.MinValue) {
		///			WorldGen.KillTile(num178, num179 - 1);
		///
		/// </summary>
		private void PreventDoorInteractions(ILContext il)
		{
			ILCursor cursor = new ILCursor(il);

			#region Opening Door Change

			/// Match the following IL:
			///		IL_B69D: ldloc     V_262
			///		IL_B6A1: nop
			///		IL_B6A2: nop
			///		IL_B6A3: and
			///		IL_B6A4: brfalse IL_BFB3
			/// This places the cursor onto ldloc

			if (!cursor.TryGotoNext(MoveType.Before,
					i => i.MatchLdloc(262),
					i => i.MatchNop(),
					i => i.MatchNop(),
					i => i.MatchAnd(),
					i => i.MatchBrfalse(out _)
					))
			{
				throw new Exception("Unable to patch Terraria.NPC.AI_003_Fighters: Could not match IL (Opening Door Change).");
			}

			// Move onto the AND instruction, then push !StopOpeningDoors.
			// AND the current value and !StopOpeningDoors
			cursor.Index += 4;
			cursor.EmitDelegate<Func<bool>>(() => !ModContent.GetInstance<DoorOptionsConfig>().StopOpeningDoors);
			cursor.Emit(OpCodes.And);

			#endregion Opening Door Change

			#region Peon Change

			/// Match the following IL:
			///		IL_B6B4: ldarg.0
			///		IL_B6B5: ldfld     int32 Terraria.NPC::'type'
			///		IL_B6BA: ldc.i4.s  26
			///		IL_B6BC: bne.un.s  IL_B707
			/// This places the cursor on ldarg.0.

			if (!cursor.TryGotoNext(MoveType.Before,
					i => i.MatchLdarg(0),
					i => i.MatchLdfld<NPC>(nameof(NPC.type)),
					i => i.MatchLdcI4(NPCID.GoblinPeon)
				))
			{
				throw new Exception("Unable to patch Terraria.NPC.AI_003_Fighters: Could not match IL (Peon Change).");
			}

			// Move the cursor to the instruction loading the Goblin Peon's ID (26).
			cursor.Index += 2;

			// Replace 26 with int.MinValue. The code now checks if npc.type == int.MinValue.
			cursor.Remove();
			cursor.Emit(OpCodes.Ldc_I4, int.MinValue);

			#endregion Peon Change
		}
	}
}