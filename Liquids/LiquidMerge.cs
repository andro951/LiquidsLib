using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.ID;
using Terraria;

namespace Terraria.ModLoader {
	public class LiquidMerge {
		public enum MergeType {
			None,
			TopMerge,
			BottomMerge,
			PlaceMerge
		}
		/// <summary>
		/// Controls the sound played when a merge happens.
		/// </summary>
		public TileChangeType LiquidChangeType {
			get {
				if (!liquidChangeSoundDetermined && LiquidMergeType != LiquidMergeDefaultType) {
					liquidChangeType = WorldGen.GetLiquidChangeType(ThisLiquidType, LiquidMergeType);
					liquidChangeSoundDetermined = true;
				}

				return liquidChangeType;
			}
			set {
				liquidChangeType = value;
				liquidChangeSoundDetermined = true;
			}
		}
		private TileChangeType liquidChangeType = TileChangeType.None;
		bool liquidChangeSoundDetermined = false;
		public int LiquidMergeTileType = LiquidMergeTileDefaultType;
		private const int LiquidMergeTileDefaultType = -1;

		public int LiquidMergeType = LiquidMergeDefaultType;
		private const int LiquidMergeDefaultType = -1;

		internal void GetLiquidMergeTypes() {
			Liquid.GetLiquidMergeTypes(ThisLiquidType, out LiquidMergeTileType, out LiquidMergeType, LiquidsNearby[LiquidID.Water], LiquidsNearby[LiquidID.Lava], LiquidsNearby[LiquidID.Honey], LiquidsNearby[LiquidID.Shimmer]);
		}
		public int X { get; private set; }
		public int Y { get; private set; }
		internal int ThisLiquidType => thisLiquid.Tile.LiquidType;
		private LiquidMergeIngredient thisLiquid;
		private LiquidMergeIngredient leftLiquid;
		private LiquidMergeIngredient upLiquid;
		private LiquidMergeIngredient rightLiquid;
		private LiquidMergeIngredient downLiquid;
		private Tile ThisTile => thisLiquid.Tile;
		private Tile LeftTile => leftLiquid.Tile;
		private Tile UpTile => upLiquid.Tile;
		private Tile RightTile => rightLiquid.Tile;
		private Tile DownTile => downLiquid.Tile;
		public Tile MergeTargetTile { get; private set; }
		public List<LiquidMergeIngredient> LiquidMergeIngredients { get; private set; } = null;
		internal bool MergeAllowed => LiquidMergeIngredients != null && LiquidMergeIngredients.Count > 0;
		public MergeType mergeType = MergeType.None;

