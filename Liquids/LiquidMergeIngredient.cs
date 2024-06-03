﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.ID;
using Terraria;

namespace Terraria.ModLoader {
	public abstract class LiquidMergeIngredient {
		public Tile Tile { get; private set; }
		public int X { get; private set; }
		public int Y { get; private set; }
		public abstract byte LiquidAmount { get; }
		public abstract int LiquidType { get; }
		public byte LiquidAmountToConsume = 0;
		protected internal bool BeingUsedForMerge { get; protected set; } = false;
		internal virtual void SetBeingUsedForMerge() {
			if (LiquidAmount <= 0)
				return;

			BeingUsedForMerge = true;
			LiquidAmountToConsume = LiquidAmount;
		}
		internal virtual void CheckBeingUsedForMerge(int x, int y, Tile tile, Tile tile2, bool tileSolid) {
			if (LiquidAmount <= 0)
				return;

			BeingUsedForMerge = LL_LiquidLoader.AllowMergeLiquids(x, y, tile, tileSolid, X, Y, tile2, WorldGen.SolidTile(X, Y));
			if (BeingUsedForMerge)
				LiquidAmountToConsume = LiquidAmount;
		}
		internal virtual bool CausingMerge(int liquidType) => BeingUsedForMerge && LiquidType != liquidType;
		public virtual void DeleteLiquid() {
			Tile.LiquidAmount = 0;
			Tile.SetLiquid(LiquidID.Water, true);
		}
		public virtual byte ConsumeLiquid() {
			byte consumedAmount = Math.Min(Tile.LiquidAmount, LiquidAmountToConsume);
			Tile.LiquidAmount -= consumedAmount;
			if (Tile.LiquidAmount == 0)
				Tile.SetLiquid(LiquidID.Water, true);

			return consumedAmount;
		}
		public LiquidMergeIngredient(int x, int y) {
			X = x;
			Y = y;
			Tile = Main.tile[x, y];
		}
		//public LiquidMergeIngredient(int x, int y, int liquidType, byte amount) {
		//	X = x;
		//	Y = y;
		//	Tile = Main.tile[x, y];
		//}
	}

	public static class LiquidStaticMethods {
		public static void SetLiquid(this Tile tile, int liquidId, bool value) {
			if (value)
				tile.LiquidType = liquidId;
			else if (tile.LiquidType == liquidId)
				tile.LiquidType = LiquidID.Water;
		}
	}
}
