using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace FreeDraw
{
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(Collider2D))]  // REQUIRES A COLLIDER2D to function
    // 1. Attach this to a read/write enabled sprite image
    // 2. Set the drawing_layers  to use in the raycast
    // 3. Attach a 2D collider (like a Box Collider 2D) to this sprite
    // 4. Hold down left mouse to draw on this texture!
    public class Drawable : MonoBehaviour
    {
        // PEN COLOUR
        public static Color Pen_Colour = Color.red;     // Change these to change the default drawing settings

        // PEN WIDTH (actually, it's a radius, in pixels)
        public static int Pen_Width = 5;


        public delegate void Brush_Function(Vector2 world_position);
        // This is the function called when a left click happens
        // Pass in your own custom one to change the brush type
        // Set the default function in the Awake method
        public Brush_Function current_brush;

        public LayerMask Drawing_Layers;

        public bool Reset_Canvas_On_Play = true;
        // The colour the canvas is reset to each time
        public Color Reset_Colour = new Color(0, 0, 0, 0);  // By default, reset the canvas to be transparent
		
		public bool Reset_To_This_Texture_On_Play = false;	// If true, will reset the image back to whatever reset texture is
		public Texture2D reset_texture;

        // Used to reference THIS specific file without making all methods static
        public static Drawable drawable;
        // MUST HAVE READ/WRITE enabled set in the file editor of Unity
        Sprite drawable_sprite;
        Texture2D drawable_texture;

        Vector2 previous_drag_position;
        Color[] clean_colours_array;
        Color transparent;
        Color32[] cur_colors;
        bool mouse_was_previously_held_down = false;
        bool no_drawing_on_current_drag = false;

        // 지우개 동작 방식
        public enum EraseBehaviour { Transparent, ToBackground, ToOriginal }
        public static EraseBehaviour Eraser_Behaviour = EraseBehaviour.ToOriginal;

        // 배경색(‘ToBackground’일 때 사용). UI가 흰색이면 Color.white
        public static Color Eraser_Background = Color.white;

        // 지우개 강도
        public static float Eraser_Strength = 1f;

        // 최초 상태 백업(원본 복구용)
        private Color32[] original_colors;


        // 펜/지우개 굵기·색상 등 브러시 파라미터가 바뀔 때 호출하면
        // 현재 드래그를 끊어주어 선이 뚝뚝 끊기거나 점프하는 현상을 방지

        // 지우개 처음 전환 시 사용할 기본 굵기(반지름, px)
        public static int Eraser_Default_Width = 12;

        // 내부 상태: 지우개 기본 굵기 1회 적용 여부, 펜 굵기 복원용
        private static bool _applyEraserDefaultOnFirstUse = true;
        private static int _savedPenWidthBeforeEraser = -1;


        void Start()  // 이미 Start가 있으면 그 안에 아래 한 줄만 추가
        {
            // ... 기존 초기화 코드 ...
            if (drawable_texture != null)
                original_colors = drawable_texture.GetPixels32();
        }

        //////////////////////////////////////////////////////////////////////////////
        // BRUSH TYPES. Implement your own here
        // How to write your own brush method:
        // 1. Copy and rename the BrushTemplate() method below with your own brush
        // 2. Write your own code inside of this method
        // 3. Assign this method to the current_brush variable (see how PenBrush does this)


        // When you want to make your own type of brush effects,
        // Copy, paste and rename this function.
        // Go through each step
        public void BrushTemplate(Vector2 world_position)
        {
            // 1. Change world position to pixel coordinates
            Vector2 pixel_pos = WorldToPixelCoordinates(world_position);

            // 2. Make sure our variable for pixel array is updated in this frame
            cur_colors = drawable_texture.GetPixels32();

            ////////////////////////////////////////////////////////////////
            // FILL IN CODE BELOW HERE

            // Do we care about the user left clicking and dragging?
            // If you don't, simply set the below if statement to be:
            //if (true)

            // If you do care about dragging, use the below if/else structure
            if (previous_drag_position == Vector2.zero)
            {
                // THIS IS THE FIRST CLICK
                // FILL IN WHATEVER YOU WANT TO DO HERE
                // Maybe mark multiple pixels to colour?
                MarkPixelsToColour(pixel_pos, Pen_Width, Pen_Colour);
            }
            else
            {
                // THE USER IS DRAGGING
                // Should we do stuff between the previous mouse position and the current one?
                ColourBetween(previous_drag_position, pixel_pos, Pen_Width, Pen_Colour);
            }
            ////////////////////////////////////////////////////////////////

            // 3. Actually apply the changes we marked earlier
            // Done here to be more efficient
            ApplyMarkedPixelChanges();
            
            // 4. If dragging, update where we were previously
            previous_drag_position = pixel_pos;
        }



        
        // Default brush type. Has width and colour.
        // Pass in a point in WORLD coordinates
        // Changes the surrounding pixels of the world_point to the static pen_colour
        public void PenBrush(Vector2 world_point)
        {
            Vector2 pixel_pos = WorldToPixelCoordinates(world_point);

            cur_colors = drawable_texture.GetPixels32();

            if (previous_drag_position == Vector2.zero)
            {
                // If this is the first time we've ever dragged on this image, simply colour the pixels at our mouse position
                MarkPixelsToColour(pixel_pos, Pen_Width, Pen_Colour);
            }
            else
            {
                // Colour in a line from where we were on the last update call
                ColourBetween(previous_drag_position, pixel_pos, Pen_Width, Pen_Colour);
            }
            ApplyMarkedPixelChanges();

            //Debug.Log("Dimensions: " + pixelWidth + "," + pixelHeight + ". Units to pixels: " + unitsToPixels + ". Pixel pos: " + pixel_pos);
            previous_drag_position = pixel_pos;
        }


        // Helper method used by UI to set what brush the user wants
        // Create a new one for any new brushes you implement
        public void SetPenBrush()
        {
            // PenBrush is the NAME of the method we want to set as our current brush
            current_brush = PenBrush;

            // 지우개로 갔다가 돌아오면 펜의 원래 굵기 복원
            if (_savedPenWidthBeforeEraser > 0)
            {
                Pen_Width = _savedPenWidthBeforeEraser;
                previous_drag_position = Vector2.zero;  // 즉시 반영
            }
        }


        // FILL BRUSH, from Francesco Filipini
        public void FillBrush(Vector2 world_position)
        {
            Vector2 pixel_pos = WorldToPixelCoordinates(world_position);
            int x = (int)pixel_pos.x;
            int y = (int)pixel_pos.y;

            Color target_color = drawable_texture.GetPixel(x, y);
            if (target_color == Pen_Colour) return;

            Queue<Vector2> pixels = new Queue<Vector2>();
            pixels.Enqueue(new Vector2(x, y));

            while (pixels.Count > 0)
            {
                Vector2 current_pixel = pixels.Dequeue();
                int cx = (int)current_pixel.x;
                int cy = (int)current_pixel.y;

                if (cx < 0 || cx >= drawable_texture.width || cy < 0 || cy >= drawable_texture.height) continue;
                if (drawable_texture.GetPixel(cx, cy) != target_color) continue;

                drawable_texture.SetPixel(cx, cy, Pen_Colour);

                pixels.Enqueue(new Vector2(cx + 1, cy));
                pixels.Enqueue(new Vector2(cx - 1, cy));
                pixels.Enqueue(new Vector2(cx, cy + 1));
                pixels.Enqueue(new Vector2(cx, cy - 1));
            }

            drawable_texture.Apply();
        }

        public void SetFillBrush()
        {
            current_brush = FillBrush;
        }
        //////////////////////////////////////////////////////////////////////////////

        // 지우개로 전환
        public void SetEraserBrush()
        {
            current_brush = EraserBrush;

            // 지우개를 처음 켤 때만 기본 굵기 적용
            if (_applyEraserDefaultOnFirstUse)
            {
                _savedPenWidthBeforeEraser = Pen_Width;                 // 펜 굵기 기억
                Pen_Width = Mathf.Max(1, Eraser_Default_Width);         // 지우개 기본 굵기 적용
                previous_drag_position = Vector2.zero;                  // 바로 반영되도록 드래그 리셋
                _applyEraserDefaultOnFirstUse = false;
            }
        }

         // 브러시 파라미터(굵기 등) 변경 시 드래그 경로를 끊어 즉시 반영
        public void NotifyBrushParamsChanged()
        {
            previous_drag_position = Vector2.zero;
        }

    // ERASE: 알파를 줄이면서 RGB도 새 알파 비율만큼 축소(프리멀티플라이 보정)
        public void EraserBrush(Vector2 world_point)
        {
            Vector2 pixel_pos = WorldToPixelCoordinates(world_point);
            cur_colors = drawable_texture.GetPixels32();

            if (previous_drag_position == Vector2.zero)
            {
                MarkPixelsToErase(pixel_pos, Pen_Width, Eraser_Strength);
            }
            else
            {
                ColourBetweenErase(previous_drag_position, pixel_pos, Pen_Width, Eraser_Strength);
            }

            ApplyMarkedPixelChanges();
            previous_drag_position = pixel_pos;
        }

        public void ColourBetweenErase(Vector2 start_point, Vector2 end_point, int width, float strength)
        {
            float distance = Vector2.Distance(start_point, end_point);
            if (distance <= 0f) { MarkPixelsToErase(start_point, width, strength); return; }
            float lerp_steps = 1f / distance;
            for (float lerp = 0; lerp <= 1f; lerp += lerp_steps)
            {
                Vector2 cur = Vector2.Lerp(start_point, end_point, lerp);
                MarkPixelsToErase(cur, width, strength);
            }
        }

        public void MarkPixelsToErase(Vector2 center_pixel, int pen_thickness, float strength)
        {
            int cx = (int)center_pixel.x;
            int cy = (int)center_pixel.y;
            int r = Mathf.Max(1, pen_thickness);

            int w = (int)drawable_sprite.rect.width;
            int h = (int)drawable_sprite.rect.height;

            int x0 = Mathf.Max(0, cx - r);
            int x1 = Mathf.Min(w - 1, cx + r);
            int y0 = Mathf.Max(0, cy - r);
            int y1 = Mathf.Min(h - 1, cy + r);

            float rf = r;

            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                {
                    float dx = x - cx, dy = y - cy;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    if (dist > rf) continue;

                    float edge = 1f - (dist / rf); // soft edge
                    edge *= edge;
                    float s = Mathf.Clamp01(strength * edge);
                    MarkPixelToErase(x, y, s);
                }
        }

        public void MarkPixelToErase(int x, int y, float strength01)
        {
            int w = (int)drawable_sprite.rect.width;
            int h = (int)drawable_sprite.rect.height;
            if (x < 0 || x >= w || y < 0 || y >= h) return;

            int idx = y * w + x;
            if (idx < 0 || idx >= cur_colors.Length) return;

            Color32 cur = cur_colors[idx];
            float s = Mathf.Clamp01(strength01);

            switch (Eraser_Behaviour)
            {
                case EraseBehaviour.ToOriginal:
                    {
                        // 최초 텍스처 상태로 복구(강도 s만큼 원본으로 당김)
                        if (original_colors != null && original_colors.Length == cur_colors.Length)
                        {
                            Color32 org = original_colors[idx];
                            cur_colors[idx] = new Color32(
                                (byte)Mathf.Round(Mathf.Lerp(cur.r, org.r, s)),
                                (byte)Mathf.Round(Mathf.Lerp(cur.g, org.g, s)),
                                (byte)Mathf.Round(Mathf.Lerp(cur.b, org.b, s)),
                                (byte)Mathf.Round(Mathf.Lerp(cur.a, org.a, s))
                            );
                        }
                        else
                        {
                            // 백업 없으면 배경색으로 덮기
                            goto case EraseBehaviour.ToBackground;
                        }
                        break;
                    }
                case EraseBehaviour.ToBackground:
                    {
                        // 배경색으로 불투명하게(뚫리지 않음)
                        Vector3 rgb = Vector3.Lerp(
                            new Vector3(cur.r / 255f, cur.g / 255f, cur.b / 255f),
                            new Vector3(Eraser_Background.r, Eraser_Background.g, Eraser_Background.b),
                            s
                        );
                        float a = Mathf.Lerp(cur.a / 255f, 1f, s);
                        cur_colors[idx] = new Color32(
                            (byte)Mathf.Round(rgb.x * 255f),
                            (byte)Mathf.Round(rgb.y * 255f),
                            (byte)Mathf.Round(rgb.z * 255f),
                            (byte)Mathf.Round(a * 255f)
                        );
                        break;
                    }
                case EraseBehaviour.Transparent:
                default:
                    {
                        // 완전 투명(배경 보임). 경계 회색 방지 위해 RGB = 배경색
                        cur_colors[idx] = new Color32(
                            (byte)Mathf.Round(Eraser_Background.r * 255f),
                            (byte)Mathf.Round(Eraser_Background.g * 255f),
                            (byte)Mathf.Round(Eraser_Background.b * 255f),
                            0
                        );
                        break;
                    }
            }
        }

        // This is where the magic happens.
        // Detects when user is left clicking, which then call the appropriate function
        void Update()
        {
            // Is the user holding down the left mouse button?
            bool mouse_held_down = Input.GetMouseButton(0);
            if (mouse_held_down && !no_drawing_on_current_drag)
            {
                // Convert mouse coordinates to world coordinates
                Vector2 mouse_world_position = Camera.main.ScreenToWorldPoint(Input.mousePosition);

                // Check if the current mouse position overlaps our image
                Collider2D hit = Physics2D.OverlapPoint(mouse_world_position, Drawing_Layers.value);
                if (hit != null && hit.transform != null)
                {
                    // We're over the texture we're drawing on!
                    // Use whatever function the current brush is
                    current_brush(mouse_world_position);
                }

                else
                {
                    // We're not over our destination texture
                    previous_drag_position = Vector2.zero;
                    if (!mouse_was_previously_held_down)
                    {
                        // This is a new drag where the user is left clicking off the canvas
                        // Ensure no drawing happens until a new drag is started
                        no_drawing_on_current_drag = true;
                    }
                }
            }
            // Mouse is released
            else if (!mouse_held_down)
            {
                previous_drag_position = Vector2.zero;
                no_drawing_on_current_drag = false;
            }
            mouse_was_previously_held_down = mouse_held_down;
        }



        // Set the colour of pixels in a straight line from start_point all the way to end_point, to ensure everything inbetween is coloured
        public void ColourBetween(Vector2 start_point, Vector2 end_point, int width, Color color)
        {
            // Get the distance from start to finish
            float distance = Vector2.Distance(start_point, end_point);
            Vector2 direction = (start_point - end_point).normalized;

            Vector2 cur_position = start_point;

            // Calculate how many times we should interpolate between start_point and end_point based on the amount of time that has passed since the last update
            float lerp_steps = 1 / distance;

            for (float lerp = 0; lerp <= 1; lerp += lerp_steps)
            {
                cur_position = Vector2.Lerp(start_point, end_point, lerp);
                MarkPixelsToColour(cur_position, width, color);
            }
        }





        public void MarkPixelsToColour(Vector2 center_pixel, int pen_thickness, Color color_of_pen)
        {
            // Figure out how many pixels we need to colour in each direction (x and y)
            int center_x = (int)center_pixel.x;
            int center_y = (int)center_pixel.y;
            //int extra_radius = Mathf.Min(0, pen_thickness - 2);

            for (int x = center_x - pen_thickness; x <= center_x + pen_thickness; x++)
            {
                // Check if the X wraps around the image, so we don't draw pixels on the other side of the image
                if (x >= (int)drawable_sprite.rect.width || x < 0)
                    continue;

                for (int y = center_y - pen_thickness; y <= center_y + pen_thickness; y++)
                {
                    MarkPixelToChange(x, y, color_of_pen);
                }
            }
        }
        public void MarkPixelToChange(int x, int y, Color color)
        {
            // Need to transform x and y coordinates to flat coordinates of array
            int array_pos = y * (int)drawable_sprite.rect.width + x;

            // Check if this is a valid position
            if (array_pos >= cur_colors.Length || array_pos < 0)
                return;

            cur_colors[array_pos] = color;
        }
        public void ApplyMarkedPixelChanges()
        {
            drawable_texture.SetPixels32(cur_colors);
            drawable_texture.Apply();
        }


        // Directly colours pixels. This method is slower than using MarkPixelsToColour then using ApplyMarkedPixelChanges
        // SetPixels32 is far faster than SetPixel
        // Colours both the center pixel, and a number of pixels around the center pixel based on pen_thickness (pen radius)
        public void ColourPixels(Vector2 center_pixel, int pen_thickness, Color color_of_pen)
        {
            // Figure out how many pixels we need to colour in each direction (x and y)
            int center_x = (int)center_pixel.x;
            int center_y = (int)center_pixel.y;
            //int extra_radius = Mathf.Min(0, pen_thickness - 2);

            for (int x = center_x - pen_thickness; x <= center_x + pen_thickness; x++)
            {
                for (int y = center_y - pen_thickness; y <= center_y + pen_thickness; y++)
                {
                    drawable_texture.SetPixel(x, y, color_of_pen);
                }
            }

            drawable_texture.Apply();
        }


        public Vector2 WorldToPixelCoordinates(Vector2 world_position)
        {
            // Change coordinates to local coordinates of this image
            Vector3 local_pos = transform.InverseTransformPoint(world_position);

            // Change these to coordinates of pixels
            float pixelWidth = drawable_sprite.rect.width;
            float pixelHeight = drawable_sprite.rect.height;
            float unitsToPixels = pixelWidth / drawable_sprite.bounds.size.x * transform.localScale.x;

            // Need to center our coordinates
            float centered_x = local_pos.x * unitsToPixels + pixelWidth / 2;
            float centered_y = local_pos.y * unitsToPixels + pixelHeight / 2;

            // Round current mouse position to nearest pixel
            Vector2 pixel_pos = new Vector2(Mathf.RoundToInt(centered_x), Mathf.RoundToInt(centered_y));

            return pixel_pos;
        }
		// Some guy requested this - it might be wrong
        public Vector3 PixelToWorldCoordinates(Vector2 pixel_pos)
        {
			float pixelWidth = drawable_sprite.rect.width;
            float pixelHeight = drawable_sprite.rect.height;
            float unitsToPixels = pixelWidth / drawable_sprite.bounds.size.x * transform.localScale.x;
			
			// Need to uncenter our coordinates
			float uncentered_x = pixel_pos.x / unitsToPixels - pixelWidth / 2;
			float uncentered_y = pixel_pos.y / unitsToPixels - pixelHeight / 2;
			
			// Convert point to world space
			Vector3 world_pos = transform.TransformPoint(new Vector3(uncentered_x, uncentered_y, 0f));
            return world_pos;
        }

        // Changes every pixel to be the reset colour
        public void ResetCanvas()
        {
            drawable_texture.SetPixels(clean_colours_array);
            drawable_texture.Apply();
        }


        void Awake()
        {
            drawable = this;
            // DEFAULT BRUSH SET HERE
            current_brush = PenBrush;

            drawable_sprite = this.GetComponent<SpriteRenderer>().sprite;
            drawable_texture = drawable_sprite.texture;

            // Initialize clean pixels to use
            clean_colours_array = new Color[(int)drawable_sprite.rect.width * (int)drawable_sprite.rect.height];
            for (int x = 0; x < clean_colours_array.Length; x++)
                clean_colours_array[x] = Reset_Colour;

            // Should we reset our canvas image when we hit play in the editor?
            if (Reset_Canvas_On_Play)
                ResetCanvas();
			else if (Reset_To_This_Texture_On_Play)
			{
				Graphics.CopyTexture(reset_texture, drawable_texture);
				//drawable_texture = reset_texture;
				Debug.Log("Reset texture");
			}

            // 원본 백업 (Reset 적용 이후 최종 상태)
            original_colors = drawable_texture.GetPixels32();
        }
    }
}