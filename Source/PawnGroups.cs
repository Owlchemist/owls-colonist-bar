using Verse;
using System.Collections.Generic;
using System.Linq;
using static OwlBar.Mod_OwlBar;

namespace OwlBar
{
	public class PawnGroups : GameComponent
    {
        public PawnGroups(Game game) {}
	
		public Dictionary<int, bool> groupLeaders = new Dictionary<int, bool>(); //PawnLeaderID, bool is for if the group is expanded
		public Dictionary<int, int> groupMembers = new Dictionary<int, int>(); //PawnMemberID, PawnLeaderID
		public Dictionary<int, int> groupCounts = new Dictionary<int, int>(); //LeaderID, member count
		public Dictionary<int, int> groupAbsentees = new Dictionary<int, int>(); //PawnMemberID, absent from PawnLeaderID's group

		public override void FinalizeInit()
		{
			OwlColonistBar._instance.pawnGroups = this;
		}
		public override void ExposeData()
		{
			Scribe_Collections.Look(ref groupLeaders, "groupLeaders", LookMode.Value, LookMode.Value);
			Scribe_Collections.Look(ref groupMembers, "groupMembers", LookMode.Value, LookMode.Value);
			Scribe_Collections.Look(ref groupCounts, "groupCounts", LookMode.Value, LookMode.Value);
			
			//Validate data
			var workingList = groupLeaders.Keys;
			foreach (var leader in workingList)
			{
				if (!groupMembers.ContainsKey(leader)) RemoveLeader(leader);
			}

			base.ExposeData();
		}

		public void MakeLeader(int pawnID)
		{
			groupLeaders.Add(pawnID, false);
			groupCounts.Add(pawnID, 1);
			OwlColonistBar._instance.ResetCache(Find.ColonistBar);
		}
		public void RemoveLeader(int pawnID)
		{
			//Remove all members first
			groupMembers.RemoveAll(x => x.Value == pawnID);

			groupLeaders.Remove(pawnID);
			groupCounts.Remove(pawnID);
			OwlColonistBar._instance.ResetCache(Find.ColonistBar);
		}
		public void JoinGroup(int pawnID, int leaderID, int groupID)
		{
			groupMembers.Add(pawnID, leaderID);
			++groupCounts[leaderID];
			
			//Emulate a reorder request
			int from = -1, to = -1;
			foreach (var item in OwlColonistBar._instance.colonistBarCache)
			{
				if (item == null) continue;
				if (item.ID == pawnID) from = item.entryIndex;
				if (item.ID == leaderID) to = item.entryIndex + 1;
			}
			Find.ColonistBar.Reorder(from, to, groupID);

			OwlColonistBar._instance.ResetCache(Find.ColonistBar);
		}
		public void LeaveGroup(int pawnID)
		{
			--groupCounts[groupMembers[pawnID]];
			groupMembers.Remove(pawnID);
			OwlColonistBar._instance.ResetCache(Find.ColonistBar);
		}
	}
}