using UnityEngine;

/// <summary>
/// Ensures the camera background is transparent for WebGL.
/// Attach this to the Main Camera.
/// </summary>
public class TransparentBackground : MonoBehaviour
{
    void Start()
    {
        Camera cam = GetComponent<Camera>();
        if (cam != null)
        {
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0, 0, 0, 0);  // Fully transparent
            Debug.Log("Camera background set to transparent");
        }
    }
}
