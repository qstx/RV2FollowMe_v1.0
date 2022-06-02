// "Wave SDK 
// © 2020 HTC Corporation. All Rights Reserved.
//
// Unless otherwise required by copyright law and practice,
// upon the execution of HTC SDK license agreement,
// HTC grants you access to and use of the Wave SDK(s).
// You shall fully comply with all of HTC’s SDK license agreement terms and
// conditions signed by you and all SDK and API requirements,
// specifications, and documentation provided by HTC to You."

using UnityEngine;
using UnityEngine.UI;
using Wave.Native;
using Wave.Essence.Events;
using Wave.Essence.Hand;

namespace Wave.Essence.InputModule.Demo
{
	[DisallowMultipleComponent]
	[RequireComponent(typeof(Text))]
	sealed class GestureStaticEvent : MonoBehaviour {
		const string LOG_TAG = "Wave.Essence.InputModule.Demo.GestureStaticEvent";
		private void DEBUG(string msg)
		{
			if (Log.EnableDebugLog)
				Log.d (LOG_TAG, m_Hand + ", " + msg, true);
		}

		private readonly string[] s_HandGestures = {
			"Invalid",	//WVR_HandGestureType_Invalid = 0,    /**< The gesture is invalid. */
			"Unknown",	//WVR_HandGestureType_Unknown = 1,    /**< Unknow gesture type. */
			"Fist",		//WVR_HandGestureType_Fist    = 2,    /**< Represent fist gesture. */
			"Five",		//WVR_HandGestureType_Five    = 3,    /**< Represent five gesture. */
			"OK",		//WVR_HandGestureType_OK      = 4,    /**< Represent ok gesture. */
			"ThumbUp",	//WVR_HandGestureType_ThumbUp = 5,    /**< Represent thumb up gesture. */
			"IndexUp",	//WVR_HandGestureType_IndexUp = 6,    /**< Represent index up gesture. */
			"Inverse",	//WVR_HandGestureType_Inverse = 7,    /**< Represent inverse gesture. */
		};

		[SerializeField]
		private HandManager.HandType m_Hand = HandManager.HandType.Right;
		public HandManager.HandType Hand { get { return m_Hand; } set { m_Hand = value; } }

		private Text m_Text = null;

		private HandManager.GestureType m_HandGesture = HandManager.GestureType.Invalid;
		private void OnStaticGesture(params object[] args)
		{
			if (m_Hand == (HandManager.HandType)args[0])
				m_HandGesture = (HandManager.GestureType)args[1];
		}

		#region MonoBehaviour Overrides
		void Start()
		{
			m_Text = gameObject.GetComponent<Text>();
		}

		private bool mEnabled = false;
		void OnEnable()
		{
			if (!mEnabled)
			{
				GeneralEvent.Listen (HandManager.HAND_STATIC_GESTURE, OnStaticGesture);
				mEnabled = true;
			}
		}

		void OnDisable()
		{
			if (mEnabled)
			{
				GeneralEvent.Remove (HandManager.HAND_STATIC_GESTURE, OnStaticGesture);
				mEnabled = false;
			}
		}

		void Update()
		{
			if (m_Text == null || HandManager.Instance == null)
				return;

			m_Text.text = m_Hand + " Gesture: " + s_HandGestures[(int)m_HandGesture];
		}
		#endregion
	}
}
