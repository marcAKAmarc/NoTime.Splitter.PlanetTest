using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClientConfiguration : MonoBehaviour
{

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.KeypadEnter)){
            if (Cursor.lockState != CursorLockMode.Confined)
            {
                Cursor.lockState = CursorLockMode.Confined;
                Cursor.visible = false;
            }
            else
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            Debug.Log("Cursor.lockState: " + Cursor.lockState.ToString());
        } 
    }
}
