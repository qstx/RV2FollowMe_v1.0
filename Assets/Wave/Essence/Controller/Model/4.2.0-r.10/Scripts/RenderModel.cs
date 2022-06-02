// "Wave SDK
// © 2020 HTC Corporation. All Rights Reserved.
//
// Unless otherwise required by copyright law and practice,
// upon the execution of HTC SDK license agreement,
// HTC grants you access to and use of the Wave SDK(s).
// You shall fully comply with all of HTC’s SDK license agreement terms and
// conditions signed by you and all SDK and API requirements,
// specifications, and documentation provided by HTC to You."

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Wave.Native;
using System.Runtime.InteropServices;
using UnityEngine.XR;
using Wave.Essence.Extra;
using UnityEditor;
using UnityEngine.Profiling;

namespace Wave.Essence.Controller.Model
{
	public class RenderModel : MonoBehaviour
	{
		private static string LOG_TAG = "RenderModel";
		private void PrintDebugLog(string msg)
		{
			Log.d(LOG_TAG, "Hand: " + WhichHand + ", " + msg);
		}

		private void PrintInfoLog(string msg)
		{
			Log.i(LOG_TAG, "Hand: " + WhichHand + ", " + msg);
		}

		private void PrintWarningLog(string msg)
		{
			Log.w(LOG_TAG, "Hand: " + WhichHand + ", " + msg);
		}

		public enum LoadingState
		{
			LoadingState_NOT_LOADED,
			LoadingState_LOADING,
			LoadingState_LOADED
		}

		public XR_Hand WhichHand = XR_Hand.Dominant;
		public GameObject defaultModel = null;
		public bool updateDynamically = true;
		public bool mergeToOneBone = false;

		public delegate void RenderModelReadyDelegate(XR_Hand hand);
		public static event RenderModelReadyDelegate onRenderModelReady = null;

		private GameObject controllerSpawned = null;
		private XRNode node;

		private bool connected = false;
		private string renderModelNamePath = "";
		private string renderModelName = "";

		private List<Color32> colors = new List<Color32>();
		private GameObject meshCom = null;
		private GameObject meshGO = null;
		private GameObject[] childArray;
		private bool[] showState;
		private Mesh updateMesh;
		private Material modelMat;
		private Material ImgMaterial;
		private WaitForEndOfFrame wfef = null;
		private WaitForSeconds wfs = null;
		private bool showBatterIndicator = true;
		private bool isBatteryIndicatorReady = false;
		private BatteryIndicator currentBattery;
		private GameObject batteryGO = null;
		private MeshRenderer batteryMR = null;

		private ModelResource modelResource = null;
		private LoadingState mLoadingState = LoadingState.LoadingState_NOT_LOADED;

		[HideInInspector]
		public bool checkInteractionMode = false;
		private bool showModel = true;
		private bool preShowModel = true;

		private bool EnableDirectPreview = false;

		private InputDevice inputDevice;

		void OnEnable()
		{
			PrintDebugLog("OnEnable");
#if UNITY_EDITOR
			EnableDirectPreview = EditorPrefs.GetBool("Wave/DirectPreview/EnableDirectPreview", false);
			PrintDebugLog("OnEnterPlayModeMethod: " + EnableDirectPreview);
#endif
			if (mLoadingState == LoadingState.LoadingState_LOADING)
			{
				deleteChild("RenderModel doesn't expect model is in loading, delete all children");
			}

			if (WhichHand == XR_Hand.Dominant)
			{
				node = XRNode.RightHand;
			}
			else
			{
				node = XRNode.LeftHand;
			}

			connected = checkConnection();

			if (connected)
			{
				WVR_DeviceType type = checkDeviceType();

				if (mLoadingState == LoadingState.LoadingState_LOADED)
				{
					if (isRenderModelNameSameAsPrevious())
					{
						PrintDebugLog("OnEnable - Controller connected, model was loaded!");
					}
					else
					{
						deleteChild("Controller load when OnEnable, render model is different!");
						onLoadController(type);
					}
				}
				else
				{
					PrintDebugLog("Controller load when OnEnable!");
					onLoadController(type);
				}
			}

			OEMConfig.onOEMConfigChanged += onOEMConfigChanged;
		}

		void OnDisable()
		{
			PrintDebugLog("OnDisable");
			if (mLoadingState == LoadingState.LoadingState_LOADING)
			{
				deleteChild("RenderModel doesn't complete creating meshes before OnDisable, delete all children");
			}

			OEMConfig.onOEMConfigChanged -= onOEMConfigChanged;
		}

