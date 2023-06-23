using System.Collections.ObjectModel;
using System.ComponentModel;
using FlexRadioServices.Models;

namespace FlexRadioServices.Services;

public interface IFlexRadioService: INotifyPropertyChanged
{
    ObservableCollection<RadioProxy> DiscoveredRadios { get; set; }

    RadioProxy? ConnectedRadio { get; set; }

    void DisconnectSession();
    
    void ConnectToRadio(RadioProxy radio);

    void DisconnectRadio(RadioProxy radio);
}

