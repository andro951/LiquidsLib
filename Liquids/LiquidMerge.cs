using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.ID;
using Terraria;

namespace Terraria.ModLoader {
	public class LiquidMerge {
		public enum MergeStyle {
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

		public void GetLiquidMergeTypes() {
			Liquid.GetLiquidMergeTypes(ThisLiquidType, out LiquidMergeTileType, out LiquidMergeType, LiquidsNearby[LiquidID.Water], LiquidsNearby[LiquidID.Lava], LiquidsNearby[LiquidID.Honey], LiquidsNearby[LiquidID.Shimmer]);
		}
		public int X { get; private set; }
		public int Y { get; private set; }
		internal int ThisLiquidType => thisLiquid?.Tile.LiquidType ?? (MergeAllowed ? LiquidMergeIngredients[0].LiquidType : 0);
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
		public MergeStyle mergeType = MergeStyle.None;
		public bool SolidTile {  get; private set; }
		public string Context { get; private set; } = "";

		/// <summary>
		/// Check if a merge will occur and set the MergeTargetTile and LiquidMergeIngredients.
		/// Shouldn't be called for a PlaceMerge.
		/// </summary>
		/// <returns>true if a merge will occur.</returns>
		private bool DetermineMergeStyle() {
			//Check for top merge (Merge onto ThisTile from LeftTile, UpTile or RightTile)
			leftLiquid.CheckBeingUsedForMerge(X, Y, ThisTile, LeftTile, SolidTile);
			rightLiquid.CheckBeingUsedForMerge(X, Y, ThisTile, RightTile, SolidTile);
			upLiquid.CheckBeingUsedForMerge(X, Y, ThisTile, UpTile, SolidTile);
			bool topMerge = leftLiquid.CausingMerge(ThisLiquidType) || rightLiquid.CausingMerge(ThisLiquidType) || upLiquid.CausingMerge(ThisLiquidType);
			if (topMerge) {
				mergeType = MergeStyle.TopMerge;
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
			bool bottomMerge = false;
			//thisLiquid will always have a LiquidAmount > 0 because it's a condition for it being called.
			//Bottom Merge uses the downLiquid as the source, but hasn't been checked if it has a liquid yet.  Prevents merging liquids with downLiquid of water (0) with 0 amount.
			if (downLiquid.LiquidAmount > 0) {
				thisLiquid.CheckBeingUsedForMerge(X, Y, DownTile, ThisTile, WorldGen.SolidTile(X, Y));
				bottomMerge = thisLiquid.CausingMerge(downLiquid.LiquidType);
			}
			
			if (bottomMerge) {
				mergeType = MergeStyle.BottomMerge;
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
		public bool MergeTargetTileWillBeDestroyedByMerge => mergeType == MergeStyle.TopMerge && WillBeDestroyedByObsidianKill || mergeType == MergeStyle.BottomMerge && (WillBeDestroyedByCut || WillBeDestroyedByObsidianKill);
		private bool WillBeDestroyedByObsidianKill => MergeTargetTile.HasTile && Main.tileObsidianKill[MergeTargetTile.TileType];
		private bool WillBeDestroyedByCut => ThisLiquidType != LiquidID.Water && Main.tileCut[MergeTargetTile.TileType];
		private void TryKillMergeTargetTile() {
			if (!MergeTargetTileWillBeDestroyedByMerge)
				return;

			if (mergeType == MergeStyle.BottomMerge && WillBeDestroyedByCut) {
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
				case MergeStyle.TopMerge:
					if (!WorldGen.gen)
						WorldGen.PlayLiquidChangeSound(LiquidChangeType, X, Y);

					WorldGen.PlaceTile(X, Y, LiquidMergeTileType, mute: true, forced: true);
					WorldGen.SquareTileFrame(X, Y);
					if (Main.netMode == 2)
						NetMessage.SendTileSquare(-1, X - 1, Y - 1, 3, LiquidChangeType);
					break;
				case MergeStyle.BottomMerge:
				case MergeStyle.None:
					if (!Main.gameMenu)
						WorldGen.PlayLiquidChangeSound(LiquidChangeType, X, Y);

					WorldGen.PlaceTile(X, Y, LiquidMergeTileType, mute: true, forced: true);
					WorldGen.SquareTileFrame(X, Y);
					if (Main.netMode == 2)
						NetMessage.SendTileSquare(-1, X - 1, Y - 1, 3, LiquidChangeType);
					break;
				case MergeStyle.PlaceMerge:
					WorldGen.PlaceTile(X, Y, LiquidMergeTileType, mute: true);
					WorldGen.SquareTileFrame(X, Y);
					if (Main.netMode != 0)
						NetMessage.SendTileSquare(-1, X - 1, Y - 1, LiquidChangeType);

					break;
			}
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="consumedLiquids"></param>
		/// <returns>Returns false if TargetTile !Main.tileObsidianKill or !Main.tileObsidianKill.  Liquids will be consumed and nothing will happen if so.</returns>
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

			if (MergeTargetTile.HasTile)//TODO: Check IF this Needs to allow for tiles to be changed from one type to another by merging.  It might already work becasue of the kill tile.
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
			SolidTile = WorldGen.SolidTile(x, y);
			DetermineMergeStyle();
		}

		/// <summary>
		/// This constructor is for creating custom LiquidMerges.  To use it, you have to set up all of the ingredients manually.
		/// If you don't want other mods to change the merge, you can only call Merge() and LL_Loader.OnMerge().
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <param name="ingredients"></param>
		/// <param name="context">Context is the way you check if it's "your" merge in the other hooks for performing specific logic with this merge.
		///		Vanilla merges have a blank context, "".</param>
		public LiquidMerge(int x, int y, IEnumerable<LiquidMergeIngredient> ingredients, string context = "") {
			X = x;
			Y = y;
			LiquidMergeIngredients = ingredients.ToList();
			Context = context;
			SolidTile = WorldGen.SolidTile(x, y);
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
			mergeType = MergeStyle.PlaceMerge;
		}
	}
}