		void OnDestroy()
		{
			PrintDebugLog("OnDestroy");
		}

		private void onOEMConfigChanged()
		{
			PrintDebugLog("onOEMConfigChanged");
			ReadJsonValues();
		}

		private void ReadJsonValues()
		{
#if UNITY_EDITOR
			if (EnableDirectPreview)
				showBatterIndicator = true;
#else
			showBatterIndicator = false;
#endif
			JSON_BatteryPolicy batteryP = OEMConfig.getBatteryPolicy();

			if (batteryP != null)
			{
				if (batteryP.show == 2)
					showBatterIndicator = true;
			} else
			{
				PrintDebugLog("There is no system policy!");
			}

			PrintDebugLog("showBatterIndicator: " + showBatterIndicator);
		}

		private bool isRenderModelNameSameAsPrevious()
		{
			bool _connected = checkConnection();
			bool _same = false;

			if (!_connected)
				return _same;

			WVR_DeviceType type = checkDeviceType();

			string tmprenderModelName = ClientInterface.GetCurrentRenderModelName(type);

			PrintDebugLog("previous render model: " + renderModelName + ", current render model name: " + tmprenderModelName);

			if (tmprenderModelName == renderModelName)
			{
				_same = true;
			}

			return _same;
		}

		void OnApplicationPause(bool pauseStatus)
		{
			if (pauseStatus) // pause
			{
				PrintInfoLog("Pause(" + pauseStatus + ") and check loading");
				if (mLoadingState == LoadingState.LoadingState_LOADING)
				{
					deleteChild("Destroy controller prefeb because of spawn process is not completed and app is going to pause.");
				}
			} else
			{
				PrintDebugLog("Resume");
			}
		}

		// Use this for initialization
		void Start()
		{
			PrintDebugLog("start() connect: " + connected + " Which hand: " + WhichHand);
			wfs = new WaitForSeconds(1.0f);
			ReadJsonValues();

			if (updateDynamically)
			{
				PrintDebugLog("updateDynamically, start a coroutine to check connection and render model name periodly");
				StartCoroutine(checkRenderModelAndDelete());
			}

			if (this.transform.parent != null)
			{
				PrintDebugLog("start() parent is " + this.transform.parent.name);
			}
		}

		int t = 0;

		bool checkShowModel()
		{
			if (Interop.WVR_IsInputFocusCapturedBySystem() || !inputDevice.isValid)
				return false;

			if (checkInteractionMode && (ClientInterface.InteractionMode != XR_InteractionMode.Controller))
				return false;

			inputDevice.TryGetFeatureValue(CommonUsages.isTracked, out bool validPose);
			
			return validPose;
		}

		// Update is called once per frame
		void Update()
		{
			if (mLoadingState == LoadingState.LoadingState_NOT_LOADED)
			{
				InputDevice device = InputDevices.GetDeviceAtXRNode(node);

				if (device.isValid && device.TryGetFeatureValue(CommonUsages.isTracked, out bool validPoseState)
					&& validPoseState)
				{
					WVR_DeviceType type = checkDeviceType();

					this.connected = true;
					PrintDebugLog("spawn render model");
					inputDevice = device;
					onLoadController(type);
				}
			}

			if (mLoadingState == LoadingState.LoadingState_LOADED)
			{
				showModel = checkShowModel();

				if (showModel != preShowModel)
				{
					Profiler.BeginSample("ShowHide");
					PrintDebugLog("show model change");
					preShowModel = showModel;

					if (showModel)
					{
						PrintDebugLog("Show render model to previous state");
						if (childArray != null)
						{
							for (int i = 0; i < childArray.Length; i++)
							{
								childArray[i].SetActive(showState[i]);
							}
							Profiler.BeginSample("UpdateBatteryLevel");
							updateBatteryLevel();
							Profiler.EndSample();
						}
					}
					else
					{
						PrintDebugLog("Save render model state and force show to false");

						if (childArray != null)
						{
							for (int i = 0; i < childArray.Length; i++)
							{
								showState[i] = childArray[i].activeInHierarchy;
								childArray[i].SetActive(false);
							}
						}
					}
					Profiler.EndSample();
				}

				if (showModel && (t-- < 0))
				{
					Profiler.BeginSample("UpdateBatteryLevel");
					updateBatteryLevel();
					Profiler.EndSample();
					t = 200;
				}
			}

			if (Log.gpl.Print)
			{
				Log.d(LOG_TAG, "Update() hand " + WhichHand + " connect? " + this.connected + ", child? " + transform.childCount + ", showBattery? " + showBatterIndicator + ", hasBattery? " + isBatteryIndicatorReady + ", ShowModel? " + showModel + ", state? " + mLoadingState);
				Log.d(LOG_TAG, "Update() hand " + WhichHand + " position? x = " + this.transform.position.x + ", y = " + this.transform.position.y + ", z = " + this.transform.position.z + ", active = " + this.gameObject.activeInHierarchy );

				if (showModel)
				{
					if (childArray != null)
					{
						for (int i = 0; i < childArray.Length; i++)
						{
							if (childArray[i] != null)
							{
								if (childArray[i].name.Equals("__CM__Body"))
									Log.d(LOG_TAG, "Update() render model " + WhichHand + ", name = " + childArray[i].name + ", active = " + childArray[i].activeInHierarchy + " position ? x = " + childArray[i].transform.position.x + ", y = " + childArray[i].transform.position.y + ", z = " + childArray[i].transform.position.z);
							}
						}
					}
				}
			}
		}

