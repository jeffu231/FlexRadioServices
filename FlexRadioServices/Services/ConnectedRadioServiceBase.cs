using System.ComponentModel;
using Flex.Smoothlake.FlexLib;
using FlexRadioServices.Models;

namespace FlexRadioServices.Services;

internal abstract class ConnectedRadioServiceBase: BackgroundService
{
    private readonly IConnectedRadioCoordinator _connectedRadioCoordinator;
    protected RadioProxy? ConnectedRadio;
    private readonly ILogger _logger;
    protected ConnectedRadioServiceBase(IConnectedRadioCoordinator connectedRadioCoordinator, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(connectedRadioCoordinator);
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
        _connectedRadioCoordinator = connectedRadioCoordinator;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _connectedRadioCoordinator.ConnectedRadioChanged += ConnectedRadioCoordinatorOnConnectedRadioChanged;
        InitializeConnectedRadio();

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await DoWorkAsync(stoppingToken).ConfigureAwait(false);
            }
        }
        finally
        {
            _connectedRadioCoordinator.ConnectedRadioChanged -= ConnectedRadioCoordinatorOnConnectedRadioChanged;
            if (ConnectedRadio is not null)
            {
                RemoveRadioListeners(ConnectedRadio);
                ConnectedRadioChanged(this, new ConnectedRadioEventArgs(ConnectedRadio));
                ConnectedRadio = null;
            }
        }
    }

    protected virtual async Task DoWorkAsync(CancellationToken cancellationToken)
    {
        //_logger.LogDebug("In Connected Radio Services Do Work");
        await Task.Delay(5000, cancellationToken);
    } 
    
    private void ConnectedRadioCoordinatorOnConnectedRadioChanged(object? sender, ConnectedRadioTransition transition)
    {
        if (ConnectedRadio != null)
        {
            RemoveRadioListeners(ConnectedRadio);
        }

        ConnectedRadio = transition.CurrentRadio;

        if (ConnectedRadio != null)
        {
            AddRadioListeners(ConnectedRadio);
        }

        ConnectedRadioChanged(this, new ConnectedRadioEventArgs(transition.PreviousRadio));
    }

    private void InitializeConnectedRadio()
    {
        var connectedRadio = _connectedRadioCoordinator.GetConnectedRadioHandle();
        if (connectedRadio != null)
        {
            ConnectedRadio = connectedRadio;
            AddRadioListeners(ConnectedRadio);
            ConnectedRadioChanged(this, new ConnectedRadioEventArgs(null));
        }
    }
    
    protected virtual void AddRadioListeners(RadioProxy radio)
    {
        _logger.LogDebug("Adding radio listeners");
        radio.Radio.PropertyChanged += RadioOnPropertyChanged;
    }
    
    protected virtual void RemoveRadioListeners(RadioProxy radio)
    {
        _logger.LogDebug("Removing radio listeners");
        radio.Radio.PropertyChanged -= RadioOnPropertyChanged;
    }

    protected virtual void RadioOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        
    }

    protected virtual void ConnectedRadioChanged(object? sender, ConnectedRadioEventArgs args)
    {
        
    }

    protected class ConnectedRadioEventArgs:EventArgs
    {
        public ConnectedRadioEventArgs(RadioProxy? previousRadio)
        {
            PreviousRadio = previousRadio;
        }
    
        public RadioProxy? PreviousRadio { get; init; }
    }
    
    protected virtual Slice? TransmitSlice
    {
        get
        {
            if (ConnectedRadio != null)
            {
                foreach (var slice in ConnectedRadio.Radio.SliceList)
                {
                    if (slice.IsTransmitSlice)
                    {
                        return slice;
                    }
                }
            }

            return null;
        }
    }

    protected virtual Slice? ActiveSlice
    {
        get
        {
            if (ConnectedRadio != null)
            {
                foreach (var slice in ConnectedRadio.Radio.SliceList)
                {
                    if (slice.Active)
                    {
                        return slice;
                    }
                }
            }

            return null;
        }
    }
    
    
}
