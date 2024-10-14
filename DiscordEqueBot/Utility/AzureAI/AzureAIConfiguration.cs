namespace DiscordEqueBot.Utility.AzureAI;

public class AzureAIConfiguration
{
    public static string SectionName { get; set; } = "AzureAI";
    public ModelConfig[] Models { get; set; }
    public string? SelectedModel { get; set; }

    public static ModelConfig GetSelectedModel(ModelConfig[] Models, string? SelectedModel = null)
    {
        foreach (var model in Models)
        {
            if (model.Key == SelectedModel || model.Endpoint == SelectedModel ||
                model.GetDisplayName() == SelectedModel || SelectedModel == null)
            {
                return model;
            }
        }

        throw new Exception("Selected model not found");
    }

    public ModelConfig GetSelectedModel()
    {
        return GetSelectedModel(Models, SelectedModel);
    }

    public class ModelConfig
    {
        public string? DisplayName { get; set; }
        public string Endpoint { get; set; }
        public string Key { get; set; }
        public uint MaxOutput { get; set; } = 4096;
        public uint MaxInput { get; set; } = 4096;
        public bool IsVisionReady { get; set; } = false;


        public string GetDisplayName()
        {
            if (DisplayName != null)
            {
                return DisplayName;
            }

            // https://Llama-3-2-11B-Vision-Instruct-xe.eastus.models.ai.azure.com => Llama-3-2-11B-Vision-Instruct-xe
            var name = Endpoint.Split(".")[0];
            name = name.Replace("https://", "");
            name = name.Replace("http://", "");
            return name;
        }
    }
}