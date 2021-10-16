using Verse;
using System.Collections.Generic;
using static OwlBar.Mod_OwlBar;

namespace OwlBar
{
	public class PawnGroups : GameComponent
    {
        public PawnGroups(Game game) {}
	
		public Dictionary<int, bool> groupLeaders = new Dictionary<int, bool>(); //PawnLeaderID, bool is for if the group is expanded
		public Dictionary<int, int> groupMembers = new Dictionary<int, int>(); //PawnMemberID, PawnLeaderID

		public override void FinalizeInit()
		{
			pawnGroups = this;
		}
		public override void ExposeData()
		{
			Scribe_Collections.Look(ref groupLeaders, "groupLeaders", LookMode.Value, LookMode.Value);
			Scribe_Collections.Look(ref groupMembers, "groupMembers", LookMode.Value, LookMode.Value);
			base.ExposeData();
		}

		public void MakeLeader(int pawnID)
		{
			groupLeaders.Add(pawnID, false);
		}
		public void RemoveLeader(int pawnID)
		{
			//Remove all members first
			groupMembers.RemoveAll(x => x.Value == pawnID);

			groupLeaders.Remove(pawnID);
			fastColonistBar.ResetCache();
		}
		public void JoinGroup(int pawnID, int leaderID, int groupID)
		{
			groupMembers.Add(pawnID, leaderID);
			
			//Emulate a reorder request
			int from = -1, to = -1;
			foreach (var item in fastColonistBar.colonistBarCache)
			{
				if (item == null) continue;
				if (item.ID == pawnID) from = item.entryIndex;
				if (item.ID == leaderID) to = item.entryIndex + 1;
			}
			vanillaColonistBar.Reorder(from, to, groupID);

			fastColonistBar.ResetCache();
		}
		public void LeaveGroup(int pawnID)
		{
			groupMembers.Remove(pawnID);
			fastColonistBar.ResetCache();
		}
	}
}