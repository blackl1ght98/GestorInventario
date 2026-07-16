using System.ComponentModel.DataAnnotations;

namespace GestorInventario.Shared.DTOS.Configuration
{
    public class AppSettings
    {
        public const string SectionName = "App";

        
        public string BaseUrl { get; set; } = string.Empty;

     
        public string DockerUrl { get; set; } = string.Empty;

    }
}
