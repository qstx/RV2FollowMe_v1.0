// "WaveVR SDK 
// © 2020 HTC Corporation. All Rights Reserved.
//
// Unless otherwise required by copyright law and practice,
// upon the execution of HTC SDK license agreement,
// HTC grants you access to and use of the WaveVR SDK(s).
// You shall fully comply with all of HTC’s SDK license agreement terms and
// conditions signed by you and all SDK and API requirements,
// specifications, and documentation provided by HTC to You."

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Wave.Native;
using Wave.Essence.Hand;

namespace Wave.Essence.InputModule.Demo
{
	public class GestureOptionCanvasHandler : MonoBehaviour, IPointerClickHandler
	{
		const string LOG_TAG = "Wave.Essence.Hand.InputModule.GestureOptionCanvasHandler";
		private void DEBUG(string msg)
		{
			if (Log.EnableDebugLog)
				Log.d(LOG_TAG, msg, true);
		}

		private Toggle m_Toggle = null;
		void Start()
		{
			m_Toggle = GetComponent<Toggle>();
		}

		void Update()
		{
			if (m_Toggle == null)
				return;

			if (HandManager.Instance == null)
			{
				m_Toggle.isOn = false;
				return;
			}

			switch (m_Toggle.name)
			{
				case "Fist":
					m_Toggle.isOn = HandManager.Instance.GestureOptions.Gesture.Fist;
					break;
				case "Five":
					m_Toggle.isOn = HandManager.Instance.GestureOptions.Gesture.Five;
					break;
				case "OK":
					m_Toggle.isOn = HandManager.Instance.GestureOptions.Gesture.OK;
					break;
				case "ThumbUp":
					m_Toggle.isOn = HandManager.Instance.GestureOptions.Gesture.ThumbUp;
					break;
				case "IndexUp":
					m_Toggle.isOn = HandManager.Instance.GestureOptions.Gesture.IndexUp;
					break;
				case "Inverse":
					m_Toggle.isOn = HandManager.Instance.GestureOptions.Gesture.Inverse;
					break;
				default:
					break;
			}
		}

		public void OnPointerClick(PointerEventData eventData)
		{
			if (HandManager.Instance == null)
				return;

			switch (gameObject.name)
			{
				case "Fist":
					DEBUG("OnPointerDown() Fist");
					HandManager.Instance.GestureOptions.Gesture.Fist = !HandManager.Instance.GestureOptions.Gesture.Fist;
					break;
				case "Five":
					DEBUG("OnPointerDown() Five");
					HandManager.Instance.GestureOptions.Gesture.Five = !HandManager.Instance.GestureOptions.Gesture.Five;
					break;
				case "OK":
					DEBUG("OnPointerDown() OK");
					HandManager.Instance.GestureOptions.Gesture.OK = !HandManager.Instance.GestureOptions.Gesture.OK;
					break;
				case "ThumbUp":
					DEBUG("OnPointerDown() ThumbUp");
					HandManager.Instance.GestureOptions.Gesture.ThumbUp = !HandManager.Instance.GestureOptions.Gesture.ThumbUp;
					break;
				case "IndexUp":
					DEBUG("OnPointerDown() IndexUp");
					HandManager.Instance.GestureOptions.Gesture.IndexUp = !HandManager.Instance.GestureOptions.Gesture.IndexUp;
					break;
				case "Inverse":
					DEBUG("OnPointerDown() Inverse");
					HandManager.Instance.GestureOptions.Gesture.Inverse = !HandManager.Instance.GestureOptions.Gesture.Inverse;
					break;
				default:
					break;
			}
		}
	}
}
