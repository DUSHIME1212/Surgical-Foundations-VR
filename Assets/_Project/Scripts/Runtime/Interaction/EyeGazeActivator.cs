using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace SurgicalFoundations.Interaction
{
    /// <summary>
    /// Keeps the XR rig's Gaze Interactor inactive unless an eye-tracking device exists (Quest Pro yes, Quest 3 no).
    /// The XRI sample's GazeInputManager logs a warning in Awake whenever it starts without eye tracking; activating
    /// it only when a device is present avoids that noise while keeping eye gaze on hardware that has it.
    /// </summary>
    public class EyeGazeActivator : MonoBehaviour
    {
        [SerializeField] GameObject gazeInteractor;

        void OnEnable()
        {
            InputDevices.deviceConnected += OnDeviceConnected;
            TryActivate();
        }

        void OnDisable() => InputDevices.deviceConnected -= OnDeviceConnected;

        void OnDeviceConnected(InputDevice device)
        {
            if ((device.characteristics & InputDeviceCharacteristics.EyeTracking) != 0) TryActivate();
        }

        void TryActivate()
        {
            if (gazeInteractor == null || gazeInteractor.activeSelf) return;
            var devices = new List<InputDevice>();
            InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.EyeTracking, devices);
            if (devices.Count > 0) gazeInteractor.SetActive(true);
        }
    }
}
