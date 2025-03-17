using UnityEngine;
public class ScreenshotTaker : MonoBehaviour
{
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            string path = Application.dataPath + "/Screenshot.png";
            ScreenCapture.CaptureScreenshot(path);
            Debug.Log("Screenshot saved to: " + path);
        }
    }
}