using SynQPanel.Infrastructure;
using SynQPanel.Models;
using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SynQPanel.Views.Components
{
    public partial class FlipProperties : UserControl
    {
        public FlipProperties()
        {
            InitializeComponent();
            DataContextChanged += FlipProperties_DataContextChanged;
        }

        // ==========================================
        // UI LOGIC (Enable/Disable controls)
        // ==========================================
        private void UpdateUiState(FlipDisplayItem flip)
        {
            bool isSingleDigit = flip.FlipStyle == FlipStyle.SingleDigit;

            DigitSpacingNumberBox.IsEnabled = isSingleDigit;
            DigitSpacingLabel.Opacity = isSingleDigit ? 1.0 : 0.5;

            // Trigger re-validation of images when style changes
            CheckImportStatus(flip);
        }

        private void FlipStyleComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataContext is FlipDisplayItem flip)
            {
                UpdateUiState(flip);
                // ✅ Reload preview because style changed (might need to switch from 00 to 0)
                LoadExistingPreview(flip);
            }
        }

        // ==========================================
        // DATA CONTEXT CHANGED (On Load)
        // ==========================================
        private void FlipProperties_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is FlipDisplayItem flip)
            {
                UpdateUiState(flip);
                LoadExistingPreview(flip);

                // FIX 1: Don't show status on load (keep it clean)
                ImportStatusText.Text = "";
            }
        }

        // ==========================================
        // VALIDATION LOGIC (Issue 1 Solved)
        // ==========================================
        private void CheckImportStatus(FlipDisplayItem flip)
        {
            string folder = flip.CalculatedImageFolder;
            if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
            {
                ShowStatus("No folder selected.", Colors.Gray);
                return;
            }

            int count = CountValidImages(folder, flip.FlipStyle);
            int expected = flip.FlipStyle == FlipStyle.SingleDigit ? 10 : 60;
            string range = flip.FlipStyle == FlipStyle.SingleDigit ? "0-9" : "00-59";

            if (count == expected)
            {
                ShowStatus($"✅ Loaded: {count}/{expected} images ({range})", Colors.Green);
            }
            else if (count > 0)
            {
                ShowStatus($"⚠️ Incomplete: Found {count}/{expected} images for {flip.FlipStyle} mode.", Colors.Orange);
            }
            else
            {
                ShowStatus($"❌ Missing: No valid images found for {flip.FlipStyle} mode ({range}).", Colors.Red);
            }
        }

        private int CountValidImages(string folder, FlipStyle style)
        {
            int count = 0;
            var extensions = new[] { ".png", ".jpg", ".jpeg", ".bmp" };

            if (style == FlipStyle.SingleDigit)
            {
                // Check 0-9
                for (int i = 0; i < 10; i++)
                {
                    foreach (var ext in extensions)
                    {
                        // Check "0.png" OR "00.png" (fallback)
                        if (File.Exists(Path.Combine(folder, $"{i}{ext}")) ||
                            File.Exists(Path.Combine(folder, $"{i:00}{ext}")))
                        {
                            count++;
                            break;
                        }
                    }
                }
            }
            else // SplitFlap
            {
                // Check 00-59
                for (int i = 0; i < 60; i++)
                {
                    foreach (var ext in extensions)
                    {
                        if (File.Exists(Path.Combine(folder, $"{i:00}{ext}")))
                        {
                            count++;
                            break;
                        }
                    }
                }
            }
            return count;
        }

        private void ShowStatus(string msg, Color color)
        {
            ImportStatusText.Text = msg;
            ImportStatusText.Foreground = new SolidColorBrush(color);

            // Tooltip allows reading full message if it gets truncated
            ImportStatusText.ToolTip = msg;
        }


        // ==========================================
        // FOLDER SELECTION (Unchanged logic logic)
        // ==========================================
        private void ButtonSelectFlipFolder_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not FlipDisplayItem flip || flip.Profile == null) return;

            using var dialog = new System.Windows.Forms.FolderBrowserDialog();
            if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;

            string source = dialog.SelectedPath;
            string destRoot = Path.Combine(AppPaths.Assets, flip.Profile.Guid.ToString());
            Directory.CreateDirectory(destRoot);

            // Copy all valid images (naive copy of everything relevant)
            foreach (var file in Directory.GetFiles(source))
            {
                string name = Path.GetFileNameWithoutExtension(file);
                if (int.TryParse(name, out _)) // Copy anything that looks like a number
                {
                    string dest = Path.Combine(destRoot, Path.GetFileName(file));
                    if (!File.Exists(dest)) File.Copy(file, dest);
                }
            }


            flip.ImageFolder = "."; // Relative path

            // ✅ STRICT IMAGE DETECTION (Fixes "Half Width" Issue)
            string? firstImage = null;
            string[] exts = { ".png", ".jpg", ".jpeg", ".bmp" };

            if (flip.FlipStyle == FlipStyle.SingleDigit)
            {
                // SingleDigit: Prioritize "0.png", "1.png" (Single digit filenames)
                foreach (var ext in exts)
                {
                    if (File.Exists(Path.Combine(destRoot, "0" + ext))) { firstImage = Path.Combine(destRoot, "0" + ext); break; }
                    if (File.Exists(Path.Combine(destRoot, "1" + ext))) { firstImage = Path.Combine(destRoot, "1" + ext); break; }
                }
            }
            else // SplitFlap
            {
                // SplitFlap: Prioritize "00.png", "10.png" (Double digit filenames)
                foreach (var ext in exts)
                {
                    if (File.Exists(Path.Combine(destRoot, "00" + ext))) { firstImage = Path.Combine(destRoot, "00" + ext); break; }
                    if (File.Exists(Path.Combine(destRoot, "01" + ext))) { firstImage = Path.Combine(destRoot, "01" + ext); break; }
                    if (File.Exists(Path.Combine(destRoot, "10" + ext))) { firstImage = Path.Combine(destRoot, "10" + ext); break; }
                }
            }

            // Fallback: If strict search failed, just take ANY image (better than nothing)
            if (firstImage == null)
            {
                firstImage = Directory.GetFiles(destRoot, "*.png").FirstOrDefault()
                          ?? Directory.GetFiles(destRoot, "*.jpg").FirstOrDefault();
            }

            // ✅ CALCULATE WIDTH
            if (firstImage != null)
            {
                try
                {
                    using var stream = File.OpenRead(firstImage);
                    var frame = BitmapFrame.Create(stream, BitmapCreateOptions.DelayCreation, BitmapCacheOption.None);

                    int imgW = frame.PixelWidth;
                    int imgH = frame.PixelHeight;

                    if (flip.FlipStyle == FlipStyle.SingleDigit)
                    {
                        // SingleDigit: We expect imgW to be ~60px. Total = 120 + spacing
                        flip.Width = (imgW * 2) + flip.DigitSpacing;
                    }
                    else
                    {
                        // SplitFlap: We expect imgW to be ~120px. Total = 120
                        flip.Width = imgW;
                    }

                    flip.Height = imgH;
                }
                catch { }
            }

            // Trigger status check & preview
            CheckImportStatus(flip);
            LoadExistingPreview(flip);

        }

        // ==========================================
        // PREVIEW LOGIC
        // ==========================================
        private void LoadExistingPreview(FlipDisplayItem flip)
        {
            try
            {
                string folder = flip.CalculatedImageFolder;
                if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
                {
                    ClearPreview();
                    return;
                }

                string? imgPath = null;
                var exts = new[] { ".png", ".jpg", ".jpeg" };

                if (flip.FlipStyle == FlipStyle.SingleDigit)
                {
                    // Try single digits first (0, 1...)
                    foreach (var ext in exts)
                    {
                        if (File.Exists(Path.Combine(folder, "0" + ext))) { imgPath = Path.Combine(folder, "0" + ext); break; }
                        if (File.Exists(Path.Combine(folder, "1" + ext))) { imgPath = Path.Combine(folder, "1" + ext); break; }
                        // Fallback to 00 if 0 is missing (common mistake)
                        if (File.Exists(Path.Combine(folder, "00" + ext))) { imgPath = Path.Combine(folder, "00" + ext); break; }
                    }
                }
                else // SplitFlap
                {
                    // Try double digits (00, 10...)
                    foreach (var ext in exts)
                    {
                        if (File.Exists(Path.Combine(folder, "00" + ext))) { imgPath = Path.Combine(folder, "00" + ext); break; }
                        if (File.Exists(Path.Combine(folder, "10" + ext))) { imgPath = Path.Combine(folder, "10" + ext); break; }
                    }
                }

                if (imgPath != null)
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.UriSource = new Uri(imgPath);
                    bitmap.EndInit();
                    bitmap.Freeze();
                    PreviewImage.Source = bitmap;
                    PreviewText.Text = Path.GetFileName(imgPath);
                }
                else
                {
                    ClearPreview();
                }
            }
            catch { ClearPreview(); }
        }

        private void ClearPreview()
        {
            PreviewImage.Source = null;
            PreviewText.Text = "No preview";
        }
    }
}
