using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;

namespace Terraria.ModLoader {
	public class LiquidMergeIngredientTile : LiquidMergeIngredient {
		public override byte LiquidAmount => Tile.LiquidAmount;
		public override int LiquidType => Tile.LiquidType;
		public LiquidMergeIngredientTile(int x, int y) : base(x, y) { }
	}
}
