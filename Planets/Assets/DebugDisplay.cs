using UnityEngine;
using UnityEngine.UI;

public class DebugDisplay : MonoBehaviour
{
    public Text Text1;
    public Text Text2;
    public float UpdateUiTime = 1f;
    private float timeSinceUiUpdate = 0f;
    private int frameCount = 0;
    private float fps = 0f;

    // Update is called once per frame
    void Update()
    {
        frameCount += 1;
        timeSinceUiUpdate += Time.deltaTime;
        if (timeSinceUiUpdate > UpdateUiTime)
        {
            fps = frameCount / timeSinceUiUpdate;
            frameCount = 0;
            timeSinceUiUpdate = 0f;

            Text1.text = "Fps: " + fps.ToString();
            Text2.text = "Fps: " + fps.ToString();

        }

    }
}
