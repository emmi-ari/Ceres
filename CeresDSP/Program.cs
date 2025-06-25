namespace CeresDSP
{
    internal static class Program
    {
        private static async Task Main()
        {
            Ceres ceres = new();
            try
            {
                await ceres.ConnectAsync();
            }
            catch (Exception ex)
            {
                ConsoleColor defaultColor = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"{ex.Message} (0x{ex.HResult:x8})");
                Console.ForegroundColor = defaultColor;
                return;
            }
            await Task.Delay(-1);
        }
    }
}