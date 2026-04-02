#if DEBUG
using RT.Servers;

namespace Zinga
{
    public partial class ZingaPropellerModule
    {
        private class AutoUpdatingCssWebsocket(ZingaSettings settings) : WebSocket
        {
            private readonly ZingaSettings _settings = settings;

            protected override void OnTextMessageReceived(string msg)
            {
                if (msg == "css")
                    SendMessage(File.ReadAllText(Path.Combine(_settings.ResourcesDir, "Puzzle.css")));
            }
        }
    }
}
#endif