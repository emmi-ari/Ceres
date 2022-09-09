namespace Ceres.HelperClasses
{
    public enum MessageLevel
    {
        Error = 0,
        Warn = 1,
        Info = 2,
        Debug = 3
    }

    public class LogMessage
    {
        public MessageLevel Level { get; }

        public DateTime Timestamp { get; }

        public string? Message { get; }

        public LogMessage(MessageLevel level, DateTime timeStamp, string message)
        {
            Level = level;
            Timestamp = timeStamp;
            Message = message;
        }

        public override string? ToString()
        {
            return $"[{Level}] [{Timestamp.Year}{Timestamp.Month}{Timestamp.Day} {Timestamp.Hour}:{Timestamp.Minute}:{Timestamp.Second}.{Timestamp.Millisecond}] {Message}";
        }
    }

    public class Logging
    {
        public static Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }
    }
}
