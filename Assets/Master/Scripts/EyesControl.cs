using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EyesControl : MonoBehaviour
{
    public GameObject eyes;

    public Camera camera;
    public float intensity;
    void Start()
    {
        camera = Camera.main;
    }

    void Update()
    {
        if (camera != null)
        {
            EyesAim();
        }
    }

    void EyesAim()
    {
        Ray ray = camera.ScreenPointToRay(Input.mousePosition);
        Plane eyePlane = new Plane(-camera.transform.forward, transform.position);

        if (eyePlane.Raycast(ray, out float distance))
        {
            Vector3 mouseWorldCoord = ray.GetPoint(distance);
            Vector3 originToMouse = mouseWorldCoord - transform.position;
            originToMouse = Vector3.ClampMagnitude(originToMouse, intensitys);

            eyes.transform.position = Vector3.Lerp(eyes.transform.position, transform.position + originToMouse, 25 * Time.deltaTime); 
        }
    }

}
