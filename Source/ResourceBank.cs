using Verse;
using UnityEngine;

namespace OwlBar
{
	[StaticConstructorOnStartup]
	internal static class ResourceBank
	{
		public static readonly Texture2D iconHungry = ContentFinder<Texture2D>.Get("UI/Icons/ColonistBar/Hungry", true),
			iconTired = ContentFinder<Texture2D>.Get("UI/Icons/ColonistBar/Tired", true),
			iconBleeding = ContentFinder<Texture2D>.Get("UI/Icons/ColonistBar/Bleeding", true),
			groupExpand = ContentFinder<Texture2D>.Get("UI/Icons/ColonistBar/GroupExpand", true),
			groupCollapse = ContentFinder<Texture2D>.Get("UI/Icons/ColonistBar/GroupCollapse", true),
			portraitBackgroundWhite = ContentFinder<Texture2D>.Get("UI/Icons/ColonistBar/DesButBGWhite", true);
		public static string[] roles = new string[3] {"Leader", "Moralist", "Specialist"}; //ToDo: Should find a better way of targetting these...
		public static readonly Color colorRed = new Color(1f, 0.25f, 0.25f, 1f),
			colorWhite = Color.white,
			colorYellow = Color.yellow,
			colorGreen = new Color(0.11f, 0.51f, 0.21f, 1f),
			colorClear = Color.clear,
			colorBorder = new Color(0.294f, 0.341f, 0.388f, 1f);
		public static readonly Vector4 vector4Zero = Vector4.zero, vector4One = Vector4.one;
	}
}