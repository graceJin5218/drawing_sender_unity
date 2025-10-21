using UnityEngine;
using Klak.Ndi;

[RequireComponent(typeof(NdiSender))]
public class NdiRtPulseTest : MonoBehaviour
{
    public RenderTexture sourceRt; // NdiSender.Source Texture�� ���� RT �ֱ�
    NdiSender _sender;

    void Start()
    {
        _sender = GetComponent<NdiSender>();
        _sender.sourceTexture = sourceRt; // Render Texture mode ����
    }

    void Update()
    {
        if (!sourceRt) return;
        var prev = RenderTexture.active;
        RenderTexture.active = sourceRt;
        var c = Color.HSVToRGB((Time.time * 0.25f) % 1f, 1f, 1f);
        GL.Clear(true, true, c);  // RT�� �� ������ �ٸ� ������ ä��
        RenderTexture.active = prev;
    }
}
