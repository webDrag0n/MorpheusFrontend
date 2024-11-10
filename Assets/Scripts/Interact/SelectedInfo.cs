using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SelectedInfo", menuName = "ScriptableObjects/SelectedInfo")]
public class SelectedInfo : ScriptableObject
{
    public string selected_name;
    public string selected_description;
    public Vector3 selected_angle;
    public bool updated_flag = false;
}
