using Verse;
using UnityEngine;

namespace OwlBar
{
	[StaticConstructorOnStartup]
	internal static class ResourceBank
	{
		public static readonly Texture2D iconHungry = ContentFinder<Texture2D>.Get("UI/Icons/ColonistBar/Hungry", true);
		public static readonly Texture2D iconTired = ContentFinder<Texture2D>.Get("UI/Icons/ColonistBar/Tired", true);
		public static readonly Texture2D iconBleeding = ContentFinder<Texture2D>.Get("UI/Icons/ColonistBar/Bleeding", true);
		public static readonly Texture2D groupExpand = ContentFinder<Texture2D>.Get("UI/Icons/ColonistBar/GroupExpand", true);
		public static readonly Texture2D groupCollapse = ContentFinder<Texture2D>.Get("UI/Icons/ColonistBar/GroupCollapse", true);
		public static readonly Texture2D portraitBackgroundWhite = ContentFinder<Texture2D>.Get("UI/Icons/ColonistBar/DesButBGWhite", true);
		public static string[] roles = new string[3] {"Leader", "Moralist", "Specialist"}; //ToDo: Should find a better way of targetting these...
		public static Color colorRed = new Color(1f, 0.25f, 0.25f, 1f);
		public static Color colorWhite = Color.white;
		public static Color colorYellow = Color.yellow;
		public static Color colorGreen = new Color(0.11f, 0.51f, 0.21f, 1f);
		public static Color colorClear = Color.clear;
		public static Color colorBorder = new Color(0.294f, 0.341f, 0.388f, 1f);
		public static Vector4 vector4Zero = Vector4.zero, vector4One = Vector4.one;
	}
}