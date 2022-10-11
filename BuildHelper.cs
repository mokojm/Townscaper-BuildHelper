using MelonLoader;
using Placemaker;
using Placemaker.Graphs;
using Placemaker.Quads;
using Placemaker.Quads.GridGeneration;
using System;
using System.Collections;
using System.Collections.Generic;
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
		public static int radius = 3;
		public static int maxRadius = 8;

		public static int height = 0;
		public static bool fixedHeight = false;

		public static int maxSpeed = 100;

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

		//Add/Remove speed
		public static int speed = 15;
		public static bool speedLock = false;

		//Remove mode
		public static string[] modes = { "Single", "Corner", "Ray" };
		public static int mode = 0;

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

		//Search a voxel given its position and height
		public static Voxel SearchVoxel(int2 hexPos, int dstHeight)
		{
			if (graph.cornerDict.ContainsKey(hexPos))
			{
				Corner corner = graph.cornerDict.get_Item(hexPos);
				//MelonLogger.Msg("Squares : " + corner.squares.Count.ToString());
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

		//Remove all voxels from a corner
		public static void RemoveCorner()
        {
			if (graph.cornerDict.ContainsKey(hover.srcHexPos))
			{
				Corner corner = graph.cornerDict.get_Item(hover.srcHexPos);

				if (corner == null)
				{
					return;
				}
                FlowData[] flowDatas = corner.flowDatas.ToArray();
				foreach (FlowData item in flowDatas)
				{
					if (item.voxel != null)
					{
						maker.BeginNewAction();
						maker.AddAction(hover.srcHexPos, item.voxel.height, item.voxel.type, VoxelType.Empty);
						graph.RemoveVoxel(item.voxel);
						maker.EndAction();
					}
				}
			}
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

		//Remove a single voxel from the grid, can remove at a specific height
		public static void RemoveSingle()
        {
			if (!mainCam.isActiveAndEnabled)
			{
				return;
			}

			//Speed handling
			if (Time.frameCount % speed != 0 && speedLock)
			{
				return;
			}

			int targetHeight = !fixedHeight ? hover.srcHeight : height;
			Voxel thisVoxel = SearchVoxel(hover.srcHexPos, targetHeight);

			if (thisVoxel != null && thisVoxel.type != VoxelType.Empty && thisVoxel.type != VoxelType.Water && thisVoxel.type != VoxelType.Any)
			{
				maker.BeginNewAction();
				maker.AddAction(hover.srcHexPos, (byte)targetHeight, thisVoxel.type, VoxelType.Empty);
				graph.RemoveVoxel(thisVoxel);
				maker.EndAction();
				Vert thisVert = hover.master.grid.GetVertOrIterate(hover.srcHexPos, null);
				clickEffect.Click(true, thisVert.planePos, targetHeight, voxelColor);
			}
		}

		//Build at a specific height, can build at no specific height too
		public static void AddVoxelHeight()
        {
			if (!mainCam.isActiveAndEnabled)
            {
				return;
            }

			//Speed handling
			if (Time.frameCount % speed != 0 && speedLock)
            {
				return;
            }

			hover.dstHeight = !fixedHeight ? hover.dstHeight : height;
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

		//Remove voxels according to radius at the height of the cursor
		public static IEnumerator RemoveVoxel3s()
        {
			int dstHeight = !fixedHeight ? hover.srcHeight : height;
			dstHeight = dstHeight < 0 ? 0 : dstHeight;

			//Initialize queue
			Queue<Corner> corners = new Queue<Corner>();
			HashSet<int2> checkedCorners = new HashSet<int2>();

			
			if (hover.voxel != null && hover.voxel.type != VoxelType.Empty && hover.voxel.type != VoxelType.Any && hover.voxel.type != VoxelType.Water)
			{
				maker.BeginNewAction();
				maker.AddAction(hover.srcHexPos, (byte)dstHeight, hover.voxel.type, VoxelType.Empty);
				graph.RemoveVoxel(hover.voxel);
				maker.EndAction();
				clickEffect.Click(hover, false, hover.voxel);
			}

			int2 firstHexPos = hover.srcHexPos;
			yield return new WaitForEndOfFrame();

			Corner corner;

			try
            {
				corner = graph.cornerDict.get_Item(firstHexPos);

			}
            catch (Exception)
            {

                yield break;
            }

			Voxel thisVox;
			int count;
			Square[] squares;
			corners.Enqueue(corner);
			checkedCorners.Add(corner.hexPos);

			for (int i = 0; i < radius; i++)
			{
				count = corners.Count;
				for (int j = 0; j < count; j++)
				{
					corner = corners.Dequeue();
					squares = corner.squares.ToArray();

					foreach (Square square in squares)
					{
						HashSet<Corner> thisSquareCorners = new HashSet<Corner>() { square.GetCorner(0), square.GetCorner(1), square.GetCorner(2), square.GetCorner(3) };
						foreach (Corner c in thisSquareCorners)
						{
							//Emergency stop
							if (Input.GetKeyDown(AddVoxelsKey) || Input.GetKeyDown(KeyCode.Escape))
                            {
								yield break;
                            }

							if (c != null && checkedCorners.Contains(c.hexPos) == false)
							{
								thisVox = SearchVoxel(c.hexPos, dstHeight);
								if (thisVox != null)
								{
									maker.BeginNewAction();
									maker.AddAction(c.hexPos, (byte)dstHeight, thisVox.type, VoxelType.Empty);
									graph.RemoveVoxel(thisVox);
									maker.EndAction();
									yield return new WaitForSeconds(.02f);
								}

								if (i + 1 < radius)
								{
									corners.Enqueue(c);
								}

								checkedCorners.Add(c.hexPos);
							}
						}
					}
				}

				yield return new WaitForEndOfFrame();
			}
			corners.Clear();
			checkedCorners.Clear();
		}

		//Add voxels coording to radius at the height of the cursor or at a specific height
		public static IEnumerator AddVoxel3s()
        {

			hover.dstHeight = !fixedHeight ? hover.dstHeight : height;
			int iHeight = hover.dstHeight;

			//Initialize queue
			Queue<Corner> corners = new Queue<Corner>();
			HashSet<int2> checkedCorners = new HashSet<int2>();

			Voxel thisVoxel = SearchVoxel(hover.dstHexPos, hover.dstHeight);
			if (thisVoxel != null && thisVoxel.type != VoxelType.Empty && thisVoxel.type != VoxelType.Water && thisVoxel.type != VoxelType.Any)
			{
			}
			else if (hover.validAdd)
			{
				Voxel voxel = maker.AddClick(hover, voxelColor);
				hover.dstHeight = height;
				clickEffect.Click(hover, true, voxel);
			}

			int2 firstHexPos = hover.dstHexPos;
			yield return new WaitForEndOfFrame();

			Corner corner = graph.cornerDict.get_Item(firstHexPos);
			int count = corners.Count;
			corners.Enqueue(corner);
			checkedCorners.Add(corner.hexPos);

            for (int i = 0; i < radius; i++)
            {
				count = corners.Count;
				for (int j = 0; j < count; j++)
                {
					corner = corners.Dequeue();

					foreach (Square square in corner.squares)
					{
						HashSet<Corner> thisSquareCorners = new HashSet<Corner>() { square.GetCorner(0), square.GetCorner(1), square.GetCorner(2), square.GetCorner(3) };
						foreach (Corner c in thisSquareCorners)
						{
							//Emergency stop
							if (Input.GetKeyDown(RemoveVoxelsKey) || Input.GetKeyDown(KeyCode.Escape))
							{
								yield break;
							}

							if (c != null && checkedCorners.Contains(c.hexPos) == false)
							{
								if (SearchVoxel(c.hexPos, iHeight) == null)
								{
									maker.BeginNewAction();
									graph.AddVoxel(c.hexPos, (byte)iHeight, voxelColor, true);
									maker.AddAction(c.hexPos, (byte)iHeight, VoxelType.Empty, voxelColor);
									maker.EndAction();
									yield return new WaitForSeconds(.02f);
								}

								if (i + 1 < radius)
								{
									corners.Enqueue(c);
								}

								checkedCorners.Add(c.hexPos);
							}
						}
					}
				}

				yield return new WaitForEndOfFrame();
			}
			corners.Clear();
			checkedCorners.Clear();
		}

		//DEPRECATED : Add several voxels
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
		

		//DEPRECATED : Add several voxels
		public static void AddVoxel2s()
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


		//DEPRECATED : Remove several voxels
		public static void RemoveVoxel2s()
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

		//Coroutines starters
		public static void AddVoxels()
        {
			if (!mainCam.isActiveAndEnabled)
			{
				return;
			}
			else
            {
				MelonCoroutines.Start(AddVoxel3s());
			}

		}

		public static void RemoveVoxels()
        {
			if (!mainCam.isActiveAndEnabled)
			{
				return;
			}
			else
			{
				MelonCoroutines.Start(RemoveVoxel3s());
			}
		}

		public static void StartPaintVoxels()
        {
			if (painterLocked == false)
            {
				MelonCoroutines.Start(PaintVoxels());
			}
        }

		//Segregate remove selected option
		public static void RemoveHandler()
        {
			if (mode == 2)
            {
				RemoveVoxelsRayKeyB = false;
				RemoveVoxelsRay();
			}
			else if (mode == 1)
            {
				RemoveVoxelsRayKeyB = false;
				RemoveCorner();
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
					speedLock = false;
				}

				if (addVoxHPressed && Time.time - thisTime > 0.2)
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

				//Remove SIngle
				if (Input.GetKey(RemoveVoxelsRayKey) && mode == 0)
                {
					RemoveSingle();
					speedLock = true;
                }
				if (Input.GetKeyUp(RemoveVoxelsRayKey) && mode == 0)
                {
					speedLock = false;
                }

					//Paint
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

