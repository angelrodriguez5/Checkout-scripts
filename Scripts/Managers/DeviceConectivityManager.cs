using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;


/// <summary>
/// Centralized class for player device conectivity management.
/// Device conectivity events are called from the PlayerInput component
/// and received on the PlayerController which are spawned dynamically.
/// This class allows to preconfigure which actions will take place when 
/// devices for any player are disconnected or reconected
/// </summary>
public class DeviceConectivityManager : MonoBehaviour
{
    public UnityEvent<PlayerInput> onDeviceLost;
    public UnityEvent<PlayerInput> onDeviceRegained;

    public static DeviceConectivityManager Instance;

    private void Awake()
    {
        if(Instance != null)
        {
            throw new System.Exception("Several DeviceLostManager present in the scene");
        }
        Instance = this;
    }
}
