namespace Nexus.GameEngine.Runtime.Systems;

internal sealed class ContentSystem : IContentSystem
{
    internal IContentManager ContentManager { get; }

    public ContentSystem(IContentManager contentManager)
    {
        ContentManager = contentManager;
    }
}
