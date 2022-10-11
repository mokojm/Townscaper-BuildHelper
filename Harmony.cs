using Placemaker;
using Placemaker.Ui;
using System.Collections;
using ModUI;
using MelonLoader;

namespace BuildHelper
{
	public class Harmony_Main
	{
		[HarmonyLib.HarmonyPatch(typeof(SideMenu), "Button_Quit_Full")]
		public class BuildQuit
		{
			public static void Prefix(ref SideMenu __instance)
			{
				MelonLogger.Msg("A");
				HelperUI.RadiusSlider.textField.text = "Radius";
				UIManager.ToggleUI();
			}
		}

		[HarmonyLib.HarmonyPatch(typeof(HoverData), "SetHover")]
		public class BuildHelper
		{

			
			public static void Postfix(ref HoverData __instance)
			{
				
				if (BuildHelperMain.isInitialized == false)
				{
					BuildHelperMain.Initialize(__instance);
				}

				else
                {
					if (BuildHelperMain.AddVoxelsKeyB)
					{
						BuildHelperMain.AddVoxelsKeyB = false;
						BuildHelperMain.AddVoxels();
					}

					if (BuildHelperMain.RemoveVoxelsKeyB)
					{
						BuildHelperMain.RemoveVoxelsKeyB = false;
						BuildHelperMain.RemoveVoxels();
					}

					if (BuildHelperMain.AddVoxelHeightKeyB)
					{
						BuildHelperMain.AddVoxelHeightKeyB = false;
						BuildHelperMain.AddVoxelHeight();
						BuildHelperMain.speedLock = true;
					}

					if (BuildHelperMain.RemoveVoxelsRayKeyB)
					{
						BuildHelperMain.RemoveHandler();
					}

					if (BuildHelperMain.PaintVoxelsKeyB)
					{
						BuildHelperMain.PaintVoxelsKeyB = false;
						//BuildHelperMain.PaintVoxels();
						BuildHelperMain.StartPaintVoxels();
					}
				}			
			}
		}
	}
}
