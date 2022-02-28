using MelonLoader;
using UnityEngine;
using ModUI;
using System;
using TMPro;
using UnityEngine.UI;

namespace BuildHelper
{
	public static class HelperUI
	{
		public static MelonMod myMod;
		public static ModSettings myModSettings;
		public static UnityEngine.UI.InputField refInputField;

		public static bool isInitialized;

		public static void Initialize(MelonMod thisMod)
		{

			myModSettings = UIManager.Register(thisMod, new Color32(243, 227, 182, 255));

			myModSettings.AddToggle("Fixed Height", "General", new Color32(243, 227, 182, 255), BuildHelperMain.fixedHeight, new Action<bool>(delegate (bool value) { BuildHelperMain.fixedHeight = value; }));
			myModSettings.AddInputField("Height", "General", new Color32(255, 179, 174, 255), TMP_InputField.ContentType.Alphanumeric, "0", new Action<string>(delegate (string value) { FixedHeightInput(value); }));
			refInputField = myModSettings.controlInputFields["Height"].GetComponent<UnityEngine.UI.InputField>();

			myModSettings.AddKeybind("Add", "Input", BuildHelperMain.AddVoxelHeightKey, new Color32(10, 190, 124, 255));
			myModSettings.AddKeybind("Add blocks", "Input", BuildHelperMain.AddVoxelsKey, new Color32(10, 190, 124, 255));
			myModSettings.AddKeybind("Remove blocks", "Input", BuildHelperMain.RemoveVoxelsKey, new Color32(10, 190, 124, 255));
			myModSettings.AddKeybind("Remove line blocks", "Input", BuildHelperMain.RemoveVoxelsRayKey, new Color32(10, 190, 124, 255));
			myModSettings.AddKeybind("Paint", "Input", BuildHelperMain.PaintVoxelsKey, new Color32(10, 190, 124, 255));

			//Apply button
			myModSettings.AddButton("Apply Settings", "General", new Color32(243, 227, 182, 255), new Action(delegate { Apply(); }));

			isInitialized = true;
		}

		public static void FixedHeightInput(string value)
        {
			if (int.TryParse(value, out int validHeight) && validHeight >= 0 && validHeight < 256)
			{
				BuildHelperMain.height = validHeight;
			}
			else
            {
				refInputField.text = BuildHelperMain.height.ToString();
            }
        }
		public static void Apply()
		{
			myModSettings.GetValueKeyCode("Add", "Input", out BuildHelperMain.AddVoxelHeightKey);
			myModSettings.GetValueKeyCode("Add blocks", "Input", out BuildHelperMain.AddVoxelsKey);
			myModSettings.GetValueKeyCode("Remove blocks", "Input", out BuildHelperMain.RemoveVoxelsKey);
			myModSettings.GetValueKeyCode("Remove line blocks", "Input", out BuildHelperMain.RemoveVoxelsRayKey);
			myModSettings.GetValueKeyCode("Paint", "Input", out BuildHelperMain.PaintVoxelsKey);
		}
	}
}
