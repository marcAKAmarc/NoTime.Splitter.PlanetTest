using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClientConfiguration : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Confined;
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.KeypadEnter)){
            if (Cursor.lockState == CursorLockMode.Confined)
            {
                Cursor.lockState = CursorLockMode.None;
            }
            else if(Cursor.lockState == CursorLockMode.None)
            {
                Cursor.lockState = CursorLockMode.Confined;
            }

            Debug.Log("Cursor.lockState: " + Cursor.lockState.ToString());
        } 
    }
}
