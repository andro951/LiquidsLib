using MonoMod.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.ID;
using Terraria.WorldBuilding;
using Terraria;
using Mono.Cecil.Cil;
using Terraria.GameContent.Liquid;
using Microsoft.CodeAnalysis.Emit;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace LiquidsLib.Liquids {
	public static class LL_Liquid {
		#region IL and On

		public static void Load() {
			//IL_Liquid.LiquidCheck += IL_Liquid_LiquidCheck;
			IL_Liquid.Update += IL_Liquid_Update;
			On_Liquid.SettleWaterAt += On_Liquid_SettleWaterAt;
			On_Liquid.LiquidCheck += On_Liquid_LiquidCheck;
			On_WorldGen.PlaceLiquid += On_WorldGen_PlaceLiquid;
			IL_LiquidRenderer.InternalPrepareDraw += IL_LiquidRenderer_InternalPrepareDraw;
		}

		private static void IL_LiquidRenderer_InternalPrepareDraw(ILContext il) {
			var c = new ILCursor(il);

			// ptr2->IsSolid = WorldGen.SolidOrSlopedTile(tile);
			//IL_01a7: ldloc.s 4
			//IL_01a9: ldloc.1
			//IL_01aa: call bool Terraria.WorldGen::SolidOrSlopedTile(valuetype Terraria.Tile)
			//IL_01af: stfld bool Terraria.GameContent.Liquid.LiquidRenderer / LiquidCache::IsSolid

			if (!c.TryGotoNext(MoveType.After,
				i => i.MatchLdloc(4),
				i => i.MatchLdloc(1),
				i => i.MatchCall(typeof(WorldGen).GetMethods().First(m => m.Name == "SolidOrSlopedTile" && m.GetParameters().Length == 1))
				)) {
				throw new Exception("Failed to find instructions IL_LiquidRenderer_InternalPrepareDraw 1/1");
			}

			c.EmitLdloc(7);
			c.EmitLdloc(8);
			c.EmitDelegate((bool isSolid, int x, int y) => !LL_LiquidLoader.ShouldDrawLiquids(x, y, !isSolid));
		}

		private static bool On_WorldGen_PlaceLiquid(On_WorldGen.orig_PlaceLiquid orig, int x, int y, byte liquidType, byte amount) {
			return PlaceLiquid(x, y, liquidType, amount);
			//return orig(x, y, liquidType, amount);//Completely replace original
		}
		private static bool PlaceLiquid(int x, int y, byte liquidType, byte amount) {
			if (!WorldGen.InWorld(x, y))
				return false;

			Tile tile = Main.tile[x, y];
			if (tile == null)
				return false;

			ushort b = (ushort)tile.LiquidType;
			if (tile.HasUnactuatedTile && Main.tileSolid[tile.TileType] && !Main.tileSolidTop[tile.TileType])
				return false;

			if (tile.LiquidAmount == 0 || liquidType == b) {
				tile.LiquidType = liquidType;
				if (amount + tile.LiquidAmount > 255)
					amount = (byte)(255 - tile.LiquidAmount);

				tile.LiquidAmount += amount;
				WorldGen.SquareTileFrame(x, y);
				if (Main.netMode != 0)
					NetMessage.sendWater(x, y);

				return true;
			}

			LiquidMerge liquidMerge = new(x, y, liquidType, amount);
			liquidMerge.TryPlaceMerge();

			/*
			int liquidMergeTileType = 0;
			bool waterNearby = b == 0;
			bool lavaNearby = b == 1;
			bool honeyNearby = b == 2;
			bool shimmerNearby = b == 3;
			int liquidMergeType = 0;
			Liquid.GetLiquidMergeTypes(liquidType, out liquidMergeTileType, out liquidMergeType, waterNearby, lavaNearby, honeyNearby, shimmerNearby);
			if (liquidMergeTileType != 0) {
				tile.liquid = 0;
				tile.liquidType(0);
				PlaceTile(x, y, liquidMergeTileType, mute: true);
				SquareTileFrame(x, y);
				if (Main.netMode != 0)
					NetMessage.SendTileSquare(-1, x - 1, y - 1, GetLiquidChangeType(liquidType, b));

				return true;
			}
			*/

			return false;
		}
		private static void On_Liquid_LiquidCheck(On_Liquid.orig_LiquidCheck orig, int x, int y, int thisLiquidType) {
			if (WorldGen.SolidTile(x, y))
				return;

			LiquidMerge liquidMerge = new(x, y);
			liquidMerge.TryMerge();
			//orig(x, y, thisLiquidType);//Completely replace original
		}

		private static void On_Liquid_SettleWaterAt(On_Liquid.orig_SettleWaterAt orig, int originX, int originY) {
			SettleWaterAt(originX, originY);
			//orig(originX, originY);//Completely replace original
		}

		private static void SettleWaterAt(int originX, int originY) {
			Tile tile = Main.tile[originX, originY];
			Liquid.tilesIgnoreWater(ignoreSolids: true);
			if (tile.LiquidAmount == 0)
				return;

			int num = originX;
			int num2 = originY;
			bool tileAtXYHasLava = tile.Lava();
			bool flag = tile.Honey();
			bool flag2 = tile.Shimmer();
			int num3 = tile.LiquidAmount;
			ushort b = (ushort)tile.LiquidType;
			tile.LiquidAmount = 0;
			bool flag3 = true;
			while (true) {
				Tile tile2 = Main.tile[num, num2 + 1];
				bool flag4 = false;
				bool canMoveDownVanilla = num2 < Main.maxTilesY - 5 && tile2.LiquidAmount == 0 && (!tile2.HasUnactuatedTile || !Main.tileSolid[tile2.TileType] || Main.tileSolidTop[tile2.TileType]);
				/*
				while (num2 < Main.maxTilesY - 5 && tile2.LiquidAmount == 0 && (!tile2.HasUnactuatedTile || !Main.tileSolid[tile2.TileType] || Main.tileSolidTop[tile2.TileType])) {
				*/
				while (LL_LiquidLoader.CanMoveDown(originX, originY, num, num2 + 1, canMoveDownVanilla)) {
					num2++;
					flag4 = true;
					flag3 = false;
					tile2 = Main.tile[num, num2 + 1];
					canMoveDownVanilla = num2 < Main.maxTilesY - 5 && tile2.LiquidAmount == 0 && (!tile2.HasUnactuatedTile || !Main.tileSolid[tile2.TileType] || Main.tileSolidTop[tile2.TileType]);
				}

				if (WorldGen.gen && flag4 && !flag && !flag2) {
					if (WorldGen.remixWorldGen)
						b = (byte)((num2 > GenVars.lavaLine && ((double)num2 < Main.rockLayer - 80.0 || num2 > Main.maxTilesY - 350)) ? ((!WorldGen.oceanDepths(num, num2)) ? 1 : 0) : 0);
					else if (num2 > GenVars.waterLine)
						b = 1;
				}

				int num4 = -1;
				int num5 = 0;
				int num6 = -1;
				int num7 = 0;
				bool flag5 = false;
				bool flag6 = false;
				bool flag7 = false;
				while (true) {
					//Left or Right tile Check
					if (Main.tile[num + num5 * num4, num2].LiquidAmount == 0) {
						num6 = num4;
						num7 = num5;
					}

					//Check if too close to min/max x
					if (num4 == -1 && num + num5 * num4 < 5)
						flag6 = true;
					else if (num4 == 1 && num + num5 * num4 > Main.maxTilesX - 5)
						flag5 = true;

					//Check if down tile has the same type of liquid and not full, then transfer liquid to it.
					tile2 = Main.tile[num + num5 * num4, num2 + 1];
					if (tile2.LiquidAmount != 0 && tile2.LiquidAmount != byte.MaxValue && tile2.LiquidType == b) {
						//Move liquid to the down tile
						int num8 = 255 - tile2.LiquidAmount;
						if (num8 > num3)
							num8 = num3;

						tile2.LiquidAmount += (byte)num8;
						num3 -= num8;
						if (num3 == 0)
							break;
					}

					//Potential problem here is that the tile being looked at Main.tile[num, num2] is not always the original tile, and won't have values for
					//	liquid type or liquid, making it difficult to work with.  May need to have a CanSettleLiquids hook with extra info passed in.
					bool canMoveDown = num2 < Main.maxTilesY - 5 && tile2.LiquidAmount == 0 && (!tile2.HasUnactuatedTile || !Main.tileSolid[tile2.TileType] || Main.tileSolidTop[tile2.TileType]);
					int moveX = num + num5 * num4;
					/*
					if (num2 < Main.maxTilesY - 5 && tile2.LiquidAmount == 0 && (!tile2.HasUnactuatedTile || !Main.tileSolid[tile2.TileType] || Main.tileSolidTop[tile2.TileType])) {
					*/
					if (LL_LiquidLoader.CanMoveDown(moveX, num2, moveX, num2 + 1, canMoveDown)) {
						flag7 = true;
						break;
					}

					//Check horizontal tile.
					Tile tile3 = Main.tile[num + (num5 + 1) * num4, num2];
					bool cantMoveHorizontal = (tile3.LiquidAmount != 0 && (!flag3 || num4 != 1)) || (tile3.HasUnactuatedTile && Main.tileSolid[tile3.TileType] && !Main.tileSolidTop[tile3.TileType]);
					bool canMoveHorizontalVanilla = !cantMoveHorizontal;
					int nextMoveX = num + (num5 + 1) * num4;
					bool canMoveHorizontal = num4 == -1 ? LL_LiquidLoader.CanMoveLeft(num, num2, nextMoveX, num2, canMoveHorizontalVanilla) : LL_LiquidLoader.CanMoveRight(num, num2, nextMoveX, num2, canMoveHorizontalVanilla);
					/*
					if ((tile3.LiquidAmount != 0 && (!flag3 || num4 != 1)) || (tile3.HasUnactuatedTile && Main.tileSolid[tile3.TileType] && !Main.tileSolidTop[tile3.TileType])) {
					*/
					if (!canMoveHorizontal) {
						if (num4 == 1)
							flag5 = true;
						else
							flag6 = true;
					}

					if (flag6 && flag5)
						break;

					if (flag5) {
						num4 = -1;
						num5++;
					}
					else if (flag6) {
						if (num4 == 1)
							num5++;

						num4 = 1;
					}
					else {
						if (num4 == 1)
							num5++;

						num4 = -num4;
					}
				}

				num += num7 * num6;
				if (num3 == 0 || !flag7)
					break;

				num2++;
			}

			Tile tile4 = Main.tile[num, num2];
			tile4.LiquidAmount = (byte)num3;
			tile4.LiquidType = b;
			if (Main.tile[num, num2].LiquidAmount > 0) {
				AttemptToMoveLava(num, num2, tileAtXYHasLava);
				AttemptToMoveHoney(num, num2, flag);
				AttemptToMoveShimmer(num, num2, flag2);
			}

			Liquid.tilesIgnoreWater(ignoreSolids: false);
		}
		private static void AttemptToMoveHoney(int X, int Y, bool tileAtXYHasHoney) {
			if (Main.tile[X - 1, Y].LiquidAmount > 0 && Main.tile[X - 1, Y].Honey() != tileAtXYHasHoney) {
				if (tileAtXYHasHoney)
					Liquid.HoneyCheck(X, Y);
				else
					Liquid.HoneyCheck(X - 1, Y);
			}
			else if (Main.tile[X + 1, Y].LiquidAmount > 0 && Main.tile[X + 1, Y].Honey() != tileAtXYHasHoney) {
				if (tileAtXYHasHoney)
					Liquid.HoneyCheck(X, Y);
				else
					Liquid.HoneyCheck(X + 1, Y);
			}
			else if (Main.tile[X, Y - 1].LiquidAmount > 0 && Main.tile[X, Y - 1].Honey() != tileAtXYHasHoney) {
				if (tileAtXYHasHoney)
					Liquid.HoneyCheck(X, Y);
				else
					Liquid.HoneyCheck(X, Y - 1);
			}
			else if (Main.tile[X, Y + 1].LiquidAmount > 0 && Main.tile[X, Y + 1].Honey() != tileAtXYHasHoney) {
				if (tileAtXYHasHoney)
					Liquid.HoneyCheck(X, Y);
				else
					Liquid.HoneyCheck(X, Y + 1);
			}
		}

		private static void AttemptToMoveLava(int X, int Y, bool tileAtXYHasLava) {
			if (Main.tile[X - 1, Y].LiquidAmount > 0 && Main.tile[X - 1, Y].Lava() != tileAtXYHasLava) {
				if (tileAtXYHasLava)
					Liquid.LavaCheck(X, Y);
				else
					Liquid.LavaCheck(X - 1, Y);
			}
			else if (Main.tile[X + 1, Y].LiquidAmount > 0 && Main.tile[X + 1, Y].Lava() != tileAtXYHasLava) {
				if (tileAtXYHasLava)
					Liquid.LavaCheck(X, Y);
				else
					Liquid.LavaCheck(X + 1, Y);
			}
			else if (Main.tile[X, Y - 1].LiquidAmount > 0 && Main.tile[X, Y - 1].Lava() != tileAtXYHasLava) {
				if (tileAtXYHasLava)
					Liquid.LavaCheck(X, Y);
				else
					Liquid.LavaCheck(X, Y - 1);
			}
			else if (Main.tile[X, Y + 1].LiquidAmount > 0 && Main.tile[X, Y + 1].Lava() != tileAtXYHasLava) {
				if (tileAtXYHasLava)
					Liquid.LavaCheck(X, Y);
				else
					Liquid.LavaCheck(X, Y + 1);
			}
		}

		private static void AttemptToMoveShimmer(int X, int Y, bool tileAtXYHasShimmer) {
			if (Main.tile[X - 1, Y].LiquidAmount > 0 && Main.tile[X - 1, Y].Shimmer() != tileAtXYHasShimmer) {
				if (tileAtXYHasShimmer)
					Liquid.ShimmerCheck(X, Y);
				else
					Liquid.ShimmerCheck(X - 1, Y);
			}
			else if (Main.tile[X + 1, Y].LiquidAmount > 0 && Main.tile[X + 1, Y].Shimmer() != tileAtXYHasShimmer) {
				if (tileAtXYHasShimmer)
					Liquid.ShimmerCheck(X, Y);
				else
					Liquid.ShimmerCheck(X + 1, Y);
			}
			else if (Main.tile[X, Y - 1].LiquidAmount > 0 && Main.tile[X, Y - 1].Shimmer() != tileAtXYHasShimmer) {
				if (tileAtXYHasShimmer)
					Liquid.ShimmerCheck(X, Y);
				else
					Liquid.ShimmerCheck(X, Y - 1);
			}
			else if (Main.tile[X, Y + 1].LiquidAmount > 0 && Main.tile[X, Y + 1].Shimmer() != tileAtXYHasShimmer) {
				if (tileAtXYHasShimmer)
					Liquid.ShimmerCheck(X, Y);
				else
					Liquid.ShimmerCheck(X, Y + 1);
			}
		}
		private static bool Lava(this Tile tile) => tile.LiquidType == LiquidID.Lava;
		private static bool Water(this Tile tile) => tile.LiquidType == LiquidID.Water;
		private static bool Honey(this Tile tile) => tile.LiquidType == LiquidID.Honey;
		private static bool Shimmer(this Tile tile) => tile.LiquidType == LiquidID.Shimmer;

		private static void IL_Liquid_Update(ILContext il) {
			var c = new ILCursor(il);

			//// if ((!tile4.nactive() || !Main.tileSolid[tile4.type] || Main.tileSolidTop[tile4.type]) && (tile4.liquid <= 0 || tile4.liquidType() == tile5.liquidType()) && tile4.liquid < byte.MaxValue)
			//IL_0331: ldloca.s 3
			//IL_0333: call instance bool Terraria.Tile::nactive()
			//IL_0338: brfalse.s IL_035d

			//IL_033a: ldsfld bool[] Terraria.Main::tileSolid
			//IL_033f: ldloca.s 3
			//IL_0341: call instance uint16 & Terraria.Tile::get_type()
			//IL_0346: ldind.u2
			//IL_0347: ldelem.u1
			//IL_0348: brfalse.s IL_035d


			//Note To self: Labels only exist as part of brfalse and similar branch instructions.  The instruction they point to has no way of knowing it's being pointed to.
			//Look through all labels and save the ones that match the unique instructions found at the target instruction.  (be careful with this as it's not the same as finding the first occurance)
			//
			Func<Instruction, bool>[] functionsToMatch = [
				i => i.MatchLdloca(3),
				i => i.MatchCall<Terraria.Tile>("nactive"),
				i => i.MatchBrfalse(out _),
				i => i.MatchLdsfld(typeof(Main).GetField("tileSolid"))
			];

			List<ILLabel> labelsToBeUpdated = new();
			while (c.Next != null) {
				if (c.Next.Operand is ILLabel l) {
					Instruction i = l.Target;
					bool match = true;
					foreach (Func<Instruction, bool> func in functionsToMatch) {
						if (!func(i)) {
							match = false;
							break;
						}

						i = i.Next;
					}

					if (match)
						labelsToBeUpdated.Add(l);
				}

				c.Index++;
			}

			c.Index = 0;

			if (!c.TryGotoNext(MoveType.Before,
					functionsToMatch
				)) {
				throw new Exception("Failed to find instructions IL_Liquid_Update 1/2");
			}

			//IL_0331: ldloca.s 3
			//IL_0333: call instance bool Terraria.Tile::nactive()
			//IL_0338: brfalse.s IL_035d

			//IL_033a: ldsfld bool[] Terraria.Main::tileSolid
			//IL_033f: ldloca.s 3
			//IL_0341: call instance uint16 & Terraria.Tile::get_type()
			//IL_0346: ldind.u2
			//IL_0347: ldelem.u1
			//IL_0348: brfalse.s IL_035d

			//// (no C# code)
			//IL_034a: ldsfld bool[] Terraria.Main::tileSolidTop
			//IL_034f: ldloca.s 3
			//IL_0351: call instance uint16 & Terraria.Tile::get_type()
			//IL_0356: ldind.u2
			//IL_0357: ldelem.u1
			//IL_0358: brfalse IL_0476

			//IL_035d: ldloca.s 3
			//IL_035f: call instance uint8 & Terraria.Tile::get_liquid()
			//IL_0364: ldind.u1
			//IL_0365: ldc.i4.0
			//IL_0366: ble.s IL_037b

			//IL_0368: ldloca.s 3
			//IL_036a: call instance uint8 Terraria.Tile::liquidType()
			//IL_036f: ldloca.s 4
			//IL_0371: call instance uint8 Terraria.Tile::liquidType()
			//IL_0376: bne.un IL_0476

			//$"c.Index: {c.Index} Instruction: {c.Next}".LogSimple();
			//$"c.Index: {c.Index} Instruction: {c.Prev}".LogSimple();

			ILLabel firstCanMoveDownLabel = c.DefineLabel();
			for (int i = 0; i < 1;) {
				//string s = c.Next.Operand is ILLabel ? "Label" : c.Next.ToString();
				//string p = c.Prev.Operand is ILLabel ? "Label" : c.Prev.ToString();
				string s1 = c.Next.OpCode.ToString();
				//string p1 = c.Prev.OpCode.ToString();
				if (s1 == "brfalse") {
					i++;
				}

				if (i < 1) {
					c.Remove();
				}
				else {
					c.MarkLabel(firstCanMoveDownLabel);//Mark before your first instruction after the removed instruction that was targeted by a label
					c.EmitLdarg(0);
					c.EmitLdfld(typeof(Liquid).GetField("x"));
					c.EmitLdarg(0);
					c.EmitLdfld(typeof(Liquid).GetField("y"));
					c.EmitLdloc(3);
					c.EmitLdloc(4);
					c.EmitDelegate((int x, int y, Tile tile4, Tile tile5) => {
						bool canMoveDown = (!tile4.HasUnactuatedTile || !Main.tileSolid[tile4.TileType] || Main.tileSolidTop[tile4.TileType]) && (tile4.LiquidAmount <= 0 || tile4.LiquidType == tile5.LiquidType) && tile4.LiquidAmount < byte.MaxValue;
						return LL_LiquidLoader.CanMoveDown(x, y, x, y + 1, canMoveDown);
					});

					//Intentionally removed instructions until a brfalse instruction that points to the place I want to go and used it's operand to make my brfalse.
					//The old one is deleted below.
					c.Emit(OpCodes.Brfalse, c.Next.Operand);
				}
			}

			//Change the target of all labels that pointed to the removed instruction to the new one.
			foreach (ILLabel l in labelsToBeUpdated) {
				l.Target = firstCanMoveDownLabel.Target;
			}

			for (int i = 0; i < 1;) {
				//string s = c.Next.Operand is ILLabel ? "Label" : c.Next.ToString();
				//string p = c.Prev.Operand is ILLabel ? "Label" : c.Prev.ToString();
				string s1 = c.Next.OpCode.ToString();
				//string p1 = c.Prev.OpCode.ToString();
				if (s1 == "bne.un") {
					i++;
				}

				c.Remove();
			}

			//// bool flag2 = true;
			//IL_0484: ldc.i4.1
			//IL_0485: stloc.s 9
			//// bool flag3 = true;
			//IL_0487: ldc.i4.1
			//IL_0488: stloc.s 10
			//// bool flag4 = true;
			//IL_048a: ldc.i4.1
			//IL_048b: stloc.s 11
			//// bool flag5 = true;
			//IL_048d: ldc.i4.1
			//IL_048e: stloc.s 12

			if (!c.TryGotoNext(MoveType.Before,
				i => i.MatchLdcI4(1),
				i => i.MatchStloc(9),
				i => i.MatchLdcI4(1),
				i => i.MatchStloc(10),
				i => i.MatchLdcI4(1),
				i => i.MatchStloc(11),
				i => i.MatchLdcI4(1),
				i => i.MatchStloc(12)
				)) {
				throw new Exception("Failed to find instructions IL_Liquid_Update 1/2");
			}

			//IL_0011: ldarg.0
			//IL_0012: ldfld int32 Terraria.Liquid::x
			//IL_0017: ldc.i4.1
			//IL_0018: sub
			//IL_0019: ldarg.0
			//IL_001a: ldfld int32 Terraria.Liquid::y

			c.EmitLdarg(0);
			c.EmitLdfld(typeof(Liquid).GetField("x"));
			c.EmitLdarg(0);
			c.EmitLdfld(typeof(Liquid).GetField("y"));

			//c.EmitDelegate(CheckMoveLiquids);
			c.EmitDelegate(CheckMoveLiquidsLeftOrRight);

			var label = c.DefineLabel();
			c.Emit(OpCodes.Br, label);

			//IL_1656: ldloca.s 4
			//IL_1658: call instance uint8 & Terraria.Tile::get_liquid()
			//IL_165d: ldind.u1
			//IL_165e: ldloc.s 5
			//IL_1660: beq.s IL_16c7

			if (!c.TryGotoNext(MoveType.Before,
				i => i.MatchLdloca(4),
				i => i.MatchCall(out _),
				i => i.MatchLdindU1(),
				i => i.MatchLdloc(5),
				i => i.MatchBeq(out _)
				)) {
				throw new Exception("Failed to find instructions IL_Liquid_Update 2/2");
			}

			c.MarkLabel(label);
		}
		public static void LogSimple(this string s) => ModContent.GetInstance<LiquidsLib>().Logger.Info(s);
		/*
		private static bool MergeLiquidsShouldDoVanilla(int x, int y, int thisLiquidType) {
			if (!WorldGen.InWorld(x, y, 1))
				return true;

			Tile tile = Main.tile[x, y];
			Tile up = Main.tile[x, y - 1];
			Tile left = Main.tile[x - 1, y];
			Tile right = Main.tile[x + 1, y];
			int[] liquids = new int[4];
			liquids[thisLiquidType] += tile.LiquidAmount;
			Action onSucces = null;
			onSucces += () => tile.LiquidAmount = 0;
			if (left.LiquidAmount > 0 && left.LiquidType != thisLiquidType) {
				liquids[left.LiquidType] += left.LiquidAmount;
				onSucces += () => left.LiquidAmount = 0;
			}

			if (right.LiquidAmount > 0 && right.LiquidType != thisLiquidType) {
				liquids[right.LiquidType] += right.LiquidAmount;
				onSucces += () => right.LiquidAmount = 0;
			}

			if (up.LiquidAmount > 0 && up.LiquidType != thisLiquidType) {
				liquids[up.LiquidType] += up.LiquidAmount;
				onSucces += () => up.LiquidAmount = 0;
			}

			return MergeLiquidsShouldDoVanilla(x, y, thisLiquidType, liquids, onSucces);
		}
		private static bool MergeLiquidsShouldDoVanillaDownOnly(int x, int y, int thisLiquidType) {
			if (y == Main.maxTilesY)
				return true;

			Tile tile = Main.tile[x, y];
			Tile down = Main.tile[x, y + 1];
			int[] liquids = new int[4];
			liquids[thisLiquidType] += tile.LiquidAmount;
			Action onSucces = null;
			onSucces += () => tile.LiquidAmount = 0;
			if (down.LiquidAmount > 0 && down.LiquidType != thisLiquidType) {
				liquids[down.LiquidType] += down.LiquidAmount;
				onSucces += () => down.LiquidAmount = 0;
			}

			return MergeLiquidsShouldDoVanilla(x, y + 1, thisLiquidType, liquids, onSucces);
		}
		private static bool MergeLiquidsShouldDoVanilla(int x, int y, int thisLiquidType, int[] liquids, Action postAction) {
			Liquid.GetLiquidMergeTypes(thisLiquidType, out int liquidMergeTileType, out int liquidMergeType, liquids[LiquidID.Water] > 0, liquids[LiquidID.Lava] > 0, liquids[LiquidID.Honey] > 0, liquids[LiquidID.Shimmer] > 0);
			if (liquidMergeType == thisLiquidType)
				return false;

			int lavaCount = liquids[LiquidID.Lava];
			switch (liquidMergeTileType) {
				case TileID.Obsidian:
					int tileType = lavaCount < 240 ? lavaCount < 64 ? TileID.Stone : TileID.Silt : TileID.Obsidian;
					PlaceBlockFromLiquidMerge(x, y, tileType, thisLiquidType, liquidMergeType);
					postAction();
					return false;
			}

			return true;
		}
		private static void IL_Liquid_LiquidCheck(ILContext il) {
			//IL_009a: ldc.i4.0
			//IL_009b: stloc.s 5

			var c = new ILCursor(il);

			if (!c.TryGotoNext(MoveType.After,
				i => i.MatchLdcI4(0),
				i => i.MatchStloc(5)
				)) {
				throw new Exception("Failed to find instructions IL_Liquid_LiquidCheck 1/3");
			}

			c.EmitLdarg(0);
			c.EmitLdarg(1);
			c.EmitLdarg(2);
			c.EmitDelegate((int x, int y, int thisLiquidType) => {
				if (ES_WorldGen.SkyblockWorld)
					return MergeLiquidsShouldDoVanilla(x, y, thisLiquidType);

				return true;
			});

			var label = c.DefineLabel();
			c.Emit(OpCodes.Brtrue_S, label);
			c.Emit(OpCodes.Ret);
			c.MarkLabel(label);

			//IL_0341: ldloca.s 3
			//IL_0343: call instance bool Terraria.Tile::active()
			//IL_0348: ldc.i4.0
			//IL_0349: ceq
			//IL_034b: ldloc.s 13
			//IL_034d: or
			//IL_034e: brtrue.s IL_0351

			if (!c.TryGotoNext(MoveType.After,
				i => i.MatchLdloca(3),
				i => i.MatchCall(out _),
				i => i.MatchLdcI4(0),
				i => i.MatchCeq(),
				i => i.MatchLdloc(13),
				i => i.MatchOr()
				)) {
				throw new Exception("Failed to find instructions IL_Liquid_LiquidCheck 2/3");
			}

			var label3 = c.DefineLabel();
			c.Remove();
			c.Emit(OpCodes.Brtrue_S, label3);

			//IL_0351: ldloca.s 4
			//IL_0353: call instance uint8 & Terraria.Tile::get_liquid()
			//IL_0358: ldind.u1
			//IL_0359: ldc.i4.s 24

			if (!c.TryGotoNext(MoveType.Before,
				i => i.MatchLdloca(4),
				i => i.MatchCall(out _),
				i => i.MatchLdindU1(),
				i => i.MatchLdcI4(24)
				)) {
				throw new Exception("Failed to find instructions IL_Liquid_LiquidCheck 2/3");
			}

			c.MarkLabel(label3);
			c.EmitLdarg(0);
			c.EmitLdarg(1);
			c.EmitLdarg(2);
			c.EmitDelegate((int x, int y, int thisLiquidType) => {
				if (ES_WorldGen.SkyblockWorld)
					return MergeLiquidsShouldDoVanillaDownOnly(x, y, thisLiquidType);

				return true;
			});

			var label2 = c.DefineLabel();
			c.Emit(OpCodes.Brtrue_S, label2);
			c.Emit(OpCodes.Ret);
			c.MarkLabel(label2);
		}
		*/
		//private static bool MovePrevented(Tile tile) => tile.HasUnactuatedTile && Main.tileSolid[tile.TileType] && !Main.tileSolidTop[tile.TileType];
		//private static bool WillMerge(Tile tile) => tile.LiquidAmount > 0 && tile.LiquidType != tile.LiquidType;
		private static bool MovePrevented(Tile tile) => tile.HasUnactuatedTile && Main.tileSolid[tile.TileType] && !Main.tileSolidTop[tile.TileType];
		private static bool WillMerge(Tile tile, Tile other) => other.LiquidAmount > 0 && tile.LiquidType != other.LiquidType;
		private static void CheckMoveLiquidsLeftOrRight(int x, int y) {
			Tile tile = Main.tile[x, y];
			if (tile.LiquidAmount > 0) {
				float num = 0f;
				int numLeft = 0;
				int numRight = 0;
				int liquidAmount = 0;
				for (int i = 1; i <= 3; i++) {
					int leftX = x - i;
					Tile tileL = Main.tile[leftX, y];
					bool canMoveLeft = true;
					if (MovePrevented(tileL) || WillMerge(tile, tileL)) {
						canMoveLeft = false;
					}
					else if (i > 1 && tileL.LiquidAmount == 0) {
						canMoveLeft = false;
					}
					else if (i == 2 && tile.LiquidAmount > 250) {
						canMoveLeft = false;
					}

					if (LL_LiquidLoader.CanMoveLeft(x, y, leftX, y, canMoveLeft)) {
						numLeft++;
						liquidAmount += tileL.LiquidAmount;
					}

					int rightX = x + i;
					Tile tileR = Main.tile[rightX, y];
					bool canMoveRight = true;
					if (MovePrevented(tileR) || WillMerge(tile, tileR)) {
						canMoveRight = false;
					}
					else if (i > 1 && tileR.LiquidAmount == 0) {
						canMoveRight = false;
					}
					else if (i == 2 && tile.LiquidAmount > 250) {
						canMoveRight = false;
					}

					if (LL_LiquidLoader.CanMoveRight(x, y, rightX, y, canMoveRight)) {
						numRight++;
						liquidAmount += tileR.LiquidAmount;
					}

					if (!canMoveLeft || !canMoveRight)
						break;
				}

				num += tile.LiquidAmount + liquidAmount;
				if (tile.LiquidAmount < 3)
					num--;

				byte newAmount = (byte)Math.Round(num / (float)(1 + numLeft + numRight));
				if (newAmount == byte.MaxValue - 1 && WorldGen.genRand.Next(30) == 0)
					newAmount = byte.MaxValue;

				bool anyUpdated = false;
				int higherNum = Math.Max(numLeft, numRight);
				for (int i = 1; i <= higherNum; i++) {
					if (i <= numLeft) {
						int tileX = x - i;
						Tile tileL = Main.tile[tileX, y];
						tileL.LiquidType = tile.LiquidType;
						if (tileL.LiquidAmount != newAmount || tile.LiquidAmount != newAmount) {
							tileL.LiquidAmount = newAmount;
							Liquid.AddWater(tileX, y);
							anyUpdated = true;
						}
					}

					if (i <= numRight) {
						int tileX = x + i;
						Tile tileLR = Main.tile[tileX, y];
						tileLR.LiquidType = tile.LiquidType;
						if (tileLR.LiquidAmount != newAmount || tile.LiquidAmount != newAmount) {
							tileLR.LiquidAmount = newAmount;
							Liquid.AddWater(tileX, y);
							anyUpdated = true;
						}
					}
				}

				if (anyUpdated || numLeft < 2 && numRight < 2 || Main.tile[x, y - 1].LiquidAmount <= 0)
					tile.LiquidAmount = newAmount;
			}
		}


		//private static void CheckMoveLiquids(int x, int y)
		//{
		//    Tile tile = Main.tile[x, y];
		//    if (tile.LiquidAmount > 0)
		//    {
		//        float num = 0f;
		//        int numLeft = 0;
		//        int numRight = 0;
		//        int liquidAmount = 0;
		//        for (int i = 1; i <= 3; i++)
		//        {
		//            int leftX = x - i;
		//            Tile tileL = Main.tile[leftX, y];
		//            bool leftCanMove = true;
		//            if (MovePrevented(tileL) || WillMerge(tileL))
		//            {
		//                leftCanMove = false;
		//            }
		//            else if (i > 1 && tileL.LiquidAmount == 0)
		//            {
		//                leftCanMove = false;
		//            }
		//            else if (i == 2 && tile.LiquidAmount > 250)
		//            {
		//                leftCanMove = false;
		//            }

		//            if (CanMove(x, y, leftX, y, leftCanMove) ?? leftCanMove)
		//            {
		//                numLeft++;
		//                liquidAmount += tileL.LiquidAmount;
		//            }

		//            int rightX = x + i;
		//            Tile tileR = Main.tile[rightX, y];
		//            bool rightCanMove = true;
		//            if (MovePrevented(tileR) || WillMerge(tileR))
		//            {
		//                rightCanMove = false;
		//            }
		//            else if (i > 1 && tileR.LiquidAmount == 0)
		//            {
		//                rightCanMove = false;
		//            }
		//            else if (i == 2 && tile.LiquidAmount > 250)
		//            {
		//                rightCanMove = false;
		//            }

		//            if (CanMove(x, y, rightX, y, rightCanMove) ?? rightCanMove)
		//            {
		//                numRight++;
		//                liquidAmount += tileR.LiquidAmount;
		//            }

		//            if (!leftCanMove || !rightCanMove)
		//                break;
		//        }

		//        num += tile.LiquidAmount + liquidAmount;
		//        if (tile.LiquidAmount < 3)
		//            num--;

		//        byte newAmount = (byte)Math.Round(num / (1 + numLeft + numRight));
		//        if (newAmount == byte.MaxValue - 1 && WorldGen.genRand.Next(30) == 0)
		//            newAmount = byte.MaxValue;

		//        bool anyUpdated = false;
		//        int higherNum = Math.Max(numLeft, numRight);
		//        for (int i = 1; i <= higherNum; i++)
		//        {
		//            if (i <= numLeft)
		//            {
		//                int tileX = x - i;
		//                Tile tileL = Main.tile[tileX, y];
		//                tileL.LiquidType = tile.LiquidType;
		//                if (tileL.LiquidAmount != newAmount || tile.LiquidAmount != newAmount)
		//                {
		//                    tileL.LiquidAmount = newAmount;
		//                    Liquid.AddWater(tileX, y);
		//                    anyUpdated = true;
		//                }
		//            }

		//            if (i <= numRight)
		//            {
		//                int tileX = x + i;
		//                Tile tileLR = Main.tile[tileX, y];
		//                tileLR.LiquidType = tile.LiquidType;
		//                if (tileLR.LiquidAmount != newAmount || tile.LiquidAmount != newAmount)
		//                {
		//                    tileLR.LiquidAmount = newAmount;
		//                    Liquid.AddWater(tileX, y);
		//                    anyUpdated = true;
		//                }
		//            }
		//        }

		//        if (anyUpdated || numLeft < 2 && numRight < 2 || Main.tile[x, y - 1].LiquidAmount <= 0)
		//            tile.LiquidAmount = newAmount;
		//    }
		//}

		#endregion
	}
}