		public void applyChange()
		{
			deleteChild("Setting is changed.");
			WVR_DeviceType type = checkDeviceType();
			onLoadController(type);
		}

		private void onLoadController(WVR_DeviceType type)
		{
			mLoadingState = LoadingState.LoadingState_LOADING;
			PrintDebugLog("Pos: " + this.transform.localPosition.x + " " + this.transform.localPosition.y + " " + this.transform.localPosition.z);
			PrintDebugLog("Rot: " + this.transform.localEulerAngles);
			PrintDebugLog("MergeToOneBone: " + mergeToOneBone);
			PrintDebugLog("type: " + type);

			if (Interop.WVR_GetWaveRuntimeVersion() < 2 && !EnableDirectPreview)
			{
				PrintDebugLog("onLoadController in old service");
				if (defaultModel != null)
				{
					controllerSpawned = Instantiate(defaultModel, this.transform);
					controllerSpawned.transform.parent = this.transform;
				}
				else
				{
					PrintDebugLog("Can't load controller model from DS, default model is null and load WaveFinchController");
					var prefab = Resources.Load("DefaultController/WaveFinchController") as GameObject;
					controllerSpawned = Instantiate(prefab, this.transform);
					controllerSpawned.transform.parent = this.transform;
					mLoadingState = LoadingState.LoadingState_LOADED;
				}
				mLoadingState = LoadingState.LoadingState_LOADED;
				return;
			}

			renderModelName = ClientInterface.GetCurrentRenderModelName(type);

			if (renderModelName.Equals(""))
			{
				PrintDebugLog("Can not find render model.");
				if (defaultModel != null)
				{
					PrintDebugLog("Can't load controller model from DS, load default model");
					controllerSpawned = Instantiate(defaultModel, this.transform);
					controllerSpawned.transform.parent = this.transform;
					mLoadingState = LoadingState.LoadingState_LOADED;
				}
				else
				{
					PrintDebugLog("Can't load controller model from DS, default model is null and load WaveFinchController");
					var prefab = Resources.Load("DefaultController/WaveFinchController") as GameObject;
					controllerSpawned = Instantiate(prefab, this.transform);
					controllerSpawned.transform.parent = this.transform;
					mLoadingState = LoadingState.LoadingState_LOADED;
				}
				return;
			}

			if (controllerSpawned != null)
			{
				Destroy(controllerSpawned);
			}

			bool retModel = false;
			modelResource = null;

			retModel = ResourceHolder.Instance.addRenderModel(renderModelName, WhichHand, mergeToOneBone);
			if (retModel)
			{
				PrintDebugLog("Add " + renderModelName + " model sucessfully!");
			}

			modelResource = ResourceHolder.Instance.getRenderModelResource(renderModelName, WhichHand, mergeToOneBone);

			if (modelResource != null)
			{
				mLoadingState = LoadingState.LoadingState_LOADING;

				PrintDebugLog("Starting load " + renderModelName + " model!");

				ImgMaterial = new Material(Shader.Find("Unlit/Texture"));
				wfef = new WaitForEndOfFrame();

				StartCoroutine(SpawnRenderModel());
			}
			else
			{
				PrintDebugLog("Model is null!");

				if (defaultModel != null)
				{
					PrintDebugLog("Can't load controller model from DS, load default model");
					controllerSpawned = Instantiate(defaultModel, this.transform);
					controllerSpawned.transform.parent = this.transform;
					mLoadingState = LoadingState.LoadingState_LOADED;
				} else
				{
					PrintDebugLog("Can't load controller model from DS, default model is null and load WaveFinchController");
					var prefab = Resources.Load("DefaultController/WaveFinchController") as GameObject;
					controllerSpawned = Instantiate(prefab, this.transform);
					controllerSpawned.transform.parent = this.transform;
					mLoadingState = LoadingState.LoadingState_LOADED;
				}
			}
		}

