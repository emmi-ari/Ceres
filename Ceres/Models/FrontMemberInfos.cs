namespace Ceres.Models
{
    internal class FrontMemberInfos
    {
        public string MemberName { get; init; }

        internal DateTimeOffset StartTime { get; init; }

        internal DateTimeOffset EndTime { get; init; }

        public FrontMemberInfos(string memberName, long? startTime, long? endTime)
        {
            MemberName = memberName ?? throw new ArgumentNullException(nameof(memberName));
            
            StartTime = DateTimeOffset.FromUnixTimeMilliseconds(startTime ?? 0L);
            EndTime = DateTimeOffset.FromUnixTimeMilliseconds(endTime ?? 0L);
        }
    }
}
