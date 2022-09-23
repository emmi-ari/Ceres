namespace Ceres.Models
{
    internal class FrontMemberInfos
    {
        public string MemberName { get; init; }

        internal DateTimeOffset StartTime { get; init; }

        internal DateTimeOffset EndTime { get; init; }

        public FrontMemberInfos(string memberName, ulong? startTime, ulong? endTime)
        {
            MemberName = memberName ?? throw new ArgumentNullException(nameof(memberName));
            
            StartTime = DateTimeOffset.FromUnixTimeMilliseconds((long)(startTime ?? 0L));
            EndTime = DateTimeOffset.FromUnixTimeMilliseconds((long)(endTime ?? 0L));
        }
    }
}
