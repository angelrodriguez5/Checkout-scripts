using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LookAtCamera : MonoBehaviour
{
    Transform _camera;

    // Start is called before the first frame update
    void Start()
    {
        try
        {
            _camera = GameManager.Instance.MainCamera.transform;
        }
        catch (System.Exception)
        {
            _camera = Camera.main.transform;
        }
    }

    private void Update()
    {
        // Make +Z point away from the camera for world space canvases
        transform.rotation = Quaternion.LookRotation(transform.position - _camera.position, _camera.up);
        //transform.LookAt(_camera, _camera.up);
    }

}