		/// <summary>
		/// Check if a merge will occur and set the MergeTargetTile and LiquidMergeIngredients.
		/// Shouldn't be called for a PlaceMerge.
		/// </summary>
		/// <returns>true if a merge will occur.</returns>
		private bool DetermineMergeType() {
			//Check for top merge (Merge onto ThisTile from LeftTile, UpTile or RightTile)
			leftLiquid.CheckBeingUsedForMerge(X, Y, ThisTile, LeftTile);
			rightLiquid.CheckBeingUsedForMerge(X, Y, ThisTile, RightTile);
			upLiquid.CheckBeingUsedForMerge(X, Y, ThisTile, UpTile);
			bool topMerge = leftLiquid.CausingMerge(ThisLiquidType) || rightLiquid.CausingMerge(ThisLiquidType) || upLiquid.CausingMerge(ThisLiquidType);
			if (topMerge) {
				mergeType = MergeType.TopMerge;
				LiquidMergeIngredients = new();
				thisLiquid.SetBeingUsedForMerge();
				LiquidMergeIngredients.Add(thisLiquid);

				if (leftLiquid.BeingUsedForMerge)
					LiquidMergeIngredients.Add(leftLiquid);

				if (rightLiquid.BeingUsedForMerge)
					LiquidMergeIngredients.Add(rightLiquid);

				if (upLiquid.BeingUsedForMerge)
					LiquidMergeIngredients.Add(upLiquid);

				return true;
			}

			//Check for bottom merge (ThisTile merging onto DownTile)
			Y = downLiquid.Y;//Bottom merges cause a merge to happen at the DownTile, using ThisTile as an ingredient, so change the target of the merge to the DownTile.
			thisLiquid.CheckBeingUsedForMerge(X, Y, DownTile, ThisTile);
			bool bottomMerge = downLiquid.LiquidAmount > 0 && thisLiquid.CausingMerge(downLiquid.LiquidType);
			if (bottomMerge) {
				mergeType = MergeType.BottomMerge;
				LiquidMergeIngredients = new();
				downLiquid.SetBeingUsedForMerge();
				LiquidMergeIngredients.Add(downLiquid);

				if (thisLiquid.BeingUsedForMerge)
					LiquidMergeIngredients.Add(thisLiquid);

				return true;
			}

			return false;
		}
		public bool[] LiquidsNearby {
			get {
				if (liquidsNearby == null) {
					UpdateLiquidsNearby();
				}

				return liquidsNearby;
			}
		}
		private bool[] liquidsNearby = null;
		public void UpdateLiquidsNearby() {
			liquidsNearby = new bool[LL_LiquidLoader.LiquidCount];
			foreach (LiquidMergeIngredient liquidMergeIngredient in LiquidMergeIngredients) {
				liquidsNearby[liquidMergeIngredient.LiquidType] = true;
			}
		}
		public int[] LiquidsNearbyAmounts {
			get {
				if (liquidsNearbyAmount == null) {
					UpdateLiquidsNearbyAmounts();
				}

				return liquidsNearbyAmount;
			}
		}
		private int[] liquidsNearbyAmount = null;
		public void UpdateLiquidsNearbyAmounts() {
			liquidsNearbyAmount = new int[LL_LiquidLoader.LiquidCount];
			foreach (LiquidMergeIngredient liquidMergeIngredient in LiquidMergeIngredients) {
				liquidsNearbyAmount[liquidMergeIngredient.LiquidType] += liquidMergeIngredient.LiquidAmount;
			}
		}
		public void ClearLiquidsNearby() {
			liquidsNearby = null;
			liquidsNearbyAmount = null;
		}
		public void DeleteLiquids() {
			for (int i = 1; i < LiquidMergeIngredients.Count; i++) {
				LiquidMergeIngredient liquidMergeIngredient = LiquidMergeIngredients[i];
				liquidMergeIngredient.DeleteLiquid();
			}
		}
		public bool MergeTargetTileWillBeDestroyedByMerge => mergeType == MergeType.TopMerge && WillBeDestroyedByObsidianKill || mergeType == MergeType.BottomMerge && (WillBeDestroyedByCut || WillBeDestroyedByObsidianKill);
		private bool WillBeDestroyedByObsidianKill => MergeTargetTile.HasTile && Main.tileObsidianKill[MergeTargetTile.TileType];
		private bool WillBeDestroyedByCut => ThisLiquidType != LiquidID.Water && Main.tileCut[MergeTargetTile.TileType];
		private void TryKillMergeTargetTile() {
			if (!MergeTargetTileWillBeDestroyedByMerge)
				return;

			if (mergeType == MergeType.BottomMerge && WillBeDestroyedByCut) {
				WorldGen.KillTile(X, Y);
				if (Main.netMode == 2)
					NetMessage.SendData(17, -1, -1, null, 0, X, Y);

				return;
			}

			WorldGen.KillTile(X, Y);
			if (Main.netMode == 2)
				NetMessage.SendData(17, -1, -1, null, 0, X, Y);
		}
		private void PlaceMergeTile() {
			switch (mergeType) {
				case MergeType.TopMerge:
					if (!WorldGen.gen)
						WorldGen.PlayLiquidChangeSound(liquidChangeType, X, Y);

					WorldGen.PlaceTile(X, Y, LiquidMergeTileType, mute: true, forced: true);
					WorldGen.SquareTileFrame(X, Y);
					if (Main.netMode == 2)
						NetMessage.SendTileSquare(-1, X - 1, Y - 1, 3, liquidChangeType);
					break;
				case MergeType.BottomMerge:
					if (!Main.gameMenu)
						WorldGen.PlayLiquidChangeSound(liquidChangeType, X, Y - 1);

					WorldGen.PlaceTile(X, Y, LiquidMergeTileType, mute: true, forced: true);
					WorldGen.SquareTileFrame(X, Y);
					if (Main.netMode == 2)
						NetMessage.SendTileSquare(-1, X - 1, Y - 1, 3, liquidChangeType);
					break;
				case MergeType.PlaceMerge:
					WorldGen.PlaceTile(X, Y, LiquidMergeTileType, mute: true);
					WorldGen.SquareTileFrame(X, Y);
					if (Main.netMode != 0)
						NetMessage.SendTileSquare(-1, X - 1, Y - 1, liquidChangeType);

					break;
			}
		}
		public bool Merge(out Dictionary<int, int> consumedLiquids) {
			TryKillMergeTargetTile();
			consumedLiquids = new();
			for (int i = 1; i < LiquidMergeIngredients.Count; i++) {
				LiquidMergeIngredient liquidMergeIngredient = LiquidMergeIngredients[i];
				int liquidType = liquidMergeIngredient.LiquidType;
				int consumedAmount = liquidMergeIngredient.ConsumeLiquid();
				if (consumedLiquids.ContainsKey(liquidType)) {
					consumedLiquids[liquidType] += consumedAmount;
				}
				else {
					consumedLiquids.Add(liquidType, consumedAmount);
				}
			}

			if (MergeTargetTile.HasTile)
				return false;

			LiquidMergeIngredient thisLiquidMergeIngredient = LiquidMergeIngredients[0];
			int thisLiquidType = thisLiquidMergeIngredient.LiquidType;
			int thisConsumedAmount = thisLiquidMergeIngredient.LiquidAmountToConsume;
			thisLiquidMergeIngredient.ConsumeLiquid();
			if (consumedLiquids.ContainsKey(thisLiquidType)) {
				consumedLiquids[thisLiquidType] += thisConsumedAmount;
			}
			else {
				consumedLiquids.Add(thisLiquidType, thisConsumedAmount);
			}

			PlaceMergeTile();

			return true;
		}
		public void TryMerge() {
			if (MergeAllowed) {
				GetLiquidMergeTypes();
				LL_LiquidLoader.GetLiquidMergeTypes(X, Y, ThisLiquidType, LiquidsNearby, ref LiquidMergeTileType, ref LiquidMergeType, this);
				int totalOtherLiquids = 0;
				for (int i = 1; i < LiquidMergeIngredients.Count; i++) {
					LiquidMergeIngredient liquidMergeIngredient = LiquidMergeIngredients[i];
					totalOtherLiquids += liquidMergeIngredient.LiquidAmountToConsume;
				}

				bool deleteLiquids = totalOtherLiquids < 24 || ThisLiquidType == LiquidMergeType;
				if (deleteLiquids && LL_LiquidLoader.ShouldDeleteLiquid(this)) {
					DeleteLiquids();
					return;
				}

				ClearLiquidsNearby();

				if (LL_LiquidLoader.PreventMerge(this))
					return;

				ClearLiquidsNearby();

				Merge(out Dictionary<int, int> consumedLiquids);
				LL_LiquidLoader.OnMerge(this, consumedLiquids);
			}
		}
		internal void TryPlaceMerge() {
			if (MergeAllowed) {
				GetLiquidMergeTypes();
				LL_LiquidLoader.GetLiquidMergeTypes(X, Y, ThisLiquidType, LiquidsNearby, ref LiquidMergeTileType, ref LiquidMergeType, this);

				if (LiquidMergeTileType != 0) {
					if (LL_LiquidLoader.PreventMerge(this))
						return;

					ClearLiquidsNearby();

					Merge(out Dictionary<int, int> consumedLiquids);
					LL_LiquidLoader.OnMerge(this, consumedLiquids);
				}
			}
		}
		public LiquidMerge(int x, int y) {
			X = x;
			Y = y;
			thisLiquid = new LiquidMergeIngredientTile(x, y);
			leftLiquid = new LiquidMergeIngredientTile(x - 1, y);
			rightLiquid = new LiquidMergeIngredientTile(x + 1, y);
			upLiquid = new LiquidMergeIngredientTile(x, y - 1);
			downLiquid = new LiquidMergeIngredientTile(x, y + 1);
			DetermineMergeType();
		}

		/// <summary>
		/// Only used in WorldGen.PlaceLiquid(int x, int y, byte liquidType, byte amount)
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <param name="liquidType"></param>
		/// <param name="amount"></param>
		public LiquidMerge(int x, int y, int liquidType, byte amount) {
			X = x;
			Y = y;
			thisLiquid = new LiquidMergeIngredientTile(x, y);
			LiquidMergeIngredientBeingPlaced liquidBeingPlaced = new(x, y, liquidType, amount);
			LiquidMergeIngredients = new();
			thisLiquid.SetBeingUsedForMerge();
			LiquidMergeIngredients.Add(thisLiquid);
			liquidBeingPlaced.SetBeingUsedForMerge();
			LiquidMergeIngredients.Add(liquidBeingPlaced);
			mergeType = MergeType.PlaceMerge;
		}
	}
}
