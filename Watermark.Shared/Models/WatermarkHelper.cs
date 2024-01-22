﻿using SkiaSharp;
using Watermark.Shared.Enums;

namespace Watermark.Win.Models
{
    public class WatermarkHelper
    {
        public Task<string> GenerationAsync(WMCanvas mainCanvas, ZipedTemplate ziped, bool isPreview = true)
        {
            return Task.Run(() => Generation(mainCanvas, ziped, isPreview));
        }
        public string Generation(WMCanvas mainCanvas, ZipedTemplate ziped, bool isPreview = true)
        {
            SKBitmap originalBitmap;
            string path = Global.TemplatesFolder + mainCanvas.ID + Path.DirectorySeparatorChar + "default.jpg";// "C:\\Users\\Jiang\\Pictures\\DSC02754.jpg";
            if (ziped == null)
            {
                var codec = SKCodec.Create(!string.IsNullOrEmpty(mainCanvas.Path) ? mainCanvas.Path : path);
                if (!string.IsNullOrEmpty(mainCanvas.Path))
                {
                    path = mainCanvas.Path;
                    if (isPreview) path = Path.Combine(Global.ThumbnailFolder, path.Substring(path.LastIndexOf('\\') + 1));
                }
                originalBitmap = SKBitmap.Decode(path);
                if (originalBitmap == null)
                {
                    return "";
                }
                originalBitmap = AutoOrient(codec, originalBitmap);
                codec.Dispose();
            }
            else
            {
                originalBitmap = new SKBitmap();
                ziped.Bitmap.CopyTo(originalBitmap);
            }

            if (mainCanvas.Exif == null || mainCanvas.Exif.Count == 0)
            {
                mainCanvas.Exif = ExifHelper.ReadImage(path);
            }

            var xs = (originalBitmap.Height * originalBitmap.Width) / (1080.0 * 1980);
            //创建画布
            var wh_xs = Math.Min(originalBitmap.Width, originalBitmap.Height) * 1.0 / Math.Max(originalBitmap.Width, originalBitmap.Height);
            var singeBorderWidth = originalBitmap.Width / 100.0;
            var singeBorderHeight = originalBitmap.Height / 100.0;
            double sw = singeBorderWidth, sh = singeBorderHeight;
            if (mainCanvas.EnableMarginXS)
            {
                if (sh > sw) sh *= wh_xs;
                else sw *= wh_xs;
            }
            var border_l = sw * mainCanvas.BorderThickness.Left;
            var border_r = sw * mainCanvas.BorderThickness.Right;
            var border_b = sh * mainCanvas.BorderThickness.Bottom;
            var border_t = sh * mainCanvas.BorderThickness.Top;

            var totalHeight = originalBitmap.Height + border_b + border_t;
            var totalWidth = originalBitmap.Width + border_l + border_r;
            var info = new SKImageInfo()
            {
                Height = (int)totalHeight,
                Width = (int)totalWidth,
                AlphaType = originalBitmap.AlphaType,
                ColorSpace = originalBitmap.ColorSpace,
                ColorType = originalBitmap.ColorType
            };
            var targetImage = SKImage.Create(info);
            var targetBitmap = SKBitmap.FromImage(targetImage);
            var targetCanvas = new SKCanvas(targetBitmap);
            mainCanvas.BackgroundColor ??= "#FFF";
            var bkColor = mainCanvas.BackgroundColor.Length > 7 ? mainCanvas.BackgroundColor[..7] : mainCanvas.BackgroundColor;
            targetCanvas.Clear(SKColor.Parse(bkColor));
            if (mainCanvas.ImageProperties != null && mainCanvas.ImageProperties.EnableGaussianBlur)
            {
                GaussianBlur(targetBitmap, originalBitmap, mainCanvas.ImageProperties.GaussianDeep, (float)xs);
            }
            SKPoint p1 = new((float)border_l, (float)border_t);
            if (mainCanvas.ImageProperties != null && mainCanvas.ImageProperties.EnableShadow)
            {
                DrawShadow(targetCanvas, p1, originalBitmap.Width, originalBitmap.Height, xs, mainCanvas.ImageProperties);
            }

            targetCanvas.DrawBitmap(originalBitmap, p1);


            //绘制容器
            foreach (var container in mainCanvas.Children)
            {
                SKBitmap bitmapc = DrawContainer(mainCanvas.Exif, originalBitmap, xs, ref info, container, mainCanvas.ID, ziped);

                var container_point = new SKPoint(0, 0);
                var cl = container.Margin.Left * singeBorderWidth;
                var cr = container.Margin.Right * singeBorderWidth;
                var ct = container.Margin.Top * singeBorderHeight;
                var cb = container.Margin.Bottom * singeBorderHeight;
                //绘制容器的位置
                if (container.ContainerAlignment == ContainerAlignment.Top)
                {
                    container_point = new SKPoint((float)(cl+border_l), (float)ct);
                }
                else if (container.ContainerAlignment == ContainerAlignment.Left)
                {
                    container_point = new SKPoint((float)(cl), (float)(ct+border_t));
                }
                else if (container.ContainerAlignment == ContainerAlignment.Right)
                {
                    var cw = container.WidthPercent * singeBorderWidth;
                    var r = (float)(totalWidth - cr - cw);
                    container_point = new SKPoint(r, (float)(ct + border_t));
                }
                else if (container.ContainerAlignment == ContainerAlignment.Bottom)
                {
                    var ch = container.HeightPercent  * singeBorderHeight;
                    var b = (totalHeight - ch - cb);
                    container_point = new SKPoint((float)(cl+border_l), (float)b);
                }
                targetCanvas.DrawBitmap(bitmapc, container_point);
            }

            using var sk = SKImage.FromBitmap(targetBitmap);
            using var data = sk.Encode(SKEncodedImageFormat.Jpeg, 100);
            if (!isPreview)
            {
                var output = AppDomain.CurrentDomain.BaseDirectory + "output";
                if (!Directory.Exists(output))
                {
                    Directory.CreateDirectory(output);  
                }
                output += Path.DirectorySeparatorChar + Path.GetFileName(mainCanvas.Path);
                using var sm = File.OpenWrite(output);
                data.SaveTo(sm);
                return "";
            }

            var bytes = data.ToArray();
            return "data:image/jpeg;base64," + Convert.ToBase64String(bytes);
        }

