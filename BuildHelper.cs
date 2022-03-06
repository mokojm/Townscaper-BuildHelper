using MelonLoader;
using Placemaker;
using Placemaker.Graphs;
using Placemaker.Quads;
using Placemaker.Quads.GridGeneration;
using System.Collections;
using System.Threading;
using Unity.Mathematics;
using UnityEngine;

namespace BuildHelper
{
    public class BuildHelperMain : MelonMod
    {

		//Objects
		public static HoverData hover;
		public static Graph graph;
		public static Maker maker;
		public static ClickEffect clickEffect;
		public static VoxelType voxelColor;

		public static bool isInitialized = false;

		//UI
		public static int height = 0;
		public static bool fixedHeight = false;

		//Keyboard
		public static KeyCode AddVoxelsKey = KeyCode.F;
		public static KeyCode RemoveVoxelsKey = KeyCode.G;
		public static KeyCode AddVoxelHeightKey = KeyCode.Space;
		public static KeyCode RemoveVoxelsRayKey = KeyCode.H;
		public static KeyCode PaintVoxelsKey = KeyCode.J;

		public static bool AddVoxelsKeyB = false;
		public static bool RemoveVoxelsKeyB = false;
		public static bool AddVoxelHeightKeyB = false;
		public static bool RemoveVoxelsRayKeyB = false;
		public static bool PaintVoxelsKeyB = false;

		//PaintVoxel check to make sure one voxel is painted at a time
		public static bool painterLocked = false;

		//Fixed height
		public static GameObject sphere;

		//Time management for AddVoxelHeight
		public static float thisTime;
		public static bool addVoxHPressed = false;

		//Time management for Sphere
		public static float sphereTime;

		//MainCamera check for identifying whether FPS mod is "ON" or not
		public static Camera mainCam;


		//Methods

		public static void Initialize(HoverData thisHover)
        {
			if (isInitialized == false)
            {
				hover = thisHover;
				graph = hover.master.graph;
				maker = hover.master.maker;
				clickEffect = hover.master.clickEffect;
				voxelColor = (VoxelType)hover.master.uiMaster.paletteMenu.selectedPickerIndex;

				//Initialize sphere
				sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
				sphere.transform.position = new Vector3(0, 0, 0);
				sphere.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
				sphere.GetComponent<MeshRenderer>().material = GameObject.Find("HoverHightlight").GetComponent<MeshRenderer>().material;
				sphere.GetComponent<MeshRenderer>().material.color = new Color(1, 1, 1, 0.5f);
				sphere.SetActive(fixedHeight);

				//Initialize mainCamera
				mainCam = GameObject.FindWithTag("MainCamera").GetComponent<Camera>();

				isInitialized = true;
            }
        }



		public static Voxel SearchVoxel(int2 hexPos, int dstHeight)
		{
			if (graph.cornerDict.ContainsKey(hexPos))
			{
				Corner corner = graph.cornerDict.get_Item(hexPos);
				if (corner == null)
				{
					return null;
				}
				foreach (FlowData item in corner.flowDatas)
				{
					if (item.voxel != null && (int)item.voxel.height == dstHeight)
					{
						return item.voxel;
					}
				}
			}
			return null;
		}

		//Remove all voxels on the Raycast
		public static void RemoveVoxelsRay()
        {
			Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
			RaycastHit[] hits = Physics.RaycastAll(ray, 1E+07f);

			if (hits.Length == 0)
            {
				return;
            }

			Voxel hitVoxel;

			foreach (RaycastHit hit in hits)
            {
				hitVoxel = hit.collider.GetComponent<Voxel>();
				if (hitVoxel != null && hitVoxel.type != VoxelType.Empty && hitVoxel.type != VoxelType.Any && hitVoxel.type != VoxelType.Water)
                {
					int2 hexPos = hitVoxel.GetComponentInParent<Corner>().hexPos;
					maker.BeginNewAction();
					maker.AddAction(hexPos, hitVoxel.height, hitVoxel.type, VoxelType.Empty);
					graph.RemoveVoxel(hitVoxel);
					maker.EndAction();
				}
            }
        }

		//Build at a specific height
		public static void AddVoxelHeight()
        {
			if (fixedHeight == false || !mainCam.isActiveAndEnabled)
            {
				return;
            }

			hover.dstHeight = height;
			Voxel thisVoxel = SearchVoxel(hover.dstHexPos, hover.dstHeight);
			if (thisVoxel != null && thisVoxel.type != VoxelType.Empty && thisVoxel.type != VoxelType.Water && thisVoxel.type != VoxelType.Any)
			{
				return;
			}

			if (hover.validAdd)
			{
				Voxel voxel = maker.AddClick(hover, voxelColor);
				hover.dstHeight = height;
				clickEffect.Click(hover, true, voxel);
			}
		}

