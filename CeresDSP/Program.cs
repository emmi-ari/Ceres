namespace CeresDSP
{
    internal static class Program
    {
        private static async Task Main()
        {
            Ceres ceres = new();
            await ceres.ConnectAsync();
            await Task.Delay(-1);
        }
    }
}