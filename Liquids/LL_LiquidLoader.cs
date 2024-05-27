using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.GameContent.Liquid;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Core;
using MonoMod.Utils;
using System.Reflection;
using System.Linq.Expressions;

namespace Terraria.ModLoader {
    public static class LL_LiquidLoader {
        private static bool loaded = false;
        private static int nextLiquid = LiquidID.Count;
        public static int LiquidCount => nextLiquid;
        internal static readonly IList<GlobalLiquid> globalLiquids = new List<GlobalLiquid>();

        private static readonly MethodInfo buildGlobalHook = typeof(ModLoader).GetMethods(BindingFlags.Static | BindingFlags.NonPublic).First(m => m.Name == "BuildGlobalHook" && m.ReturnType == typeof(void) && m.IsGenericMethodDefinition);
        private static void BuildGlobalHook<T, F>(ref F[] list, IList<T> providers, Expression<Func<T, F>> expr) where F : Delegate {
			MethodInfo constructedMethod = buildGlobalHook.MakeGenericMethod(typeof(T), typeof(F));
            object[] args = new object[] { list, providers, expr };
            constructedMethod.Invoke(null, args);
            list = (F[])args[0];
		}

		//private static void BuildGlobalHook<T, F>(ref F[] list, IList<T> providers, Expression<Func<T, F>> expr) where F : Delegate => buildGlobalHook.Invoke(null, new object[] { list, providers, expr });
		internal static void ResizeArrays(bool unloading = false) {
			//Hooks
            if (!unloading) {
                BuildGlobalHook(ref HookModifyLight, globalLiquids, g => g.ModifyLight);
                BuildGlobalHook(ref HookLiquidPreUpdate, globalLiquids, g => g.PreUpdate);
                BuildGlobalHook(ref HookLiquidUpdate, globalLiquids, g => g.Update);
                BuildGlobalHook(ref HookLiquidPostUpdate, globalLiquids, g => g.PostUpdate);
                BuildGlobalHook(ref HookAllowMergeLiquids, globalLiquids, g => g.AllowMergeLiquids);
                BuildGlobalHook(ref HookGetLiquidMergeTypes, globalLiquids, g => g.GetLiquidMergeTypes);
                BuildGlobalHook(ref HookShouldDeleteLiquids, globalLiquids, g => g.ShouldDeleteLiquid);
                BuildGlobalHook(ref HookPreventMerge, globalLiquids, g => g.PreventMerge);
                BuildGlobalHook(ref HookOnMerge, globalLiquids, g => g.OnMerge);
                BuildGlobalHook(ref HookCanMoveLeft, globalLiquids, g => g.CanMoveLeft);
                BuildGlobalHook(ref HookCanMoveRight, globalLiquids, g => g.CanMoveRight);
                BuildGlobalHook(ref HookCanMoveDown, globalLiquids, g => g.CanMoveDown);
                BuildGlobalHook(ref HookShouldDrawLiquids, globalLiquids, g => g.ShouldDrawLiquids);

                loaded = true;
			}
		}
		internal static void Unload() {
			loaded = false;
			nextLiquid = LiquidID.Count;
		}

		private delegate void DelegateModifyLight(int x, int y, int liquidType, ref float r, ref float g, ref float b);
        private static DelegateModifyLight[] HookModifyLight;

		public static void ModifyLight(int x, int y, int liquidType, ref float r, ref float g, ref float b)
        {
            //GetLiquid(liquidType)?.ModifyLight(x, y, ref r, ref g, ref b);

            foreach (var hook in HookModifyLight)
            {
                hook(x, y, liquidType, ref r, ref g, ref b);
            }
        }

        private delegate void DelegateLiquidPreUpdate(int x, int y, int liquidType, Liquid liquid, Tile thisTile, Tile left, Tile right, Tile up, Tile down);
        private static DelegateLiquidPreUpdate[] HookLiquidPreUpdate;

		public static void PreUpdate(int x, int y, int liquidType, Liquid liquid, Tile thisTile, Tile left, Tile right, Tile up, Tile down)
        {
            //GetLiquid(liquidType)?.PreUpdate(x, y, liquid, thisTile, left, right, up, down);

            foreach (var hook in HookLiquidPreUpdate)
            {
                hook(x, y, liquidType, liquid, thisTile, left, right, up, down);
            }
        }

