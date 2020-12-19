using GameFramework.Event;
using System.Collections.Generic;
using Game.Hotfix;

namespace Game
{
    public static partial class NetDataEvent
    {
		/// <summary>
        /// 建築Reset更新
        /// </summary>
        public sealed class ResetBuildingEvent : GameEventArgs
        {
            public static readonly int EventId = typeof(ResetBuildingEvent).GetHashCode();
            public override int Id { get { return EventId; } }
            public int PosIndex = -1;
            public override void Clear() { PosIndex = -1; }
        }
	}
}