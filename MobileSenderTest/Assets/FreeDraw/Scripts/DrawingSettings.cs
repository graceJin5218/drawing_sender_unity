using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace FreeDraw
{
    // Helper methods used to set drawing settings
    public class DrawingSettings : MonoBehaviour
    {
        public static bool isCursorOverUI = false;
        public float Transparency = 1f;

        // Changing pen settings is easy as changing the static properties Drawable.Pen_Colour and Drawable.Pen_Width
        public void SetMarkerColour(Color new_color)
        {
            Drawable.Pen_Colour = new_color;
        }
        // new_width is radius in pixels
        public void SetMarkerWidth(int new_width)
        {
            Drawable.Pen_Width = new_width;
            if (Drawable.drawable != null) Drawable.drawable.NotifyBrushParamsChanged();
        }
        public void SetMarkerWidth(float new_width)
        {
            SetMarkerWidth((int)new_width);
        }

        public void SetTransparency(float amount)
        {
            Transparency = amount;
            Color c = Drawable.Pen_Colour;
            c.a = amount;
            Drawable.Pen_Colour = c;
        }


        // Call these these to change the pen settings
        public void SetPenRed()
        {
            Color c = Color.red;
            c.a = Transparency;
            SetMarkerColour(c);
            Drawable.drawable.SetPenBrush();
        }
        public void SetPenGreen()
        {
            Color c = Color.green;
            c.a = Transparency;
            SetMarkerColour(c);
            Drawable.drawable.SetPenBrush();
        }
        public void SetPenBlue()
        {
            Color c = Color.blue;
            c.a = Transparency;
            SetMarkerColour(c);
            Drawable.drawable.SetPenBrush();
        }
        public void SetPenYellow()
        {
            Color c = Color.yellow;
            c.a = Transparency;
            SetMarkerColour(c);
            Drawable.drawable.SetPenBrush();
        }

        public void SetPenPink()
        {
            Color c = new Color(1.0f, 0.412f, 0.706f, 1f);
            c.a = Transparency;
            SetMarkerColour(c);
            Drawable.drawable.SetPenBrush();
        }

        public void SetPenPurple()
        {
            Color c = new Color(0.502f, 0.000f, 0.502f, 1f);
            c.a = Transparency;
            SetMarkerColour(c);
            Drawable.drawable.SetPenBrush();
        }

        public void SetPenBlack()
        {
            Color c = new Color(0.0f, 0.0f, 0.0f, 1f);
            c.a = Transparency;
            SetMarkerColour(c);
            Drawable.drawable.SetPenBrush();
        }

        public void SetPenWhite()
        {
            Color c = new Color(1.0f, 1.0f, 1.0f, 1f);
            c.a = Transparency;
            SetMarkerColour(c);
            Drawable.drawable.SetPenBrush();
        }

        public void ResetCanvas()
        {
            Drawable.drawable.ResetCanvas();
        }


        public void SetEraser()
        {
            Drawable.drawable.SetEraserBrush();
            Drawable.Eraser_Strength = 1f;

            // 1) 원본 복구형(권장) - 처음 상태로 복귀
            Drawable.Eraser_Behaviour = Drawable.EraseBehaviour.ToOriginal;

            // 2) 또는 배경색 덮어쓰기형
            // Drawable.Eraser_Behaviour = Drawable.EraseBehaviour.ToBackground;
            // Drawable.Eraser_Background = Color.white; // 실제 배경색으로 맞춰주세요
        }

        public void PartialSetEraser()
        {
            Drawable.drawable.SetEraserBrush();
            Drawable.Eraser_Strength = 0.5f;
            Drawable.Eraser_Behaviour = Drawable.EraseBehaviour.ToOriginal;
            // 또는 ToBackground
        }

        public void SetFillBrush()
        {
            Drawable.drawable.SetFillBrush();
        }
    }
}