		string emitterMeshName = "__CM__Emitter";
		string textureContent = "";

		IEnumerator SpawnRenderModel()
		{
			while (true)
			{
				if (modelResource != null)
				{
					if (modelResource.parserReady) break;
				}
				PrintDebugLog("SpawnRenderModel is waiting");
				yield return wfef;
			}

			PrintDebugLog("Start to spawn all meshes!");

			if (modelResource == null)
			{
				PrintDebugLog("modelResource is null, skipping spawn objects");
				mLoadingState = LoadingState.LoadingState_NOT_LOADED;
				yield return null;
			}

			int textureSize = modelResource.modelTextureCount;
			PrintDebugLog("modelResource texture count = " + textureSize);

			Profiler.BeginSample("Create Texture");
			for (int t = 0; t < textureSize; t++)
			{
				TextureInfo mainTexture = modelResource.modelTextureInfo[t];

				Texture2D modelpng = new Texture2D((int)mainTexture.width, (int)mainTexture.height, TextureFormat.RGBA32, false);
				modelpng.LoadRawTextureData(mainTexture.modelTextureData);
				modelpng.Apply();

				modelResource.modelTexture[t] = modelpng;

				for (int q = 0; q < 10240; q+=1024) {
					textureContent = "";

					for (int c = 0; c < 64; c++)
					{
						if ((q * 64 + c) >= mainTexture.modelTextureData.Length)
							break;
						textureContent += mainTexture.modelTextureData.GetValue(q*64+c).ToString();
						textureContent += " ";
					}
					PrintDebugLog("T(" + t + ") L(" + q + ")=" + textureContent);
				}

				PrintDebugLog("Add [" + t + "] to texture2D");
			}
			Profiler.EndSample();

			string meshName = "";
			childArray = new GameObject[modelResource.sectionCount];
			showState = new bool[modelResource.sectionCount];
			for (uint i = 0; i < modelResource.sectionCount; i++)
			{
				Profiler.BeginSample("Create Mesh");
				meshName = Marshal.PtrToStringAnsi(modelResource.FBXInfo[i].meshName);
				meshCom = null;
				meshGO = null;

				bool meshAlready = false;

				for (uint j = 0; j < i; j++)
				{
					string tmp = Marshal.PtrToStringAnsi(modelResource.FBXInfo[j].meshName);

					if (tmp.Equals(meshName))
					{
						meshAlready = true;
					}
				}

				if (meshAlready)
				{
					PrintDebugLog(meshName + " is created! skip.");
					Profiler.EndSample();
					continue;
				}

				updateMesh = new Mesh();
				meshCom = new GameObject();
				meshCom.AddComponent<MeshRenderer>();
				meshCom.AddComponent<MeshFilter>();
				meshGO = Instantiate(meshCom);
				meshGO.transform.parent = this.transform;
				meshGO.name = meshName;
				childArray[i] = meshGO;
				Destroy(meshCom);
				Matrix4x4 t = TransformConverter.RigidTransform.toMatrix44(modelResource.FBXInfo[i].matrix, false);

				Vector3 x = TransformConverter.GetPosition(t);
				meshGO.transform.localPosition = new Vector3(x.x, x.y, -x.z);

				meshGO.transform.localRotation = TransformConverter.GetRotation(t);
				Vector3 r = meshGO.transform.localEulerAngles;
				meshGO.transform.localEulerAngles = new Vector3(-r.x, r.y, r.z);
				meshGO.transform.localScale = TransformConverter.GetScale(t);

				PrintDebugLog("i = " + i + " MeshGO = " + meshName + ", localPosition: " + meshGO.transform.localPosition.x + ", " + meshGO.transform.localPosition.y + ", " + meshGO.transform.localPosition.z);
				PrintDebugLog("i = " + i + " MeshGO = " + meshName + ", localRotation: " + meshGO.transform.localEulerAngles);
				PrintDebugLog("i = " + i + " MeshGO = " + meshName + ", localScale: " + meshGO.transform.localScale);

				var meshfilter = meshGO.GetComponent<MeshFilter>();
				updateMesh.Clear();
				updateMesh.vertices = modelResource.SectionInfo[i]._vectice;
				updateMesh.uv = modelResource.SectionInfo[i]._uv;
				updateMesh.uv2 = modelResource.SectionInfo[i]._uv;
				updateMesh.colors32 = colors.ToArray();
				updateMesh.normals = modelResource.SectionInfo[i]._normal;
				updateMesh.SetIndices(modelResource.SectionInfo[i]._indice, MeshTopology.Triangles, 0);
				updateMesh.name = meshName;
				if (meshfilter != null)
				{
					meshfilter.mesh = updateMesh;
				}
				var meshRenderer = meshGO.GetComponent<MeshRenderer>();
				if (meshRenderer != null)
				{
					if (ImgMaterial == null)
					{
						PrintDebugLog("ImgMaterial is null");
					}
					meshRenderer.material = ImgMaterial;
					meshRenderer.material.mainTexture = modelResource.modelTexture[0];
					meshRenderer.enabled = true;
				}

				if (meshName.Equals(emitterMeshName))
				{
					PrintDebugLog(meshName + " is found, set " + meshName + " active: true");
					meshGO.SetActive(true);
				}
				else if (meshName.Equals("__CM__Battery"))
				{
					isBatteryIndicatorReady = false;
					if (modelResource.isBatterySetting)
					{
						if (modelResource.batteryTextureList != null)
						{
							batteryMR = meshGO.GetComponent<MeshRenderer>();
							Material mat = null;

							if (modelResource.hand == XR_Hand.Dominant)
							{
								PrintDebugLog(modelResource.hand + " loaded Materials/WaveBatteryMatR");
								mat = Resources.Load("Materials/WaveBatteryMatR") as Material;
							}
							else
							{
								PrintDebugLog(modelResource.hand + " loaded Materials/WaveBatteryMatL");
								mat = Resources.Load("Materials/WaveBatteryMatL") as Material;
							}

							if (mat != null)
							{
								batteryMR.material = mat;
							}

							foreach (BatteryIndicator bi in modelResource.batteryTextureList)
							{
								TextureInfo ti = bi.batteryTextureInfo;

								bi.batteryTexture = new Texture2D((int)ti.width, (int)ti.height, TextureFormat.RGBA32, false);
								bi.batteryTexture.LoadRawTextureData(ti.modelTextureData);
								bi.batteryTexture.Apply();
								PrintInfoLog(" min: " + bi.min + " max: " + bi.max + " loaded: " + bi.textureLoaded + " w: " + ti.width + " h: " + ti.height + " size: " + ti.size + " array length: " + ti.modelTextureData.Length);
							}

							batteryMR.material.mainTexture = modelResource.batteryTextureList[0].batteryTexture;
							batteryMR.enabled = true;
							isBatteryIndicatorReady = true;
						}
					}
					meshGO.SetActive(false);
					PrintDebugLog(meshName + " is found, set " + meshName + " active: false (waiting for update");
					batteryGO = meshGO;
				}
				else if (meshName == "__CM__TouchPad_Touch")
				{
					PrintDebugLog(meshName + " is found, set " + meshName + " active: false");
					meshGO.SetActive(false);
				}
				else
				{
					PrintDebugLog("set " + meshName + " active: " + modelResource.SectionInfo[i]._active);
					meshGO.SetActive(modelResource.SectionInfo[i]._active);
				}

				showState[i] = childArray[i].activeInHierarchy;

				Profiler.EndSample();
				yield return wfef;
			}
			PrintDebugLog("send " + WhichHand + " RENDER_MODEL_READY ");

			Profiler.BeginSample("onRenderModelReady");
			onRenderModelReady?.Invoke(WhichHand);
			Profiler.EndSample();

			// This will cause significant GC event in a scene with complex design.
			//Resources.UnloadUnusedAssets();
			mLoadingState = LoadingState.LoadingState_LOADED;
		}

