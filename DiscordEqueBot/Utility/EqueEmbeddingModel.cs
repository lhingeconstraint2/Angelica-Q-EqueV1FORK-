using LangChain.Providers.OpenAI;

namespace DiscordEqueBot.Utility;

public class EqueEmbeddingModel : OpenAiEmbeddingModel
{
    public EqueEmbeddingModel(OpenAiProvider provider) : base(provider, "text-embedding-3-small")
    {
    }
}