        private SKBitmap DrawContainer(Dictionary<string, string> meta, SKBitmap originalBitmap, double xs, ref SKImageInfo info, WMContainer container, string canvasId, ZipedTemplate ziped)
        {
            //创建容器大小的画布
            var hc = container.HeightPercent / 100.0 * originalBitmap.Height;
            var wc = container.WidthPercent / 100.0 * originalBitmap.Width;
            info.Height = (int)hc == 0 ? 1 : (int)hc;
            info.Width = (int)wc == 0 ? 1 : (int)wc;
            var bitmapc = new SKBitmap(info.Width, info.Height);
            var canvasc = new SKCanvas(bitmapc);
            canvasc.Clear(SKColors.Transparent);

            void DrawLogo(double hc, double wc, IWMControl component, WMLogo mLogo, out SKCanvas canvas_cp, out SKBitmap bitmap_logo, Action<SKBitmap> callback)
            {
                //logo系数按窄边计算
                var min = Math.Min(hc, wc);
                if (ziped == null)
                {
                    if (File.Exists(mLogo.Path))
                    {
                        bitmap_logo = SKBitmap.Decode(mLogo.Path);
                    }
                    else
                    {
                        var path = Global.TemplatesFolder + canvasId + Path.DirectorySeparatorChar + mLogo.Path;
                        if (File.Exists(path))
                        {
                            bitmap_logo = SKBitmap.Decode(path);
                        }
                        else
                        {
                            bitmap_logo = new SKBitmap(100, 100);
                        }
                    }
                }
                else
                {
                    if (ziped.Images.TryGetValue(mLogo.Path, out SKBitmap logo))
                    {
                        bitmap_logo = logo;
                    }
                    else
                    {
                        bitmap_logo = new SKBitmap(100, 100);
                    }
                }
                if (mLogo.White2Transparent)
                {
                    bitmap_logo = ConvertWhiteToTransparent(bitmap_logo);
                }
                var hcp = min * (component.Percent / 100.0);

                var logo_xs = hcp * 1.0 / Math.Min(bitmap_logo.Width, bitmap_logo.Height);

                var wcp = bitmap_logo.Width * logo_xs;
                hcp = bitmap_logo.Height * logo_xs;
                var bitmap_cp = new SKBitmap((int)wcp, (int)hcp);
                canvas_cp = new SKCanvas(bitmap_cp);
                var rect_cp = new SKRect(0, 0, (int)wcp, (int)hcp);
                canvas_cp.DrawBitmap(bitmap_logo, rect_cp);

                //记录图表尺寸
                mLogo.Width = wcp;
                mLogo.Height = hcp;
                callback?.Invoke(bitmap_cp);
            }

            void DrawText(double xs, WMText mText, Action<SKPaint> action)
            {
                SKFontStyle fontStyle;
                if (mText.IsBold && mText.IsItalic) fontStyle = SKFontStyle.BoldItalic;
                else if (mText.IsItalic) fontStyle = SKFontStyle.Italic;
                else if (mText.IsBold) fontStyle = SKFontStyle.Bold;
                else fontStyle = SKFontStyle.Normal;

                var families = SKFontManager.Default.FontFamilies;
                SKTypeface tc;
                string fontPath = AppDomain.CurrentDomain.BaseDirectory + "fonts" + Path.DirectorySeparatorChar;
                if (ziped != null)
                {
                    if (ziped.Fonts.TryGetValue(mText.FontFamily, out Stream stream))
                    {
                        tc = SKTypeface.FromStream(stream);
                    }
                    else
                    {
                        tc = SKTypeface.FromFamilyName(families.FirstOrDefault());
                    }
                }
                else if(families.Any(c=> c == mText.FontFamily))
                {
                    tc = SKTypeface.FromFamilyName(mText.FontFamily);
                }
                else if (File.Exists(fontPath + mText.FontFamily))
                {
                    tc =  SKTypeface.FromFile(fontPath + mText.FontFamily);
                }
                else
                {
                    tc = SKTypeface.FromFamilyName(families.FirstOrDefault());
                }
                var typeface_cp = SKFontManager.Default.MatchTypeface(tc, fontStyle);
                var fontxs = Math.Min(hc, wc) / 156.0;
                if (fontxs == 0) fontxs = 1;

                var fontColor = mText.FontColor.Length > 7 ? mText.FontColor.Substring(0, 7) : mText.FontColor;
                //字体乘以系数
                var paint_cp = new SKPaint()
                {
                    Color = SKColor.Parse(fontColor),
                    TextSize = (int)(mText.FontSize * fontxs),
                    Typeface = typeface_cp
                };
                var text = string.Join(" ",
                            mText.Exifs.Select(x =>
                            {
                                if (meta.TryGetValue(x.Key, out var value))
                                {
                                    return x.Prefix + value + x.Suffix;
                                }
                                return x.Prefix + x.Suffix;
                            }));
                var fw = paint_cp.MeasureText(text);
                var fh = paint_cp.FontMetrics.Descent - paint_cp.FontMetrics.Ascent;
                mText.Height = fh;
                mText.Width = fw;

                action?.Invoke(paint_cp);
            }


            //首先计算出所有组件占据的宽高
            foreach (var component in container.Controls)
            {
                if (component is WMLogo mLogo)
                {
                    DrawLogo(hc, wc, component, mLogo, out SKCanvas canvas_cp, out SKBitmap bitmap_logo, null);
                }
                else if (component is WMText mText)
                {
                    DrawText(xs, mText, null);
                }
                else if (component is WMLine mLine)
                {
                    if (mLine.Orientation == Orientation.Horizontal)
                    {
                        mLine.Height = mLine.Thickness * xs;
                        mLine.Width = component.Percent / 100.0 * wc;
                    }
                    else
                    {
                        mLine.Height = component.Percent / 100.0 * hc;
                        mLine.Width = mLine.Thickness * xs;
                    }
                }
                else if (component is WMContainer mContainer)
                {
                    var bitmap_child_c = DrawContainer(meta, bitmapc, xs, ref info, mContainer, canvasId, ziped);
                    mContainer.Height = bitmap_child_c.Height;
                    mContainer.Width = bitmap_child_c.Width;
                }
            }



            //被前面组件占用了的地方
            double occupy_x = 0, occupy_y = 0;
            //计算组件实际的坐标
            foreach (var component in container.Controls)
            {
                double stdx = 0, stdy = 0;
                //水平布局，比例按高计算, margin只左右生效
                if (container.Orientation == Orientation.Horizontal)
                {
                    var ch = container.HeightPercent / 100.0 * originalBitmap.Height;
                    var cw = container.WidthPercent / 100.0 * originalBitmap.Width;
                    if (container.VerticalAlignment == VerticalAlignment.Top)
                    {
                        stdy = 0;
                    }
                    else if (container.VerticalAlignment == VerticalAlignment.Center)
                    {
                        stdy = (ch - component.Height) / 2;
                    }
                    else if (container.VerticalAlignment == VerticalAlignment.Bottom)
                    {
                        stdy = ch - component.Height;
                    }
                    stdy += (component.Margin.Top - component.Margin.Bottom) / 100.0 * ch;

                    if (container.HorizontalAlignment == HorizontalAlignment.Left)
                    {
                        stdx = occupy_x + (ch * (component.Margin.Left - component.Margin.Right) / 100.0);
                        occupy_x = stdx + component.Width;
                    }
                    else if (container.HorizontalAlignment == HorizontalAlignment.Center)
                    {
                        if (occupy_x == 0)
                        {
                            var totalComponentWidth = container.Controls.Sum(c => c.Width) + container.Controls.Select(c => (c.Margin.Left + c.Margin.Right) / 100.0 * container.HeightPercent).Sum();
                            occupy_x = (cw - totalComponentWidth) / 2;
                        }
                        stdx = occupy_x + (ch * (component.Margin.Left - component.Margin.Right) / 100.0);
                        occupy_x = stdx + component.Width;
                    }
                    else if (container.HorizontalAlignment == HorizontalAlignment.Right)
                    {
                        if (occupy_x == 0)
                        {
                            occupy_x = cw;
                        }
                        stdx = occupy_x - component.Width - (ch * (component.Margin.Right - component.Margin.Left) / 100.0);
                        occupy_x = stdx;
                    }
                }
                else
                {
                    var ch = container.HeightPercent / 100.0 * originalBitmap.Height;
                    var cw = container.WidthPercent / 100.0 * originalBitmap.Width;
                    if (container.HorizontalAlignment == HorizontalAlignment.Left)
                    {
                        stdx = 0;
                    }
                    else if (container.HorizontalAlignment == HorizontalAlignment.Center)
                    {
                        stdx = (cw - component.Width) / 2;
                    }
                    else if (container.HorizontalAlignment == HorizontalAlignment.Right)
                    {
                        stdx = cw - component.Width;
                    }

                    stdx += (component.Margin.Left - component.Margin.Right) / 100.0 * cw;


                    if (container.VerticalAlignment == VerticalAlignment.Top)
                    {
                        stdy = 0;
                        occupy_y = stdy + component.Height + (ch * (component.Margin.Top - component.Margin.Bottom) / 100.0);
                    }
                    else if (container.VerticalAlignment == VerticalAlignment.Center)
                    {
                        var min = Math.Min(hc, wc);
                        if (occupy_y == 0)
                        {
                            var totalComponentHeight = container.Controls.Sum(c => c.Height) + container.Controls.Select(c => (c.Margin.Top - c.Margin.Bottom) / 100.0 * min).Sum();
                            occupy_y = (ch - totalComponentHeight) / 2;
                        }
                        //加上当前的上边距
                        occupy_y += (component.Margin.Top / 100.0 * min);
                        stdy = occupy_y;
                        //减去当前的下边距
                        occupy_y = stdy + component.Height - (min * component.Margin.Bottom / 100.0);
                    }
                    else if (container.VerticalAlignment == VerticalAlignment.Bottom)
                    {
                        if (occupy_y == 0)
                        {
                            occupy_y = ch;
                        }
                        stdy = occupy_y - component.Height - (ch * (component.Margin.Bottom - component.Margin.Top) / 100.0);
                        occupy_y = stdy;
                    }


                }

                //绘制
                if (component is WMLogo mLogo)
                {
                    var action = new Action<SKBitmap>((bitmap_cp) =>
                    {
                        canvasc.DrawBitmap(bitmap_cp, new SKPoint((float)stdx, (float)stdy));
                    });
                    DrawLogo(hc, wc, component, mLogo, out SKCanvas canvas_cp, out SKBitmap bitmap_logo, action);
                }
                else if (component is WMText mText)
                {
                    var action = new Action<SKPaint>((p) =>
                    {
                        var skp = new SKPoint((float)stdx, (float)(stdy + mText.Height));
                        var text = string.Join(" ",
                            mText.Exifs.Select(x =>
                            {
                                if (meta.TryGetValue(x.Key, out var value))
                                {
                                    return x.Prefix + value + x.Suffix;
                                }
                                return x.Prefix + x.Suffix;
                            }));
                        canvasc.DrawText(text, skp, p);
                    });
                    DrawText(xs, mText, action);
                }
                else if (component is WMLine mLine)
                {
                    var pt1 = new SKPoint();
                    var pt2 = new SKPoint();
                    mLine.Color ??= "#000";
                    var color = mLine.Color.Length > 7 ? mLine.Color[..7] : mLine.Color;
                    var paint_line = new SKPaint
                    {
                        Color = SKColor.Parse(color),
                        StrokeWidth = (float)Math.Min(mLine.Height, mLine.Width)
                    };
                    var maxLine = Math.Max(mLine.Height, mLine.Width);
                    if (mLine.Orientation == Orientation.Horizontal)
                    {
                        pt1 = new SKPoint((float)stdx, (float)stdy);
                        pt2 = new SKPoint((float)(pt1.X + maxLine), pt1.Y);
                    }
                    else
                    {
                        pt1 = new SKPoint((float)stdx, (float)stdy);
                        pt2 = new SKPoint(pt1.X, (float)(pt1.Y + maxLine));
                    }
                    canvasc.DrawLine(pt1, pt2, paint_line);
                }
                else if (component is WMContainer mContainer)
                {
                    var bitmap_child_c = DrawContainer(meta, bitmapc, xs, ref info, mContainer, canvasId, ziped);
                    var child_cp_pt = new SKPoint((float)stdx, (float)stdy);
                    canvasc.DrawBitmap(bitmap_child_c, child_cp_pt);
                }
            }

            return bitmapc;
        }

