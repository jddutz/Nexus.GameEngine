using Microsoft.Extensions.Logging;

namespace Nexus.GameEngine.Graphics;

public class VkSettings
{
    public bool ValidationEnabled { get; set; } = false;
    public string[] EnabledValidationLayers { get; set; } = ["*"];
}
