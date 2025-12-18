using System.Collections.Generic;

namespace ApiGigaChat.Models
{
    public class YandexGPTResult
    {
        public List<YandexGPTAlternative> alternatives { get; set; }
        public YandexGPTUsage usage { get; set; }
        public string modelVersion { get; set; }
    }
}
