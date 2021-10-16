using Verse;
using UnityEngine;

namespace OwlBar
{
	internal static class FastGUI
	{
		static Internal_DrawTextureArguments drawArguments = new Internal_DrawTextureArguments
		{
			leftBorder = 0,
			rightBorder = 0,
			topBorder = 0,
			bottomBorder = 0,
			leftBorderColor = Color.white,
			topBorderColor = Color.white,
			rightBorderColor = Color.white,
			bottomBorderColor = Color.white,
			cornerRadiuses = new Vector4(0f, 0f, 0f, 0f),
			smoothCorners = false,
			sourceRect = new Rect(0f, 0f, 1f, 1f),
			mat = GUI.roundedRectMaterial
		};
		public static float currentTransparency = 1f;
		public static void DrawTextureFast(Rect position, Texture image, Vector4 borderWidth, Color color)
		{
			//Basic
			drawArguments.screenRect = position;
			drawArguments.texture = image;

			//Borders
			drawArguments.borderWidths = borderWidth;

			//colors
			drawArguments.color = color;
			if (currentTransparency != 1f) drawArguments.color.a *= currentTransparency;
			Graphics.Internal_DrawTexture(ref drawArguments);
		}
	}
}