        private delegate bool DelegateLiquidUpdate(int x, int y, int liquidType, Liquid liquid, Tile thisTile, Tile left, Tile right, Tile up, Tile down);
        private static DelegateLiquidUpdate[] HookLiquidUpdate;

		public static bool Update(int x, int y, int liquidType, Liquid liquid, Tile thisTile, Tile left, Tile right, Tile up, Tile down)
        {
            //bool result = GetLiquid(liquidType)?.Update(x, y, liquid, thisTile, left, right, up, down) ?? true;
            bool result = true;

            foreach (var hook in HookLiquidUpdate)
            {
                if (!hook(x, y, liquidType, liquid, thisTile, left, right, up, down))
                    result = false;
            }

            return result;
        }

        private delegate void DelegateLiquidPostUpdate(int x, int y, int liquidType, Liquid liquid, Tile thisTile, Tile left, Tile right, Tile up, Tile down);
        private static DelegateLiquidPostUpdate[] HookLiquidPostUpdate;

		public static void PostUpdate(int x, int y, int liquidType, Liquid liquid, Tile thisTile, Tile left, Tile right, Tile up, Tile down)
        {
            //GetLiquid(liquidType)?.PostUpdate(x, y, liquid, thisTile, left, right, up, down);

            foreach (var hook in HookLiquidPostUpdate)
            {
                hook(x, y, liquidType, liquid, thisTile, left, right, up, down);
            }
        }

        private delegate bool DelegateAllowMergeLiquids(int x, int y, Tile tile, int x2, int y2, Tile tile2);
        private static DelegateAllowMergeLiquids[] HookAllowMergeLiquids;

		public static bool AllowMergeLiquids(int x, int y, Tile tile, int x2, int y2, Tile tile2)
        {
            //AllowMergeLiquids is only called when there is a liquid at Main.tile[x, y] and Main.tile[x2, y2] and they will always be different types.
            //if (GetLiquid(tile.LiquidType)?.AllowMergeLiquids(x, y, tile, x2, y2, tile2) == false)
            //	return false;

            //if (GetLiquid(tile2.LiquidType)?.AllowMergeLiquids(x, y, tile, x2, y2, tile2) == false)
            //	return false;

            foreach (var hook in HookAllowMergeLiquids)
            {
                bool? shouldMerge = hook(x, y, tile, x2, y2, tile2);
                if (shouldMerge.HasValue)
                {
                    if (!shouldMerge.Value)
                        return false;
                }
            }

            return true;
        }

        private delegate void DelegateGetLiquidMergeTypes(int x, int y, int type, bool[] liquidNearby, ref int liquidMergeTileType, ref int liquidMergeType, LiquidMerge liquidMerge);
        private static DelegateGetLiquidMergeTypes[] HookGetLiquidMergeTypes;

		public static void GetLiquidMergeTypes(int x, int y, int liquidType, bool[] liquidsNearby, ref int liquidMergeTileType, ref int liquidMergeType, LiquidMerge liquidMerge)
        {
            SortedSet<int> liquidsToCheck = new() { liquidType };
            for (int i = 0; i < liquidsNearby.Length; i++)
            {
                if (liquidsNearby[i])
                    liquidsToCheck.Add(i);
            }

            //foreach (int liquidToCheck in liquidsToCheck) {
            //	GetLiquid(liquidToCheck)?.GetLiquidMergeTypes(x, y, liquidType, liquidsNearby, ref liquidMergeTileType, ref liquidMergeType, liquidMerge);
            //}

            foreach (var hook in HookGetLiquidMergeTypes)
            {
                hook(x, y, liquidType, liquidsNearby, ref liquidMergeTileType, ref liquidMergeType, liquidMerge);
            }
        }

        private delegate bool DelegateShouldDeleteLiquids(LiquidMerge liquidMerge);
        private static DelegateShouldDeleteLiquids[] HookShouldDeleteLiquids;

