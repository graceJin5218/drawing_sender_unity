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
    public bool keepAlpha = true;   // 알파 유지(버전에 따라 자동 Reflection)

    RenderTexture _rt;
    Material _blitMat;
    NdiSender _sender;

    static readonly int _UVOffset = Shader.PropertyToID("_UVOffset");
    static readonly int _UVScale = Shader.PropertyToID("_UVScale");

    void Awake()
    {
        _sender = GetComponent<NdiSender>();

        // 알파 유지 옵션(버전별로 프로퍼티 이름이 달라 리플렉션)
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

        // 회전/타이트 패킹은 비활성 권장 (Sprite Atlas에서 Rotation 끄기)
        var r = sp.textureRect; // 스프라이트가 차지하는 픽셀영역
        var scale = new Vector2(r.width / tex.width, r.height / tex.height);
        var offset = new Vector2(r.x / tex.width, r.y / tex.height);

        _blitMat.SetVector(_UVOffset, offset);
        _blitMat.SetVector(_UVScale, scale);

        // 소스 텍스처의 해당 영역만 _rt로 복사
        Graphics.Blit(tex, _rt, _blitMat, 0);

        // NDI로 전송
        // KlakNDI 버전에 따라 프로퍼티/필드 이름이 다를 수 있어 두 경로 사용
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
