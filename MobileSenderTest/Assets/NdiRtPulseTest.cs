using UnityEngine;
using Klak.Ndi;

[RequireComponent(typeof(NdiSender))]
public class NdiRtPulseTest : MonoBehaviour
{
    public RenderTexture sourceRt; // NdiSender.Source Texture와 같은 RT 넣기
    NdiSender _sender;

    void Start()
    {
        _sender = GetComponent<NdiSender>();
        _sender.sourceTexture = sourceRt; // Render Texture mode 강제
    }

    void Update()
    {
        if (!sourceRt) return;
        var prev = RenderTexture.active;
        RenderTexture.active = sourceRt;
        var c = Color.HSVToRGB((Time.time * 0.25f) % 1f, 1f, 1f);
        GL.Clear(true, true, c);  // RT를 매 프레임 다른 색으로 채움
        RenderTexture.active = prev;
    }
}