		public static bool ShouldDeleteLiquid(LiquidMerge liquidMerge)
        {
            SortedSet<int> liquidsToCheck = new();
            foreach (LiquidMergeIngredient liquidMergeIngredient in liquidMerge.LiquidMergeIngredients)
            {
                liquidsToCheck.Add(liquidMergeIngredient.LiquidType);
            }

            //foreach (int liquidToCheck in liquidsToCheck)
            //{
            //    if (GetLiquid(liquidToCheck)?.ShouldDeleteLiquid(liquidMerge) == false)
            //        return false;
            //}

            foreach (var hook in HookShouldDeleteLiquids)
            {
                if (!hook(liquidMerge))
                    return false;
            }

            return true;
        }

        private delegate bool DelegatePreventMerge(LiquidMerge liquidMerge);
        private static DelegatePreventMerge[] HookPreventMerge;

		public static bool PreventMerge(LiquidMerge liquidMerge)
        {
            //if (GetLiquid(liquidMerge.MergeTargetTile.LiquidType)?.PreventMerge(liquidMerge) ?? false)
            //    return true;

            foreach (var hook in HookPreventMerge)
            {
                if (hook(liquidMerge))
                    return true;
            }

            return false;
        }

		private delegate void DelegateOnMerge(LiquidMerge liquidMerge, Dictionary<int, int> consumedLiquids);
        private static DelegateOnMerge[] HookOnMerge;

		public static void OnMerge(LiquidMerge liquidMerge, Dictionary<int, int> consumedLiquids)
        {
            //GetLiquid(liquidMerge.MergeTargetTile.LiquidType)?.OnMerge(liquidMerge, consumedLiquids);

            foreach (var hook in HookOnMerge)
            {
                hook(liquidMerge, consumedLiquids);
            }
        }

        private delegate bool? DelegateCanMoveLeft(int x, int y, int xMove, int yMove, bool canMoveLeftVanilla);
        private static DelegateCanMoveLeft[] HookCanMoveLeft;
        public static bool CanMoveLeft(int x, int y, int xMove, int yMove, bool canMoveLeftVanilla)
        {
            bool? result = null;

            foreach (var hook in HookCanMoveLeft)
            {
                bool? move = hook(x, y, xMove, yMove, canMoveLeftVanilla);
                if (move.HasValue)
                {
                    result = move;
                    if (!move.Value)
                        return false;
                }
            }

            return result ?? canMoveLeftVanilla;
        }

        private delegate bool? DelegateCanMoveRight(int x, int y, int xMove, int yMove, bool canMoveRightVanilla);
		private static DelegateCanMoveRight[] HookCanMoveRight;
		public static bool CanMoveRight(int x, int y, int xMove, int yMove, bool canMoveRightVanilla)
        {
            bool? result = null;

            foreach (var hook in HookCanMoveRight)
            {
                bool? move = hook(x, y, xMove, yMove, canMoveRightVanilla);
                if (move.HasValue)
                {
                    result = move;
                    if (!move.Value)
                        return false;
                }
            }

            return result ?? canMoveRightVanilla;
        }

        private delegate bool? DelegateCanMoveDown(int x, int y, int xMove, int yMove, bool canMoveDownVanilla);
        private static DelegateCanMoveDown[] HookCanMoveDown;
        public static bool CanMoveDown(int x, int y, int xMove, int yMove, bool canMoveDownVanilla)
        {
            bool? result = null;

            foreach (var hook in HookCanMoveDown)
            {
                bool? move = hook(x, y, xMove, yMove, canMoveDownVanilla);
                if (move.HasValue)
                {
                    result = move;
                    if (!move.Value)
                        return false;
                }
            }

            return result ?? canMoveDownVanilla;
        }

        private delegate bool? DelegateShouldDrawLiquids(int x, int y, bool shouldDrawVanilla);
        private static DelegateShouldDrawLiquids[] HookShouldDrawLiquids;

		public static bool ShouldDrawLiquids(int x, int y, bool shouldDrawVanilla)
        {
            bool? result = null;

            foreach (var hook in HookShouldDrawLiquids)
            {
                bool? draw = hook(x, y, shouldDrawVanilla);
                if (draw.HasValue)
                {
                    result = draw;
                    if (!draw.Value)
                        return false;
                }
            }

            return result ?? shouldDrawVanilla;
        }
    }
}
