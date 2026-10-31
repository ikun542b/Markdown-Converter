using System.Runtime.InteropServices;

namespace MarkdownDocumentConverter.App.Services;

public sealed class SystemFontService
{
    private static readonly string[] FallbackFonts =
    {
        "Microsoft YaHei",
        "SimSun",
        "SimHei",
        "Arial",
        "Times New Roman",
        "Calibri",
    };

    private IReadOnlyList<string>? _fonts;

    public IReadOnlyList<string> GetInstalledFonts()
    {
        return _fonts ??= EnumerateFonts();
    }

    private static IReadOnlyList<string> EnumerateFonts()
    {
        try
        {
            var names = new SortedSet<string>(StringComparer.CurrentCultureIgnoreCase);
            var logFont = new LOGFONT
            {
                lfCharSet = 1,
                lfFaceName = string.Empty,
            };

            EnumFontFamExProc callback = (ref ENUMLOGFONTEX font, IntPtr textMetric, uint fontType, IntPtr parameter) =>
            {
                var name = font.elfLogFont.lfFaceName;

                if (!string.IsNullOrWhiteSpace(name) && !name.StartsWith('@'))
                {
                    names.Add(name);
                }

                return 1;
            };

            _ = EnumFontFamiliesEx(IntPtr.Zero, ref logFont, callback, IntPtr.Zero, 0);

            return names.Count > 0 ? names.ToList() : FallbackFonts.ToList();
        }
        catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException or MarshalDirectiveException)
        {
            return FallbackFonts.ToList();
        }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct LOGFONT
    {
        public int lfHeight;
        public int lfWidth;
        public int lfEscapement;
        public int lfOrientation;
        public int lfWeight;
        public byte lfItalic;
        public byte lfUnderline;
        public byte lfStrikeOut;
        public byte lfCharSet;
        public byte lfOutPrecision;
        public byte lfClipPrecision;
        public byte lfQuality;
        public byte lfPitchAndFamily;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string lfFaceName;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct ENUMLOGFONTEX
    {
        public LOGFONT elfLogFont;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string elfFullName;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string elfStyle;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string elfScript;
    }

    private delegate int EnumFontFamExProc(
        ref ENUMLOGFONTEX font,
        IntPtr textMetric,
        uint fontType,
        IntPtr parameter);

    [DllImport("gdi32.dll", CharSet = CharSet.Unicode)]
    private static extern int EnumFontFamiliesEx(
        IntPtr deviceContext,
        ref LOGFONT logFont,
        EnumFontFamExProc callback,
        IntPtr parameter,
        uint flags);
}
