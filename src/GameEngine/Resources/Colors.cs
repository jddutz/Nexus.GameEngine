using Silk.NET.Maths;

namespace Nexus.GameEngine.Resources;

/// <summary>
/// Standard color constants as Vector4D&lt;float&gt; values (RGBA format).
/// Each component ranges from 0.0 to 1.0.
/// </summary>
public static class Colors
{
    // Helper method to convert ARGB hex to Vector4D<float> RGBA
    public static Vector4D<float> FromArgb(uint argb)
    {
        var a = ((argb >> 24) & 0xFF) / 255.0f;
        var r = ((argb >> 16) & 0xFF) / 255.0f;
        var g = ((argb >> 8) & 0xFF) / 255.0f;
        var b = (argb & 0xFF) / 255.0f;
        return new Vector4D<float>(r, g, b, a);
    }

    public static Vector4D<float> Lerp(Vector4D<float> a, Vector4D<float> b, float t)
    {
        return new Vector4D<float>(
            a.X + (b.X - a.X) * t,
            a.Y + (b.Y - a.Y) * t,
            a.Z + (b.Z - a.Z) * t,
            a.W + (b.W - a.W) * t
        );
    }

    // Standard named colors
    public static Vector4D<float> Transparent => FromArgb(0x00FFFFFF);
    public static Vector4D<float> AliceBlue => FromArgb(0xFFF0F8FF);
    public static Vector4D<float> AntiqueWhite => FromArgb(0xFFFAEBD7);
    public static Vector4D<float> Aqua => FromArgb(0xFF00FFFF);
    public static Vector4D<float> Aquamarine => FromArgb(0xFF7FFFD4);
    public static Vector4D<float> Azure => FromArgb(0xFFF0FFFF);
    public static Vector4D<float> Beige => FromArgb(0xFFF5F5DC);
    public static Vector4D<float> Bisque => FromArgb(0xFFFFE4C4);
    public static Vector4D<float> Black => FromArgb(0xFF000000);
    public static Vector4D<float> BlanchedAlmond => FromArgb(0xFFFFEBCD);
    public static Vector4D<float> Blue => FromArgb(0xFF0000FF);
    public static Vector4D<float> BlueViolet => FromArgb(0xFF8A2BE2);
    public static Vector4D<float> Brown => FromArgb(0xFFA52A2A);
    public static Vector4D<float> BurlyWood => FromArgb(0xFFDEB887);
    public static Vector4D<float> CadetBlue => FromArgb(0xFF5F9EA0);
    public static Vector4D<float> Chartreuse => FromArgb(0xFF7FFF00);
    public static Vector4D<float> Chocolate => FromArgb(0xFFD2691E);
    public static Vector4D<float> Coral => FromArgb(0xFFFF7F50);
    public static Vector4D<float> CornflowerBlue => FromArgb(0xFF6495ED);
    public static Vector4D<float> Cornsilk => FromArgb(0xFFFFF8DC);
    public static Vector4D<float> Crimson => FromArgb(0xFFDC143C);
    public static Vector4D<float> Cyan => FromArgb(0xFF00FFFF);
    public static Vector4D<float> DarkBlue => FromArgb(0xFF00008B);
    public static Vector4D<float> DarkCyan => FromArgb(0xFF008B8B);
    public static Vector4D<float> DarkGoldenrod => FromArgb(0xFFB8860B);
    public static Vector4D<float> DarkGray => FromArgb(0xFFA9A9A9);
    public static Vector4D<float> DarkGreen => FromArgb(0xFF006400);
    public static Vector4D<float> DarkKhaki => FromArgb(0xFFBDB76B);
    public static Vector4D<float> DarkMagenta => FromArgb(0xFF8B008B);
    public static Vector4D<float> DarkOliveGreen => FromArgb(0xFF556B2F);
    public static Vector4D<float> DarkOrange => FromArgb(0xFFFF8C00);
    public static Vector4D<float> DarkOrchid => FromArgb(0xFF9932CC);
    public static Vector4D<float> DarkRed => FromArgb(0xFF8B0000);
    public static Vector4D<float> DarkSalmon => FromArgb(0xFFE9967A);
    public static Vector4D<float> DarkSeaGreen => FromArgb(0xFF8FBC8B);
    public static Vector4D<float> DarkSlateBlue => FromArgb(0xFF483D8B);
    public static Vector4D<float> DarkSlateGray => FromArgb(0xFF2F4F4F);
    public static Vector4D<float> DarkTurquoise => FromArgb(0xFF00CED1);
    public static Vector4D<float> DarkViolet => FromArgb(0xFF9400D3);
    public static Vector4D<float> DeepPink => FromArgb(0xFFFF1493);
    public static Vector4D<float> DeepSkyBlue => FromArgb(0xFF00BFFF);
    public static Vector4D<float> DimGray => FromArgb(0xFF696969);
    public static Vector4D<float> DodgerBlue => FromArgb(0xFF1E90FF);
    public static Vector4D<float> Firebrick => FromArgb(0xFFB22222);
    public static Vector4D<float> FloralWhite => FromArgb(0xFFFFFAF0);
    public static Vector4D<float> ForestGreen => FromArgb(0xFF228B22);
    public static Vector4D<float> Fuchsia => FromArgb(0xFFFF00FF);
    public static Vector4D<float> Gainsboro => FromArgb(0xFFDCDCDC);
    public static Vector4D<float> GhostWhite => FromArgb(0xFFF8F8FF);
    public static Vector4D<float> Gold => FromArgb(0xFFFFD700);
    public static Vector4D<float> Goldenrod => FromArgb(0xFFDAA520);
    public static Vector4D<float> Gray => FromArgb(0xFF808080);
    public static Vector4D<float> Green => FromArgb(0xFF008000);
    public static Vector4D<float> GreenYellow => FromArgb(0xFFADFF2F);
    public static Vector4D<float> Honeydew => FromArgb(0xFFF0FFF0);
    public static Vector4D<float> HotPink => FromArgb(0xFFFF69B4);
    public static Vector4D<float> IndianRed => FromArgb(0xFFCD5C5C);
    public static Vector4D<float> Indigo => FromArgb(0xFF4B0082);
    public static Vector4D<float> Ivory => FromArgb(0xFFFFFFF0);
    public static Vector4D<float> Khaki => FromArgb(0xFFF0E68C);
    public static Vector4D<float> Lavender => FromArgb(0xFFE6E6FA);
    public static Vector4D<float> LavenderBlush => FromArgb(0xFFFFF0F5);
    public static Vector4D<float> LawnGreen => FromArgb(0xFF7CFC00);
    public static Vector4D<float> LemonChiffon => FromArgb(0xFFFFFACD);
    public static Vector4D<float> LightBlue => FromArgb(0xFFADD8E6);
    public static Vector4D<float> LightCoral => FromArgb(0xFFF08080);
    public static Vector4D<float> LightCyan => FromArgb(0xFFE0FFFF);
    public static Vector4D<float> LightGoldenrodYellow => FromArgb(0xFFFAFAD2);
    public static Vector4D<float> LightGray => FromArgb(0xFFD3D3D3);
    public static Vector4D<float> LightGreen => FromArgb(0xFF90EE90);
    public static Vector4D<float> LightPink => FromArgb(0xFFFFB6C1);
    public static Vector4D<float> LightSalmon => FromArgb(0xFFFFA07A);
    public static Vector4D<float> LightSeaGreen => FromArgb(0xFF20B2AA);
    public static Vector4D<float> LightSkyBlue => FromArgb(0xFF87CEFA);
    public static Vector4D<float> LightSlateGray => FromArgb(0xFF778899);
    public static Vector4D<float> LightSteelBlue => FromArgb(0xFFB0C4DE);
    public static Vector4D<float> LightYellow => FromArgb(0xFFFFFFE0);
    public static Vector4D<float> Lime => FromArgb(0xFF00FF00);
    public static Vector4D<float> LimeGreen => FromArgb(0xFF32CD32);
    public static Vector4D<float> Linen => FromArgb(0xFFFAF0E6);
    public static Vector4D<float> Magenta => FromArgb(0xFFFF00FF);
    public static Vector4D<float> Maroon => FromArgb(0xFF800000);
    public static Vector4D<float> MediumAquamarine => FromArgb(0xFF66CDAA);
    public static Vector4D<float> MediumBlue => FromArgb(0xFF0000CD);
    public static Vector4D<float> MediumOrchid => FromArgb(0xFFBA55D3);
    public static Vector4D<float> MediumPurple => FromArgb(0xFF9370DB);
    public static Vector4D<float> MediumSeaGreen => FromArgb(0xFF3CB371);
    public static Vector4D<float> MediumSlateBlue => FromArgb(0xFF7B68EE);
    public static Vector4D<float> MediumSpringGreen => FromArgb(0xFF00FA9A);
    public static Vector4D<float> MediumTurquoise => FromArgb(0xFF48D1CC);
    public static Vector4D<float> MediumVioletRed => FromArgb(0xFFC71585);
    public static Vector4D<float> MidnightBlue => FromArgb(0xFF191970);
    public static Vector4D<float> MintCream => FromArgb(0xFFF5FFFA);
    public static Vector4D<float> MistyRose => FromArgb(0xFFFFE4E1);
    public static Vector4D<float> Moccasin => FromArgb(0xFFFFE4B5);
    public static Vector4D<float> NavajoWhite => FromArgb(0xFFFFDEAD);
    public static Vector4D<float> Navy => FromArgb(0xFF000080);
    public static Vector4D<float> OldLace => FromArgb(0xFFFDF5E6);
    public static Vector4D<float> Olive => FromArgb(0xFF808000);
    public static Vector4D<float> OliveDrab => FromArgb(0xFF6B8E23);
    public static Vector4D<float> Orange => FromArgb(0xFFFFA500);
    public static Vector4D<float> OrangeRed => FromArgb(0xFFFF4500);
    public static Vector4D<float> Orchid => FromArgb(0xFFDA70D6);
    public static Vector4D<float> PaleGoldenrod => FromArgb(0xFFEEE8AA);
    public static Vector4D<float> PaleGreen => FromArgb(0xFF98FB98);
    public static Vector4D<float> PaleTurquoise => FromArgb(0xFFAFEEEE);
    public static Vector4D<float> PaleVioletRed => FromArgb(0xFFDB7093);
    public static Vector4D<float> PapayaWhip => FromArgb(0xFFFFEFD5);
    public static Vector4D<float> PeachPuff => FromArgb(0xFFFFDAB9);
    public static Vector4D<float> Peru => FromArgb(0xFFCD853F);
    public static Vector4D<float> Pink => FromArgb(0xFFFFC0CB);
    public static Vector4D<float> Plum => FromArgb(0xFFDDA0DD);
    public static Vector4D<float> PowderBlue => FromArgb(0xFFB0E0E6);
    public static Vector4D<float> Purple => FromArgb(0xFF800080);
    public static Vector4D<float> Red => FromArgb(0xFFFF0000);
    public static Vector4D<float> RosyBrown => FromArgb(0xFFBC8F8F);
    public static Vector4D<float> RoyalBlue => FromArgb(0xFF4169E1);
    public static Vector4D<float> SaddleBrown => FromArgb(0xFF8B4513);
    public static Vector4D<float> Salmon => FromArgb(0xFFFA8072);
    public static Vector4D<float> SandyBrown => FromArgb(0xFFF4A460);
    public static Vector4D<float> SeaGreen => FromArgb(0xFF2E8B57);
    public static Vector4D<float> SeaShell => FromArgb(0xFFFFF5EE);
    public static Vector4D<float> Sienna => FromArgb(0xFFA0522D);
    public static Vector4D<float> Silver => FromArgb(0xFFC0C0C0);
    public static Vector4D<float> SkyBlue => FromArgb(0xFF87CEEB);
    public static Vector4D<float> SlateBlue => FromArgb(0xFF6A5ACD);
    public static Vector4D<float> SlateGray => FromArgb(0xFF708090);
    public static Vector4D<float> Snow => FromArgb(0xFFFFFAFA);
    public static Vector4D<float> SpringGreen => FromArgb(0xFF00FF7F);
    public static Vector4D<float> SteelBlue => FromArgb(0xFF4682B4);
    public static Vector4D<float> Tan => FromArgb(0xFFD2B48C);
    public static Vector4D<float> Teal => FromArgb(0xFF008080);
    public static Vector4D<float> Thistle => FromArgb(0xFFD8BFD8);
    public static Vector4D<float> Tomato => FromArgb(0xFFFF6347);
    public static Vector4D<float> Turquoise => FromArgb(0xFF40E0D0);
    public static Vector4D<float> Violet => FromArgb(0xFFEE82EE);
    public static Vector4D<float> Wheat => FromArgb(0xFFF5DEB3);
    public static Vector4D<float> White => FromArgb(0xFFFFFFFF);
    public static Vector4D<float> WhiteSmoke => FromArgb(0xFFF5F5F5);
    public static Vector4D<float> Yellow => FromArgb(0xFFFFFF00);
    public static Vector4D<float> YellowGreen => FromArgb(0xFF9ACD32);

    // Commonly used color aliases for convenience
    public static Vector4D<float> Clear => Transparent;
    public static Vector4D<float> Opaque => White;
}
