using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClientConfiguration : MonoBehaviour
{

    public void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }
    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.KeypadEnter)){
            if (Cursor.lockState != CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.Locked;
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
