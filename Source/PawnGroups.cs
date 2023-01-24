using Verse;
using System.Collections.Generic;
using System.Linq;

namespace OwlBar
{
	public class PawnGroups : GameComponent
    {
        public PawnGroups(Game game)
		{
			if (OwlColonistBar._instance == null) new OwlColonistBar(this);
			else OwlColonistBar._instance.pawnGroups = this;
			groupLeaders = new Dictionary<int, bool>();
			groupMembers = new Dictionary<int, int>();
			groupCounts = new Dictionary<int, int>();
		}
	
		public Dictionary<int, bool> groupLeaders; //PawnLeaderID, bool is for if the group is expanded
		public Dictionary<int, int> groupMembers; //PawnMemberID, PawnLeaderID
		public Dictionary<int, int> groupCounts; //LeaderID, member count
		//public Dictionary<int, int> groupAbsentees; //PawnMemberID, absent from PawnLeaderID's group
		
		public override void FinalizeInit()
		{
			
		}
		
		public override void ExposeData()
		{
			Scribe_Collections.Look(ref groupLeaders, "groupLeaders", LookMode.Value, LookMode.Value);
			Scribe_Collections.Look(ref groupMembers, "groupMembers", LookMode.Value, LookMode.Value);
			Scribe_Collections.Look(ref groupCounts, "groupCounts", LookMode.Value, LookMode.Value);

			//Validate save data isn't empty
			if (Scribe.mode == LoadSaveMode.PostLoadInit)
			{
				if (groupLeaders == null) groupLeaders = new Dictionary<int, bool>();
				if (groupMembers == null) groupMembers = new Dictionary<int, int>();
				if (groupCounts == null) groupCounts = new Dictionary<int, int>();
			}

			base.ExposeData();
		}

		public void ValidateAllLeaders()
		{
			foreach (var leader in groupLeaders.Keys.ToList())
			{
				if (ValidateLeader(leader)) continue;

				Log.Warning("[Owl's Colonist Bar] removing pawnID #" + leader.ToString() + " as group leader. Did this pawn not load in on save reload?");
				RemoveLeader(leader);
			}
		}
		bool ValidateLeader(int pawnID)
		{
			foreach (var entry in Find.ColonistBar.cachedEntries)
			{
				if (entry.pawn?.thingIDNumber == pawnID) return true;
			}
			return false;
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
			LongEventHandler.QueueLongEvent(() => OwlColonistBar._instance.ResetCache(Find.ColonistBar), null, false, null);
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