        // 将白色像素转为透明像素
        static SKBitmap ConvertWhiteToTransparent(SKBitmap originalBitmap)
        {
            for (int x = 0; x < originalBitmap.Width; x++)
            {
                for (int y = 0; y < originalBitmap.Height; y++)
                {
                    var pixelColor = originalBitmap.GetPixel(x, y);

                    if (pixelColor.Red == 255 && pixelColor.Green == 255 && pixelColor.Blue == 255)
                    {
                        var pc = new SKColor(255, 255, 255, 0);
                        originalBitmap.SetPixel(x, y, pc);
                    }

                }
            }
            return originalBitmap;

        }

        static SKBitmap AutoOrient(SKCodec codec, SKBitmap sKBitmap)
        {
            // 根据EncodedOrigin信息自动调整图像方向
            if (codec.EncodedOrigin != SKEncodedOrigin.TopLeft)
            {
                switch (codec.EncodedOrigin)
                {
                    case (SKEncodedOrigin)3: // 需要逆时针旋转180度
                        return Rotate(sKBitmap, 180);
                    case (SKEncodedOrigin)6: // 需要顺时针旋转90度
                        return Rotate(sKBitmap, 90);
                    case (SKEncodedOrigin)8: // 需要逆时针旋转90度
                        return Rotate(sKBitmap, -90);
                    default:
                        return Rotate(sKBitmap, -90);
                }
            }
            else return sKBitmap;
        }

