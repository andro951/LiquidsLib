using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ID;

namespace Terraria.ModLoader {
	public class LiquidMergeIngredientTileCustom : LiquidMergeIngredient {
		public override byte LiquidAmount => liquidAmount ?? Tile.LiquidAmount;
		public override int LiquidType => liquidType ?? Tile.LiquidType;
		private byte? liquidAmount;
		private int? liquidType;
		private byte? liquidConsumeAmount;
		internal override void SetBeingUsedForMerge() {
			if (LiquidAmount <= 0)
				return;

			BeingUsedForMerge = true;
			LiquidAmountToConsume = liquidConsumeAmount ?? LiquidAmount;
		}
		internal override void CheckBeingUsedForMerge(int x, int y, Tile tile, Tile tile2, bool tileSolid) {
			if (LiquidAmount <= 0)
				return;

			BeingUsedForMerge = LL_LiquidLoader.AllowMergeLiquids(x, y, tile, tileSolid, X, Y, tile2, WorldGen.SolidTile(X, Y));
			if (BeingUsedForMerge)
				LiquidAmountToConsume = liquidConsumeAmount ?? LiquidAmount;
		}
		public override byte ConsumeLiquid() {
			if (liquidType.HasValue)
				return LiquidAmount;//Don't consume liquid in the specified tile if a liquidType was specified in the constructor.

			//If no liquidType was specified, consume liquids in the tile.
			//If a consume amount was specified, use that amount.  Otherwise, use the normal amount from the result of the merge.  (all unless changed by a mod)
			byte consumedAmount = liquidConsumeAmount.HasValue ? liquidConsumeAmount.Value : Math.Min(Tile.LiquidAmount, LiquidAmountToConsume);
			Tile.LiquidAmount -= consumedAmount;
			if (Tile.LiquidAmount == 0)
				Tile.SetLiquid(LiquidID.Water, true);

			//If an amount was specified, provide that value to the consumed liquids.  Provided to LL_LiquidLoader.OnMerge() for post merge actions.
			return liquidAmount ?? consumedAmount;
		}
		/// <summary>
		/// Used for more customizable ingredients.  For instance, if you want the ingredient to be based on a tile, but use a different liquid or amount than exists in the tile.
		/// </summary>
		/// <param name="x">Tile x</param>
		/// <param name="y">Tile y</param>
		/// <param name="liquidAmount">Amount of liquid to be available to the merge.  Amount that will be consumed from the tile if liquidConsumeAmount isn't specified.</param>
		/// <param name="liquidConsumeAmount">Forces the consumed amount of liquid removed from the Tile to be this amount.  The amount it thinks it consumed in OnMerge() will always be liquidAmount if specified.</param>
		/// <param name="liquidType">Setting this changes the liquid type.  also, no liquids will be consumed from the Tile if specified, even if they are the same type.  Only use if you need to prevent any liquids from being consumed.</param>
		/// <param name="usedForMerge">Calls SetBeingUsedForMerge() which causes the this ingredient to be used/consumed by the merge.</param>
		public LiquidMergeIngredientTileCustom(int x, int y, byte? liquidAmount = null, byte? liquidConsumeAmount = null, int? liquidType = null, bool usedForMerge = true) : base(x, y) {
			this.liquidAmount = liquidAmount;
			this.liquidType = liquidType;
			this.liquidConsumeAmount = liquidConsumeAmount;
			if (usedForMerge)
				SetBeingUsedForMerge();
		}
	}
}
