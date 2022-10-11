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
		public static DZSlider RadiusSlider;
		public static SelectionButton refRemoveMode;

		public static bool isInitialized;

		public static int maxSpeed;
		public static int maxRadius;

		public static void Initialize(MelonMod thisMod)
		{

			myModSettings = UIManager.Register(thisMod, new Color32(243, 227, 182, 255));

			myModSettings.AddToggle("Fixed Height", "General", new Color32(243, 227, 182, 255), BuildHelperMain.fixedHeight, new Action<bool>(delegate (bool value) { FixedHeightToggle(value); }));
			/*myModSettings.AddInputField("Height", "General", new Color32(255, 179, 174, 255), TMP_InputField.ContentType.Alphanumeric, "0", new Action<string>(delegate (string value) { FixedHeightInput(value); }));
			refInputField = myModSettings.controlInputFields["Height"].GetComponent<UnityEngine.UI.InputField>();*/

			// Mod Setting management for keyboard shortcuts
			Apply();

			//Speed and Radius management
			maxSpeed = myModSettings.GetValueInt("MaxSpeed", "General", out maxSpeed) ? maxSpeed : BuildHelperMain.maxSpeed;
			maxRadius = myModSettings.GetValueInt("MaxRadius", "General", out maxRadius) ? maxRadius : BuildHelperMain.maxRadius;

			//Keyboard shortcuts button creation
			myModSettings.AddKeybind("Single", "Add", BuildHelperMain.AddVoxelHeightKey, new Color32(10, 190, 124, 255));
			myModSettings.AddKeybind("Many blocks", "Add", BuildHelperMain.AddVoxelsKey, new Color32(10, 190, 124, 255));
			myModSettings.AddSlider("Radius", "General", new Color32(119, 206, 224, 255), 1, maxRadius, true, BuildHelperMain.radius, new Action<float>(delegate (float value) { UpdateRadius(value); }));
			RadiusSlider = myModSettings.controlSliders["Radius"].GetComponent<DZSlider>();

			//Speed control for Add/Remove
			myModSettings.AddSlider("Speed", "General", new Color32(119, 206, 224, 255), 3, maxSpeed, true, maxSpeed - BuildHelperMain.speed, new Action<float>(delegate (float value) { BuildHelperMain.speed = maxSpeed - (int)value + 1; }));
			refRemoveMode = myModSettings.AddSelectionButton
				("SelectMode",
				"Remove",
				new Color32(119, 206, 224, 255),
				new Action(delegate { UpdateMode(true); }),
				new Action(delegate { UpdateMode(false); }),
				BuildHelperMain.modes[BuildHelperMain.mode]);

			myModSettings.GetValueString("SelectMode", "Remove", out string mode);
			BuildHelperMain.mode = Array.IndexOf(BuildHelperMain.modes, mode);

			myModSettings.AddKeybind("Custom", "Remove", BuildHelperMain.RemoveVoxelsRayKey, new Color32(10, 190, 124, 255));
			myModSettings.AddKeybind("Many delete", "Remove", BuildHelperMain.RemoveVoxelsKey, new Color32(10, 190, 124, 255));
			myModSettings.AddKeybind("Paint", "Paint", BuildHelperMain.PaintVoxelsKey, new Color32(10, 190, 124, 255));

			//Apply button
			myModSettings.AddButton("Apply Settings", "General", new Color32(243, 227, 182, 255), new Action(delegate { Apply(); }));

			UpdateRadius(BuildHelperMain.radius);

			isInitialized = true;

		}

		public static void UpdateRadius(float value)
        {
			BuildHelperMain.radius = (int)value;

			RadiusSlider.textField.text = "Radius : " + value.ToString();
		}

		public static void UpdateMode(bool prev)
        {
			if (prev)
            {
				BuildHelperMain.mode = BuildHelperMain.mode == 0 ? 2 : BuildHelperMain.mode - 1;
			}
			else
            {
				BuildHelperMain.mode = BuildHelperMain.mode == 2 ? 0 : BuildHelperMain.mode + 1;
			}
			refRemoveMode.selectValue = BuildHelperMain.modes[BuildHelperMain.mode];
		}

		public static void FixedHeightToggle(bool value)
        {
			BuildHelperMain.fixedHeight = value;
			BuildHelperMain.height = 0;
			BuildHelperMain.ResetSphere();
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