        public static SKBitmap Rotate(SKBitmap bitmap, double angle)
        {
            double radians = Math.PI * angle / 180;
            float sine = (float)Math.Abs(Math.Sin(radians));
            float cosine = (float)Math.Abs(Math.Cos(radians));
            int originalWidth = bitmap.Width;
            int originalHeight = bitmap.Height;
            int rotatedWidth = (int)(cosine * originalWidth + sine * originalHeight);
            int rotatedHeight = (int)(cosine * originalHeight + sine * originalWidth);

            var rotatedBitmap = new SKBitmap(rotatedWidth, rotatedHeight);

            using (var surface = new SKCanvas(rotatedBitmap))
            {
                surface.Translate(rotatedWidth / 2, rotatedHeight / 2);
                surface.RotateDegrees((float)angle);
                surface.Translate(-originalWidth / 2, -originalHeight / 2);
                surface.DrawBitmap(bitmap, new SKPoint());
            }
            return rotatedBitmap;
        }

        static void DrawShadow(SKCanvas canvas, SKPoint point, int w, int h, double xs, WMImage mImage)
        {
            var rect = new SKRect(point.X, point.Y, point.X + w, point.Y + h);
            float cornerRadius = mImage.CornerRadius;
            using var paint2 = new SKPaint();
            paint2.IsAntialias = true;
            paint2.Color = SKColors.White;

            // 绘制阴影
            paint2.ImageFilter = SKImageFilter.CreateDropShadow(
                dx: 0,
                dy: 0,
                sigmaX: (int)(mImage.ShadowRange * xs),
                sigmaY: (int)(mImage.ShadowRange * xs),
                color: SKColor.Parse(mImage.ShadowColor),
                shadowMode: SKDropShadowImageFilterShadowMode.DrawShadowAndForeground
            );

            canvas.DrawRoundRect(rect, cornerRadius, cornerRadius, paint2);
        }

        private void GaussianBlur(SKBitmap bitmap, SKBitmap original, float sigma, float xs)
        {
            var width = bitmap.Width;
            var height = bitmap.Height;
            var paint = new SKPaint();
            var imageFilter = SKImageFilter.CreateBlur(sigma * xs, sigma * xs);
            paint.ImageFilter = imageFilter;
            var can = new SKCanvas(bitmap);
            can.DrawBitmap(original, new SKRect(0, 0, width, height), paint);
        }

    }
}
