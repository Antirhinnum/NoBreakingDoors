using Mono.Cecil.Cil;
using MonoMod.Cil;
using NoBreakingDoors.Common.Configs;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace NoBreakingDoors.Common.Systems;

public sealed class FighterAIDoorBehaviourModifier : ModSystem
{
	public override void Load()
	{
		IL_NPC.AI_003_Fighters += PreventDoorInteractions;
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
		ILCursor c = new(il);

		#region Opening Door Change

		// Match (C#):
		//	if (type == 460) { flag = true; }
		// Match (IL):
		//	ldarg.0
		//	ldfld int32 Terraria.NPC::'type'
		//	ldc.i4 460
		//	bne.un.s LABEL
		//	ldc.i4.1
		//	stloc LOCAL
		// Need LOCAL for code below.
		int localIndex = -1;
		if (!c.TryGotoNext(MoveType.Before,
			i => i.MatchLdarg(0),
			i => i.MatchLdfld<NPC>(nameof(NPC.type)),
			i => i.MatchLdcI4(NPCID.Butcher),
			i => i.MatchBneUn(out _),
			i => i.MatchLdcI4(1), // true
			i => i.MatchStloc(out localIndex)
			))
		{
			throw new Exception("Unable to patch Terraria.NPC.AI_003_Fighters: Could not match IL (Finding Local Index).");
		}

		// Match (C#):
		//	WorldGen.KillTile(num194, num195 - 1, fail: true);
		// Change to:
		//	WorldGen.KillTile(num194, num195 - 1, fail: true);
		//	LOCAL &= !ModContent.GetInstance<DoorOptionsConfig>().StopOpeningDoors;
		// LOCAL is whether any NPC should try hitting doors. By and-ing the config option, NPCs won't hit doors if they're disallwoed by this mod.
		if (!c.TryGotoNext(MoveType.After,
			// Match the five params for KillTile
			i => true,
			i => true,
			i => true,
			i => true,
			i => true,
			i => i.MatchCall<WorldGen>(nameof(WorldGen.KillTile))
		))
		{
			throw new Exception("Unable to patch Terraria.NPC.AI_003_Fighters: Could not match IL (Finding Local Index).");
		}

		c.Emit(OpCodes.Ldloc, localIndex);
		c.EmitDelegate(() => !ModContent.GetInstance<DoorOptionsConfig>().StopOpeningDoors);
		c.Emit(OpCodes.And);
		c.Emit(OpCodes.Stloc, localIndex);

		#endregion Opening Door Change

		#region Peon Change

		/// Match the following IL:
		///		IL_B6B4: ldarg.0
		///		IL_B6B5: ldfld     int32 Terraria.NPC::'type'
		///		IL_B6BA: ldc.i4.s  26
		///		IL_B6BC: bne.un.s  IL_B707
		/// This places the cursor on ldarg.0.

		if (!c.TryGotoNext(MoveType.Before,
				i => i.MatchLdarg(0),
				i => i.MatchLdfld<NPC>(nameof(NPC.type)),
				i => i.MatchLdcI4(NPCID.GoblinPeon)
			))
		{
			throw new Exception("Unable to patch Terraria.NPC.AI_003_Fighters: Could not match IL (Peon Change).");
		}

		// Move the cursor to the instruction loading the Goblin Peon's ID (26).
		c.Index += 2;

		// Replace 26 with NPCID.None. The code now checks if npc.type == NPCID.None.
		// The OpCode here is ldc.i4.s, so I'm constrained to the range of an sbyte.
		c.Next.Operand = NPCID.None;

		#endregion Peon Change

		MonoModHooks.DumpIL(Mod, il);
	}
}