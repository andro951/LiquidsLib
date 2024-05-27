using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.ID;

namespace Terraria.ModLoader {
	public class LiquidMergeIngredientBeingPlaced : LiquidMergeIngredient {
		public override byte LiquidAmount => liquidAmount;
		private byte liquidAmount;
		public override int LiquidType => liquidType;
		private int liquidType;
		public override void DeleteLiquid() {
			liquidAmount = 0;
			liquidType = LiquidID.Water;
		}
		public override byte ConsumeLiquid() {
			byte consumedAmount = Math.Min(liquidAmount, LiquidAmountToConsume);
			liquidAmount -= consumedAmount;
			if (liquidAmount == 0)
				liquidType = LiquidID.Water;

			return consumedAmount;
		}
		public LiquidMergeIngredientBeingPlaced(int x, int y, int liquidType, byte amount) : base(x, y) {
			liquidAmount = amount;
			this.liquidType = liquidType;
		}
	}
}
