using Verse;
using UnityEngine;

namespace OwlBar
{
	[StaticConstructorOnStartup]
	internal static class ResourceBank
	{
		public static readonly Texture2D Icon_Hungry = ContentFinder<Texture2D>.Get("UI/Icons/ColonistBar/Hungry", true);
		public static readonly Texture2D Icon_Tired = ContentFinder<Texture2D>.Get("UI/Icons/ColonistBar/Tired", true);
		public static readonly Texture2D Group_Expand = ContentFinder<Texture2D>.Get("UI/Icons/ColonistBar/GroupExpand", true);
		public static readonly Texture2D Group_Collapse = ContentFinder<Texture2D>.Get("UI/Icons/ColonistBar/GroupCollapse", true);
		public static string[] roles = new string[3] {"Leader", "Moralist", "Specialist"}; //ToDo: Should find a better way of targetting these...
		public static Color colorRed = new Color(1f, 0.25f, 0.25f, 1f);
		public static Color colorWhite = Color.white;
		public static Color colorYellow = Color.yellow;
		public static Vector4 vector4Zero = Vector4.zero, vector4One = Vector4.one;
	}
}