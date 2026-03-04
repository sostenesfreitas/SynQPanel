using SkiaSharp;
using SynQPanel.Drawing;
using SynQPanel.Models;
using System;

namespace SynQPanel.Rendering
{
    /// <summary>
    /// Single-digit flip renderer (draws ONE digit: 0-9)
    /// Call twice for tens/ones positions
    /// </summary>
    internal static class FlipRendererSingle
    {
        public static void Draw(
            SkiaGraphics g,
            LockedImage currentLocked,
            LockedImage? nextLocked,
            int x,
            int y,
            int w,
            int h,
            int currentDigit,      // 0-9
            int nextDigit,         // 0-9
            bool shouldFlip,       // Is this digit animating?
            float progress,
            float scale,
            string cacheHint,
            float shadowFactor = 0.7f,
            float lightingFactor = 0.4f
        )
        {
            var sampling = new SKSamplingOptions(
                SKFilterMode.Linear,
                SKMipmapMode.None
            );

            currentLocked.AccessSK(w, h, image =>
            {
                int halfH = h / 2;
                int srcHalf = image.Height / 2;
                int dstHalf = halfH;

                float p = Math.Clamp(progress, 0f, 1f);
                float eased = EaseInOutCubic(p);

                using var paint = new SKPaint
                {
                    IsAntialias = true
                };

                // ================================
                // LAYER 1: BACK CARD (NEXT TOP HALF)
                // ================================
                if (shouldFlip && nextLocked != null && nextLocked.Loaded)
                {
                    nextLocked.AccessSK(w, h, nextImage =>
                    {
                        g.Canvas.DrawImage(
                            nextImage,
                            new SKRect(0, 0, nextImage.Width, srcHalf),
                            new SKRect(x, y, x + w, y + dstHalf),
                            sampling,
                            paint
                        );
                    }, cache: true, cacheHint: cacheHint, grContext: null);
                }
                else
                {
                    // Static: show current top half
                    g.Canvas.DrawImage(
                        image,
                        new SKRect(0, 0, image.Width, srcHalf),
                        new SKRect(x, y, x + w, y + dstHalf),
                        sampling,
                        paint
                    );
                }

                // ================================
                // LAYER 2: BOTTOM BASE (CURRENT BOTTOM)
                // ================================
                g.Canvas.DrawImage(
                    image,
                    new SKRect(0, srcHalf, image.Width, image.Height),
                    new SKRect(x, y + dstHalf, x + w, y + h),
                    sampling,
                    paint
                );

                // Only animate if shouldFlip is true
                if (!shouldFlip)
                    return;

                const float splitPoint = 0.55f;

                // ================================
                // LAYER 3: FRONT FLIP (TOP HALF) - PHASE 1
                // ================================
                if (p < splitPoint)
                {
                    float tTop = p / splitPoint;
                    float tTopEased = EaseInOutQuad(tTop);

                    float scaleY = (float)Math.Cos(tTopEased * Math.PI / 2.0);
                    scaleY = Math.Max(0.02f, scaleY);

                    float pivotX = x + w / 2f;
                    float pivotY = y + dstHalf;

                    g.Canvas.Save();
                    g.Canvas.ClipRect(new SKRect(x, y, x + w, y + dstHalf), SKClipOperation.Intersect, true);

                    g.Canvas.Translate(pivotX, pivotY);
                    g.Canvas.Scale(1f, scaleY);

                    float depthPush = tTopEased * 6f;
                    g.Canvas.Translate(0, -depthPush);

                    // Darken during flip
                    // Old: float darkFactor = 1f - (tTopEased * 0.3f);
                    float darkFactor = 1f - (tTopEased * lightingFactor * 0.8f); // Scale 0.8 to prevent pitch black
                    using var flipPaint = new SKPaint
                    {
                        IsAntialias = true,
                        ColorFilter = SKColorFilter.CreateColorMatrix(new float[]
                        {
                            darkFactor, 0, 0, 0, 0,
                            0, darkFactor, 0, 0, 0,
                            0, 0, darkFactor, 0, 0,
                            0, 0, 0, 1, 0
                        })
                    };

                    g.Canvas.DrawImage(
                        image,
                        new SKRect(0, 0, image.Width, srcHalf),
                        new SKRect(-w / 2f, -dstHalf, w / 2f, 0),
                        sampling,
                        flipPaint
                    );

                    // Edge highlight
                    float edgeAlpha = (float)Math.Sin(tTopEased * Math.PI) * 80;
                    if (edgeAlpha > 5)
                    {
                        using var edgePaint = new SKPaint
                        {
                            Color = new SKColor(255, 255, 255, (byte)edgeAlpha),
                            IsAntialias = true,
                            Style = SKPaintStyle.Stroke,
                            StrokeWidth = 1.5f
                        };

                        g.Canvas.DrawLine(-w / 2f, 0, w / 2f, 0, edgePaint);
                    }

                    g.Canvas.Restore();
                }

                // ================================
                // LAYER 4: BOTTOM FLIP (NEXT BOTTOM) - PHASE 2
                // ================================
                if (p >= splitPoint && nextLocked != null && nextLocked.Loaded)
                {
                    float tBot = (p - splitPoint) / (1f - splitPoint);
                    tBot = Math.Clamp(tBot, 0f, 1f);
                    float tBotEased = EaseOutQuad(tBot);

                    float scaleY = (float)Math.Sin(tBotEased * Math.PI / 2.0);
                    scaleY = Math.Max(0.02f, scaleY);

                    float pivotX = x + w / 2f;
                    float pivotY = y + dstHalf;

                    nextLocked.AccessSK(w, h, nextImage =>
                    {
                        g.Canvas.Save();
                        g.Canvas.ClipRect(new SKRect(x, y + dstHalf, x + w, y + h), SKClipOperation.Intersect, true);

                        g.Canvas.Translate(pivotX, pivotY);
                        g.Canvas.Scale(1f, scaleY);

                        float depthPush = (1f - scaleY) * 3f;
                        g.Canvas.Translate(0, depthPush);

                        // Lighten as it unfolds
                        // Old: float lightFactor = 0.7f + (tBotEased * 0.3f);
                        float lightFactor = (1f - (lightingFactor * 0.6f)) + (tBotEased * (lightingFactor * 0.6f));
                        using var flipPaint = new SKPaint
                        {
                            IsAntialias = true,
                            ColorFilter = SKColorFilter.CreateColorMatrix(new float[]
                            {
                                lightFactor, 0, 0, 0, 0,
                                0, lightFactor, 0, 0, 0,
                                0, 0, lightFactor, 0, 0,
                                0, 0, 0, 1, 0
                            })
                        };

                        g.Canvas.DrawImage(
                            nextImage,
                            new SKRect(0, srcHalf, nextImage.Width, nextImage.Height),
                            new SKRect(-w / 2f, 0, w / 2f, dstHalf),
                            sampling,
                            flipPaint
                        );

                        g.Canvas.Restore();
                    }, cache: true, cacheHint: cacheHint, grContext: null);
                }

                // ================================
                // HINGE SHADOW (ENHANCED GRADIENT)
                // ================================
                if (shouldFlip)
                {
                    float hingeStrength = (float)Math.Sin(eased * Math.PI);
                    
                    // Old: byte hingeAlpha = (byte)(hingeStrength * 150);
                    byte hingeAlpha = (byte)(hingeStrength * 255 * shadowFactor);

                    if (hingeAlpha > 5)
                    {
                        using var shadowShader = SKShader.CreateLinearGradient(
                            new SKPoint(x, y + dstHalf - 3),
                            new SKPoint(x, y + dstHalf + 3),
                            new SKColor[] {
                                new SKColor(0, 0, 0, 0),
                                new SKColor(0, 0, 0, hingeAlpha),
                                new SKColor(0, 0, 0, 0)
                            },
                            new float[] { 0f, 0.5f, 1f },
                            SKShaderTileMode.Clamp
                        );

                        using var hingePaint = new SKPaint
                        {
                            Shader = shadowShader,
                            IsAntialias = true
                        };

                        g.Canvas.DrawRect(
                            new SKRect(x, y + dstHalf - 3, x + w, y + dstHalf + 3),
                            hingePaint
                        );
                    }

                    // Ambient shadow
                    if (p > 0.05f && p < 0.95f)
                    {
                        float shadowStrength = (float)Math.Sin(eased * Math.PI) * 0.4f;
                        
                        // Old: byte shadowAlpha = (byte)(shadowStrength * 100);
                        byte shadowAlpha = (byte)(shadowStrength * 150 * shadowFactor);

                        if (shadowAlpha > 5)
                        {
                            using var ambientPaint = new SKPaint
                            {
                                Color = new SKColor(0, 0, 0, shadowAlpha),
                                IsAntialias = true,
                                ImageFilter = SKImageFilter.CreateBlur(4, 4)
                            };

                            float shadowOffset = 4f * shadowStrength;
                            g.Canvas.DrawRect(
                                new SKRect(x + 2, y + 2 + shadowOffset, x + w - 2, y + h - 2),
                                ambientPaint
                            );
                        }
                    }
                }
            });
        }

        // =====================================
        // EASING FUNCTIONS (shared with FlipRendererModern)
        // =====================================
        private static float EaseInOutCubic(float t)
        {
            return t < 0.5f
                ? 4f * t * t * t
                : 1f - (float)Math.Pow(-2f * t + 2f, 3) / 2f;
        }

        private static float EaseInOutQuad(float t)
        {
            return t < 0.5f
                ? 2f * t * t
                : 1f - (float)Math.Pow(-2f * t + 2f, 2) / 2f;
        }

        private static float EaseOutQuad(float t)
        {
            return 1f - (1f - t) * (1f - t);
        }
    }
}
