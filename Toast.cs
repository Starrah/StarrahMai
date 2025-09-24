using MelonLoader;
using StarrahMai.AquaMai;
using UnityEngine;

namespace StarrahMai;

public class Toast : MonoBehaviour
{
    public static void ShowToast(GameObject gameObject, string message, float time = 2.0f, float? x = null, float? y = null, float? width = null, float? height = null)
    {
        Destroy(gameObject.GetComponent<Toast>());
        gameObject.AddComponent<Toast>().SetContent(message, time, x, y, width, height);
    }

    public string message = "";
    public float time = 2.0f;
    public float x;
    public float y;
    public float width;
    public float height;
    private float startTime;

    public Toast()
    {
        width = GuiSizes.FontSize * 20f;
        height = GuiSizes.LabelHeight * 2.5f;
        x = GuiSizes.PlayerCenter - this.width / 2f; // 默认居中
        y = Screen.height * .667f;
        startTime = Time.realtimeSinceStartup;
    }

    public void SetContent(string message, float time = 2.0f, float? x = null, float? y = null, float? width = null, float? height = null)
    {
        this.message = message;
        this.time = time;
        if (x != null) this.x = x.Value;
        if (y != null) this.y = y.Value;
        if (width != null) this.width = width.Value;
        if (height != null) this.height = height.Value;
    }

    public void OnGUI()
    {
        var rect = new Rect(x, y, width, height);
        var labelStyle = GUI.skin.GetStyle("label");
        labelStyle.fontSize = (int)(GuiSizes.FontSize * 1.2);
        labelStyle.alignment = TextAnchor.MiddleCenter;
        GUI.Box(rect, "");
        GUI.Label(rect, message);
        if (Time.realtimeSinceStartup - startTime >= time) Destroy(this);
    }
}