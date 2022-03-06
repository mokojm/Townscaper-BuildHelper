using Placemaker;
using System.Collections;

namespace BuildHelper
{
	public class Harmony_Main
	{
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
					}

					if (BuildHelperMain.RemoveVoxelsRayKeyB)
					{
						BuildHelperMain.RemoveVoxelsRayKeyB = false;
						BuildHelperMain.RemoveVoxelsRay();
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