		void updateBatteryLevel()
		{
			if (batteryGO != null)
			{
				if (showBatterIndicator && isBatteryIndicatorReady && showModel)
				{
					if ((modelResource == null) || (modelResource.batteryTextureList == null))
						return;

					bool found = false;
					WVR_DeviceType type = checkDeviceType();
					float batteryP = Interop.WVR_GetDeviceBatteryPercentage(type);
					if (batteryP < 0)
					{
						PrintDebugLog("updateBatteryLevel BatteryPercentage is negative, return");
						batteryGO.SetActive(false);
						return;
					}
					foreach (BatteryIndicator bi in modelResource.batteryTextureList)
					{
						if (batteryP >= bi.min / 100 && batteryP <= bi.max / 100)
						{
							currentBattery = bi;
							found = true;
							break;
						}
					}
					if (found)
					{
						if (batteryMR != null)
						{
							batteryMR.material.mainTexture = currentBattery.batteryTexture;
							PrintDebugLog("updateBatteryLevel battery level to " + currentBattery.level + ", battery percent: " + batteryP);
							batteryGO.SetActive(true);
						}
						else
						{
							PrintDebugLog("updateBatteryLevel Can't get battery mesh renderer");
							batteryGO.SetActive(false);
						}
					}
					else
					{
						batteryGO.SetActive(false);
					}
				}
				else
				{
					batteryGO.SetActive(false);
				}
			}
		}

