using System.Text.Json;

namespace Nexus.GameEngine.Data;

public class CustomNamingPolicy : JsonNamingPolicy
{
    public override string ConvertName(string name)
    {
        return name.ToLower().Replace("_", "");
    }
}