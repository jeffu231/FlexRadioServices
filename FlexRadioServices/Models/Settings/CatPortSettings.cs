namespace FlexRadioServices.Models.Settings;

public record CatPortSettings
{
    public List<PortSettings> PortSettings { get; set; } = new List<PortSettings>();
}