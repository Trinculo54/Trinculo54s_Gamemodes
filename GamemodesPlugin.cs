using Impostor.Api.Events.Managers;
using Impostor.Api.Plugins;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Gamemodes
{
    [ImpostorPlugin(id: "Tech.Trinculo54.Infection")]
    public class GamemodesPlugin : PluginBase
    {
        private readonly ILogger<GamemodesPlugin> _logger;

        private readonly IEventManager _eventManager;

        private IDisposable _unregister;

        public GamemodesPlugin(ILogger<GamemodesPlugin> logger, IEventManager eventManager)
        {
            _logger = logger;
            _eventManager = eventManager;
        }

        public override ValueTask EnableAsync()
        {
            _logger.LogInformation("Loaded infection Gamemode");
            _unregister = _eventManager.RegisterListener(new GamemodesEventListener(_logger));
            return default;
        }

        public override ValueTask DisableAsync()
        {
            _logger.LogInformation("Unloaded infection Gamemode");
            _unregister.Dispose();
            return default;
        }
    }
}