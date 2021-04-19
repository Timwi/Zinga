#if DEBUG
using System.IO;
using RT.Servers;

namespace Zinga
{
    public partial class ZingaPropellerModule
    {
        private class AutoUpdatingCssWebsocket : WebSocket
        {
            public AutoUpdatingCssWebsocket(ZingaSettings settings)
            {
                _settings = settings;
            }

            private readonly ZingaSettings _settings;

            protected override void onTextMessageReceived(string msg)
            {
                if (msg == "css")
                    SendMessage(File.ReadAllText(Path.Combine(_settings.ResourcesDir, "Puzzle.css")));
            }
        }
    }
}
#endif