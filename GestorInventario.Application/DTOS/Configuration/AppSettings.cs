namespace GestorInventario.Application.DTOS.Configuration
{
    public record AppSettings
    {
        public const string SectionName = "App";
        public required string BaseUrl { get; init; } 
        public required string DockerUrl { get; init; } 

    }
}
