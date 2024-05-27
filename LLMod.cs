using LiquidsLib.Liquids;
using MonoMod.RuntimeDetour;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Terraria.ModLoader;

namespace LiquidsLib
{
	// Please read https://github.com/tModLoader/tModLoader/wiki/Basic-tModLoader-Modding-Guide#mod-skeleton-contents for more information about the various files in a mod.
	public class LLMod : Mod
	{
		private static List<Hook> hooks = new();
		public override void Load() {
			hooks.Add(new(ModContentResizeArrays, ModContent_ResizeArrays_Detour));
			foreach (Hook hook in hooks) {
				hook.Apply();
			}

			LL_Liquid.Load();
		}
		private delegate void orig_ModContent_ResizeArrays(bool unloading = false);
		private delegate void hook_ModContent_ResizeArrays(orig_ModContent_ResizeArrays orig, bool unloading = false);
		private static readonly MethodInfo ModContentResizeArrays = typeof(ModContent).GetMethod("ResizeArrays", BindingFlags.NonPublic | BindingFlags.Static);
		private static void ModContent_ResizeArrays_Detour(orig_ModContent_ResizeArrays orig, bool unloading = false) {
			orig(unloading);
			LL_LiquidLoader.ResizeArrays(unloading);
		}
		public override void Unload() {
			LL_LiquidLoader.Unload();
		}
	}
}
