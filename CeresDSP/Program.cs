using System.Diagnostics.CodeAnalysis;

namespace CeresDSP
{
    internal class Program
    {
        private static Task Main() => new Program().MainAsync();

        [SuppressMessage("Performance", "CA1822:Mark members as static")]
        private async Task MainAsync()
        {
            Ceres ceres = new();
            await ceres.ConnectAsync();
            await Task.Delay(-1);
        }
    }
}