		//Add several voxels
		public static IEnumerator AddVoxelsSlow()
        {
			int dstHeight = fixedHeight ? height : hover.dstHeight;
			//dstHeight = dstHeight == 999 ? dstHeight = hover.dstHeight : dstHeight;

			foreach (HexPatch hexPatch in hover.master.grid.patches)
			{
				if (hexPatch.verts.Contains(hover.dstHexPos))
				{
					foreach (int2 hexPos in hexPatch.verts)
					{
						if (SearchVoxel(hexPos, (byte)dstHeight) == null)
						{
							maker.BeginNewAction();
							graph.AddVoxel(hexPos, (byte)dstHeight, voxelColor, true);
							maker.AddAction(hexPos, (byte)dstHeight, VoxelType.Empty, voxelColor);
							maker.EndAction();

							Vert thisVert = hover.master.grid.GetVertOrIterate(hexPos, null);
							clickEffect.Click(true, thisVert.planePos, dstHeight, voxelColor);
							yield return new WaitForSeconds(.01f);
						}
					}
					break;
				}
			}
		}

		//Add several voxels
		public static void AddVoxels()
		{
			int dstHeight = fixedHeight ? height : hover.dstHeight;
			//dstHeight = dstHeight == 999 ? dstHeight = hover.dstHeight : dstHeight;

			foreach (HexPatch hexPatch in hover.master.grid.patches)
			{
				if (hexPatch.verts.Contains(hover.dstHexPos))
				{
					foreach (int2 hexPos in hexPatch.verts)
					{
						if (SearchVoxel(hexPos, (byte)dstHeight) == null && graph.IsCoordinateAllowed(hexPos, dstHeight))
						{
							maker.BeginNewAction();
							graph.AddVoxel(hexPos, (byte)dstHeight, voxelColor, true);
							maker.AddAction(hexPos, (byte)dstHeight, VoxelType.Empty, voxelColor);
							maker.EndAction();
						}
					}
					break;
				}
			}
		}

		public static void StartAddVoxels()
		{
			MelonCoroutines.Start(AddVoxelsSlow());
		}

		//Remove several voxels
		public static void RemoveVoxels()
        {
			int dstHeight = hover.srcHeight;
			dstHeight = dstHeight < 0 ? 0 : dstHeight;
			//int dstHeight = height == 999 ? hover.dstHeight - 1 : height - 1;
			
			int2 dstHexPos = hover.dstHexPos;
			foreach (HexPatch hexPatch in hover.master.grid.patches)
			{
				if (hexPatch.verts.Contains(dstHexPos))
				{
					foreach (int2 hexPos in hexPatch.verts)
					{
						if (graph.cornerDict.ContainsKey(hexPos))
						{
							Corner corner = graph.cornerDict.get_Item(hexPos);

							foreach (FlowData item in corner.flowDatas)
							{
								if (item.voxel != null && (int)item.voxel.height == dstHeight && item.voxel.type != VoxelType.Empty && item.voxel.type != VoxelType.Any && item.voxel.type != VoxelType.Water)
								{
									maker.BeginNewAction();
									maker.AddAction(hexPos, (byte)dstHeight, item.voxel.type, VoxelType.Empty);
									graph.RemoveVoxel(item.voxel);
									maker.EndAction();
									break;
								}
							}
						}
					}
					break;
				}
			}
		}

		//Paint voxels easily
		public static IEnumerator PaintVoxels()
        {
			if (hover.validPaint)
			{
				if (hover.voxel != null && hover.voxel.type != voxelColor && hover.voxel.type != VoxelType.Ground && hover.voxel.type != VoxelType.Empty && hover.voxel.type != VoxelType.Any && hover.voxel.type != VoxelType.Water)
				{
					Voxel voxel = hover.voxel;
					int2 hexPos = hover.srcHexPos;
					byte dstHeight = (byte)hover.srcHeight;
					VoxelType srcVoxelColor = hover.voxel.type;

					maker.BeginNewAction();
					maker.AddAction(hexPos, (byte)dstHeight, srcVoxelColor, VoxelType.Empty);
					graph.RemoveVoxel(voxel);
					maker.EndAction();
					painterLocked = true;
					yield return new WaitForSeconds(.1f);

					maker.BeginNewAction();
					graph.AddVoxel(hexPos, (byte)dstHeight, voxelColor, true);
					maker.AddAction(hexPos, (byte)dstHeight, VoxelType.Empty, voxelColor);
					maker.EndAction();
					painterLocked = false;

					clickEffect.Click(hover, true, voxel);
				}
			}
		}

