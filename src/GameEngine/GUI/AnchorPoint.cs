namespace Nexus.GameEngine.GUI;

/// <summary>
/// Common anchor point presets used by UI elements.
/// Anchor coordinates are normalized in element space from -1 to 1 where
/// (-1, -1) = top-left, (0, 0) = center, (1, 1) = bottom-right.
/// </summary>
public static class AnchorPoint
{
	public static readonly Vector2D<float> TopLeft     = new(-1f, -1f);
	public static readonly Vector2D<float> TopCenter   = new(0f, -1f);
	public static readonly Vector2D<float> TopRight    = new(1f, -1f);

	public static readonly Vector2D<float> MiddleLeft  = new(-1f, 0f);
	public static readonly Vector2D<float> Center      = new(0f, 0f);
	public static readonly Vector2D<float> MiddleRight = new(1f, 0f);

	public static readonly Vector2D<float> BottomLeft   = new(-1f, 1f);
	public static readonly Vector2D<float> BottomCenter = new(0f, 1f);
	public static readonly Vector2D<float> BottomRight  = new(1f, 1f);

	public static bool TryParse(string? name, out Vector2D<float> value)
	{
		value = TopLeft;
		if (string.IsNullOrEmpty(name)) return false;

		switch (name.Trim().ToLowerInvariant())
		{
			case "topleft":
			case "top-left":
			case "top left":
				value = TopLeft; return true;

			case "topcenter":
			case "top-center":
			case "top center":
			case "topmiddle":
			case "top-middle":
			case "top middle":
				value = TopCenter; return true;

			case "topright":
			case "top-right":
			case "top right":
				value = TopRight; return true;

			case "middleleft":
			case "middle-left":
			case "middle left":
			case "centerleft":
			case "center-left":
			case "center left":
				value = MiddleLeft; return true;

			case "center":
			case "middle":
				value = Center; return true;

			case "middleright":
			case "middle-right":
			case "middle right":
			case "centerright":
			case "center-right":
			case "center right":
				value = MiddleRight; return true;

			case "bottomleft":
			case "bottom-left":
			case "bottom left":
				value = BottomLeft; return true;

			case "bottomcenter":
			case "bottom-center":
			case "bottom center":
			case "bottommiddle":
			case "bottom-middle":
			case "bottom middle":
				value = BottomCenter; return true;

			case "bottomright":
			case "bottom-right":
			case "bottom right":
				value = BottomRight; return true;

			default:
				return false;
		}
	}

	public static Vector2D<float> Parse(string? name)
	{
		if (TryParse(name, out var v)) return v;
		throw new ArgumentException($"Unknown AnchorPoint name: {name}");
	}
}