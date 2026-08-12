using System.ComponentModel;
using Flex.Smoothlake.FlexLib;
using FlexRadioServices.Models;

namespace FlexRadioServices.Services;

public abstract class ConnectedRadioServiceBase: BackgroundService
{
    private readonly FlexRadioService _flexRadioService;
    protected RadioProxy? ConnectedRadio;
    private readonly ILogger _logger;
    protected ConnectedRadioServiceBase(IFlexRadioService flexRadioService, ILogger logger)
    {
        _logger = logger;
        _flexRadioService = flexRadioService as FlexRadioService
            ?? throw new ArgumentException("Connected radio services require FlexRadioService.", nameof(flexRadioService));
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        InitializeConnectedRadio();
        _flexRadioService.ConnectedRadioChanged += FlexRadioServiceOnConnectedRadioChanged;

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await DoWorkAsync(stoppingToken).ConfigureAwait(false);
            }
        }
        finally
        {
            _flexRadioService.ConnectedRadioChanged -= FlexRadioServiceOnConnectedRadioChanged;
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
    
    private void FlexRadioServiceOnConnectedRadioChanged(object? sender, EventArgs e)
    {
        var previousRadio = ConnectedRadio;
        if (ConnectedRadio != null)
        {
            RemoveRadioListeners(ConnectedRadio);
        }

        ConnectedRadio = _flexRadioService.GetConnectedRadioHandle();

        if (ConnectedRadio != null)
        {
            AddRadioListeners(ConnectedRadio);
        }

        ConnectedRadioChanged(this, new ConnectedRadioEventArgs(previousRadio));
    }

    private void InitializeConnectedRadio()
    {
        var connectedRadio = _flexRadioService.GetConnectedRadioHandle();
        if (connectedRadio != null)
        {
            ConnectedRadio = connectedRadio;
            AddRadioListeners(ConnectedRadio);
            ConnectedRadioChanged(this, new ConnectedRadioEventArgs(null));
        }
    }
    
    private void AddRadioListeners(RadioProxy radio)
    {
        _logger.LogDebug("Adding radio listeners");
        radio.Radio.PropertyChanged += RadioOnPropertyChanged;
    }
    
    private void RemoveRadioListeners(RadioProxy radio)
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
