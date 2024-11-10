using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class DisplaySelectedMotorAngle : MonoBehaviour
{
    public SelectedInfo selected_info;
    public TMPro.TMP_Text tmp_text;

    // Update is called once per frame
    void FixedUpdate()
    {
        tmp_text.text = "Angle: " + selected_info.selected_angle.ToString();
    }
}
