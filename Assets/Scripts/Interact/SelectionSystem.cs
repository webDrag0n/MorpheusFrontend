using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SelectionSystem : MonoBehaviour
{
    public SelectedInfo selected_info;
    public Material selected_mat;
    private Material original_mat;

    public GameObject current_selected_object;

    private void Update()
    {
        if (selected_info.updated_flag)
        {
            selected_info.updated_flag = false;
            if (current_selected_object != null)
                current_selected_object.GetComponent<Renderer>().material = original_mat;
            current_selected_object = GameObject.Find(selected_info.selected_name);
            Debug.Log("Selected: " + current_selected_object.name);
            original_mat = current_selected_object.GetComponent<Renderer>().material;
            current_selected_object.GetComponent<Renderer>().material = selected_mat;
        }
    }
}