		IEnumerator checkRenderModelAndDelete()
		{
			while (true)
			{
				DeleteControllerWhenDisconnect();
				yield return wfs;
			}
		}

		public void showRenderModel(bool isControllerMode)
		{
			Profiler.BeginSample("ShowRenderModel");
			if (childArray != null)
			{
				for (int i = 0; i < childArray.Length; i++)
				{
					if (childArray[i] != null)
					{
						var meshRenderer = childArray[i].GetComponent<MeshRenderer>();

						if (meshRenderer != null)
						{
							bool previousShow = meshRenderer.enabled;
							meshRenderer.enabled = (previousShow && isControllerMode);
						}
					}
				}
			}
			Profiler.EndSample();
		}

		private void deleteChild(string reason)
		{
			Profiler.BeginSample("DeleteChild");
			PrintInfoLog(reason);

			if (childArray != null)
			{
				int ca = childArray.Length;
				PrintInfoLog("deleteChild count: " + ca);

				for (int i = 0; i < ca; i++)
				{
					GameObject CM = childArray[i];
					if (CM != null)
					{
						PrintInfoLog("deleteChild: " + CM.name);
						Destroy(CM);
					}
					childArray[i] = null;
				}
				childArray = null;
			}

			this.connected = false;
			mLoadingState = LoadingState.LoadingState_NOT_LOADED;
			Profiler.EndSample();
		}

		private void DeleteControllerWhenDisconnect()
		{
			if (mLoadingState != LoadingState.LoadingState_LOADED)
				return;

			this.connected = checkConnection();

			if (this.connected)
			{
				WVR_DeviceType type = checkDeviceType();

				string tmprenderModelName = ClientInterface.GetCurrentRenderModelName(type);

				if (tmprenderModelName != renderModelName)
				{
					deleteChild("Destroy controller prefeb because render model is different");
				}
			}
			else
			{
				deleteChild("Destroy controller prefeb because it is disconnect");
			}
			return;
		}

		private bool checkConnection()
		{
#if UNITY_EDITOR
			if (!EnableDirectPreview)
				return true;
#endif
			// InputDevice is a struct.  Therefore GetDeviceAtXRNode will never return null.
			if (!inputDevice.isValid)
				inputDevice = InputDevices.GetDeviceAtXRNode(node);
			return inputDevice.isValid;
		}

		private WVR_DeviceType checkDeviceType()
		{
			WVR_DeviceType type = WVR_DeviceType.WVR_DeviceType_Invalid;
			//if (WaveEssence.Instance && WaveEssence.Instance.IsLeftHanded)
			//{
			//	if (WhichHand == XR_Hand.Dominant)
			//		type = WVR_DeviceType.WVR_DeviceType_Controller_Left;
			//	else
			//		type = WVR_DeviceType.WVR_DeviceType_Controller_Right;
			//}
			//else
			{
				if (WhichHand == XR_Hand.Dominant)
					type = WVR_DeviceType.WVR_DeviceType_Controller_Right;
				else
					type = WVR_DeviceType.WVR_DeviceType_Controller_Left;
			}
			return type;
		}
	}
}
