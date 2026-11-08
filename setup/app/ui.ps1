# Window plumbing for setup.ps1: system glass, XAML loading, animation helpers.

Add-Type -AssemblyName PresentationFramework, PresentationCore, WindowsBase, System.Windows.Forms
Add-Type @"
using System;
using System.Runtime.InteropServices;
public static class FortsGlass {
  [StructLayout(LayoutKind.Sequential)] struct MARGINS { public int l, r, t, b; }
  [DllImport("dwmapi.dll")] static extern int DwmSetWindowAttribute(IntPtr h, int a, ref int v, int s);
  [DllImport("dwmapi.dll")] static extern int DwmExtendFrameIntoClientArea(IntPtr h, ref MARGINS m);
  [DllImport("user32.dll")] static extern int GetWindowLong(IntPtr h, int i);
  [DllImport("user32.dll")] static extern int SetWindowLong(IntPtr h, int i, int v);
  // Rounded corners and acrylic behind the whole window (Windows 11 22H2+).
  // Returns false where the system backdrop is not available.
  public static bool Apply(IntPtr h) {
    SetWindowLong(h, -16, GetWindowLong(h, -16) & ~0x80000);  // no WS_SYSMENU: no system caption buttons
    var m = new MARGINS { l = -1, r = -1, t = -1, b = -1 };
    DwmExtendFrameIntoClientArea(h, ref m);
    int v = 0; DwmSetWindowAttribute(h, 20, ref v, 4);         // light frame
    v = 2; DwmSetWindowAttribute(h, 33, ref v, 4);             // round corners
    if (Environment.OSVersion.Version.Build < 22621) return false;
    v = 3; return DwmSetWindowAttribute(h, 38, ref v, 4) == 0; // acrylic
  }
}
"@

function Load-Window([string]$dir, [hashtable]$tokens) {
    # The window is split into numbered XAML parts; tokens like {SPRING} are filled in here.
    $text = (Get-ChildItem -LiteralPath $dir -Filter "*.xaml" | Sort-Object Name |
        ForEach-Object { [IO.File]::ReadAllText($_.FullName, [Text.Encoding]::UTF8) }) -join "`n"
    foreach ($k in $tokens.Keys) { $text = $text.Replace("{$k}", $tokens[$k]) }
    if ($script:Lang -eq "en") {
        foreach ($k in $script:English.Keys) {
            $text = $text.Replace("=`"$k`"", "=`"" + [Security.SecurityElement]::Escape($script:English[$k]) + "`"")
        }
    }
    [xml]$xml = $text
    $window = [Windows.Markup.XamlReader]::Load([System.Xml.XmlNodeReader]::new($xml))
    $names = @{}
    foreach ($node in $xml.SelectNodes("//*[@*[local-name()='Name']]")) {
        $name = $node.Attributes | Where-Object { $_.LocalName -eq "Name" } | Select-Object -First 1 -ExpandProperty Value
        $names[$name] = $window.FindName($name)
    }
    return $window, $names
}

function Enable-Glass($window, $tint) {
    $window.Add_SourceInitialized({
        $h = [Windows.Interop.WindowInteropHelper]::new($window).Handle
        [Windows.Interop.HwndSource]::FromHwnd($h).CompositionTarget.BackgroundColor = [Windows.Media.Colors]::Transparent
        if (-not [FortsGlass]::Apply($h)) { $tint.Background = [Windows.Media.BrushConverter]::new().ConvertFrom("#F2F2F5") }
    }.GetNewClosure())
}

function Image-From([string]$file) {
    $bmp = [Windows.Media.Imaging.BitmapImage]::new()
    $bmp.BeginInit()
    $bmp.CacheOption = "OnLoad"
    $bmp.UriSource = [Uri]::new((Join-Path $assets $file))
    $bmp.EndInit()
    $bmp.Freeze()
    $bmp
}

function Tint-Image($bmp, [string]$hex) {
    # Preview of a coloured crosshair: the same multiply the installer applies to the DDS.
    if ($hex -eq "FFFFFF") { return $bmp }
    $src = [Windows.Media.Imaging.FormatConvertedBitmap]::new($bmp, [Windows.Media.PixelFormats]::Bgra32, $null, 0)
    $w = $src.PixelWidth; $h = $src.PixelHeight; $stride = $w * 4
    $px = New-Object byte[] ($stride * $h)
    $src.CopyPixels($px, $stride, 0)
    [FortsTint]::MultiplyBgra($px, 0, [Convert]::ToInt32($hex.Substring(0, 2), 16),
        [Convert]::ToInt32($hex.Substring(2, 2), 16), [Convert]::ToInt32($hex.Substring(4, 2), 16))
    $out = [Windows.Media.Imaging.WriteableBitmap]::new($w, $h, 96, 96, [Windows.Media.PixelFormats]::Bgra32, $null)
    $out.WritePixels([Windows.Int32Rect]::new(0, 0, $w, $h), $px, $stride, 0)
    $out.Freeze()
    $out
}

function Animate($target, [string]$property, $to, [double]$ms, [double]$from = [double]::NaN, [switch]$Spring) {
    # "Opacity" -> UIElement.OpacityProperty etc., found on the target's class or a base class.
    $field = $target.GetType().GetField("${property}Property", [Reflection.BindingFlags]"Public,Static,FlattenHierarchy")
    $anim = [Windows.Media.Animation.DoubleAnimation]::new()
    $anim.To = $to
    if (-not [double]::IsNaN($from)) { $anim.From = $from }
    $anim.Duration = [TimeSpan]::FromMilliseconds($(if ($reduceMotion) { 0 } else { $ms }))
    if ($Spring) {
        $ease = [Windows.Media.Animation.BackEase]::new(); $ease.Amplitude = 0.35
    } else {
        $ease = [Windows.Media.Animation.QuinticEase]::new()
    }
    $ease.EasingMode = "EaseOut"
    $anim.EasingFunction = $ease
    $target.BeginAnimation($field.GetValue($null), $anim)
}

function Flush { $window.Dispatcher.Invoke([action] {}, [Windows.Threading.DispatcherPriority]::Background) }