		public static void StartPaintVoxels()
        {
			if (painterLocked == false)
            {
				MelonCoroutines.Start(PaintVoxels());
			}
        }


		public override void OnSceneWasLoaded(int buildIndex, string sceneName)
		{
			if (sceneName == "Placemaker")
			{
				MelonLogger.Msg("Main scene loaded");

				// Initializing
				//Initialize();
				HelperUI.Initialize(this);

			}
		}

		public static void ResetSphere()
        {
			sphere.transform.position = hover.pointerHitPos;
		}

		public static void UpdateSphere(float upDown = 0)
        {
			/*float x = hover.srcVert.hexPos.x;
			float z = hover.srcVert.hexPos.y;
			float y = sphere.transform.position.y;*/

			float x = hover.pointerHitPos.x;
			float z = hover.pointerHitPos.z;
			float y = sphere.transform.position.y;

			if (upDown == 0)
            {
				sphere.transform.position = new Vector3(x, y, z);
				//sphere.transform.position = new Vector3(hover.pointerHitPos.x, sphere.transform.position.y, hover.pointerHitPos.z);
			}
			else if (upDown == 1)
            {
				sphere.transform.position = new Vector3(x, y+1, z);
				//sphere.transform.position = new Vector3(hover.pointerHitPos.x, sphere.transform.position.y + 1, hover.pointerHitPos.z);
			}
			else if (sphere.transform.position.y > 1)
            {
				sphere.transform.position = new Vector3(x, y-1, z);
				//sphere.transform.position = new Vector3(hover.pointerHitPos.x, sphere.transform.position.y - 1, hover.pointerHitPos.z);
			}
		}

		public static void Reset()
		{
		}

		public override void OnApplicationStart()
		{
			
		}

		public override void OnUpdate()
		{
			if (Application.isFocused && isInitialized)
            {
				voxelColor = (VoxelType)hover.master.uiMaster.paletteMenu.selectedPickerIndex;

				if (Input.GetKeyDown(AddVoxelsKey))
				{
					AddVoxelsKeyB = true;
				}
				else
				{
					AddVoxelsKeyB = false;
				}

				if (Input.GetKeyDown(RemoveVoxelsKey))
				{
					RemoveVoxelsKeyB = true;
				}
				else
				{
					RemoveVoxelsKeyB = false;
				}

				//Add voxel height input management
				if (Input.GetKeyDown(AddVoxelHeightKey))
				{
					thisTime = Time.time;
					AddVoxelHeightKeyB = true;
					addVoxHPressed = true;
					sphere.SetActive(false);
				}
				else if (Input.GetKeyUp(AddVoxelHeightKey))
				{
					AddVoxelHeightKeyB = false;
					addVoxHPressed = false;
				}

				if (addVoxHPressed && Time.time - thisTime > 1)
				{
					AddVoxelHeightKeyB = true;
				}


				//Others
				if (Input.GetKeyDown(RemoveVoxelsRayKey))
				{
					RemoveVoxelsRayKeyB = true;
				}
				else
				{
					RemoveVoxelsRayKeyB = false;
				}

				if (Input.GetKey(PaintVoxelsKey))
				{
					PaintVoxelsKeyB = true;
				}
				else if (Input.GetKeyUp(PaintVoxelsKey))
				{
					PaintVoxelsKeyB = false;
				}

				//Sphere update
				if (Input.GetKeyDown(KeyCode.LeftAlt))
                {
					UpdateSphere(1);
					height = height < 255 ? height + 1 : height;
					sphere.SetActive(fixedHeight);
					sphereTime = Time.time;
                }
				else if (Input.GetKeyDown(KeyCode.LeftControl))
                {
					UpdateSphere(-1);
					height = height > 0 ? height - 1 : height;
					sphere.SetActive(fixedHeight);
					sphereTime = Time.time;
				}
				else if (Time.time - sphereTime > 10)
                {
					sphere.SetActive(false);
				}
				else
                {
					UpdateSphere();
                }
			}
		}
	}
}

