using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Klak.Ndi;


[RequireComponent(typeof(NdiSender))]
public class SpriteToSender : MonoBehaviour
{
    public SpriteRenderer spriteRenderer;
    public int outputWidth = 1028;
    public int outputHeight = 720;
    public bool keepAlpha = true;   // ���� ����(������ ���� �ڵ� Reflection)

    RenderTexture _rt;
    Material _blitMat;
    NdiSender _sender;

    static readonly int _UVOffset = Shader.PropertyToID("_UVOffset");
    static readonly int _UVScale = Shader.PropertyToID("_UVScale");

    void Awake()
    {
        _sender = GetComponent<NdiSender>();

        // ���� ���� �ɼ�(�������� ������Ƽ �̸��� �޶� ���÷���)
        TrySetBoolProperty(_sender, "alphaSupport", keepAlpha);
        TrySetBoolProperty(_sender, "keepAlpha", keepAlpha);

        var rw = (QualitySettings.activeColorSpace == ColorSpace.Linear)
     ? RenderTextureReadWrite.sRGB
     : RenderTextureReadWrite.Linear;

        _rt = new RenderTexture(outputWidth, outputHeight, 0, RenderTextureFormat.ARGB32, rw)
        {
            useMipMap = false,
            autoGenerateMips = false
        };
        _rt.Create();

        _blitMat = new Material(Shader.Find("Hidden/SpriteCropNDI"));
    }

    void OnDestroy()
    {
        if (_rt != null) _rt.Release();
        Destroy(_blitMat);
    }

    void LateUpdate()
    {
        if (spriteRenderer == null || spriteRenderer.sprite == null) return;

        var sp = spriteRenderer.sprite;
        var tex = sp.texture;
        if (tex == null) return;

        // ȸ��/Ÿ��Ʈ ��ŷ�� ��Ȱ�� ���� (Sprite Atlas���� Rotation ����)
        var r = sp.textureRect; // ��������Ʈ�� �����ϴ� �ȼ�����
        var scale = new Vector2(r.width / tex.width, r.height / tex.height);
        var offset = new Vector2(r.x / tex.width, r.y / tex.height);

        _blitMat.SetVector(_UVOffset, offset);
        _blitMat.SetVector(_UVScale, scale);

        // �ҽ� �ؽ�ó�� �ش� ������ _rt�� ����
        Graphics.Blit(tex, _rt, _blitMat, 0);

        // NDI�� ����
        // KlakNDI ������ ���� ������Ƽ/�ʵ� �̸��� �ٸ� �� �־� �� ��� ���
        if (!TrySetTextureProperty(_sender, "SourceTexture", _rt))
            TrySetTextureProperty(_sender, "texture", _rt);
    }

    bool TrySetBoolProperty(object obj, string prop, bool v)
    {
        var p = obj.GetType().GetProperty(prop);
        if (p != null && p.CanWrite && p.PropertyType == typeof(bool)) { p.SetValue(obj, v); return true; }
        var f = obj.GetType().GetField(prop);
        if (f != null && f.FieldType == typeof(bool)) { f.SetValue(obj, v); return true; }
        return false;
    }

    bool TrySetTextureProperty(object obj, string prop, Texture t)
    {
        var p = obj.GetType().GetProperty(prop);
        if (p != null && p.CanWrite && typeof(Texture).IsAssignableFrom(p.PropertyType)) { p.SetValue(obj, t); return true; }
        var f = obj.GetType().GetField(prop);
        if (f != null && typeof(Texture).IsAssignableFrom(f.FieldType)) { f.SetValue(obj, t); return true; }
        return false;
    }
}
