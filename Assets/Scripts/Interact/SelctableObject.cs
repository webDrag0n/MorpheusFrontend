using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SelctableObject : MonoBehaviour
{
    public SelectedInfo selected_info;
    public GameObject binded_motor;
    private void OnMouseDown()
    {
        selected_info.selected_name = transform.gameObject.name;
        selected_info.selected_description = "This is a " + transform.gameObject.name;
        selected_info.selected_angle = binded_motor.transform.rotation.eulerAngles;
        selected_info.updated_flag = true;
    }

}
