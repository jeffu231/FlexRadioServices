using FlexRadioServices.Models.Settings;
using MQTTnet;
using MQTTnet.Client;

namespace FlexRadioServices.Services;

public class MqttClientService : IMqttClientService
{
    private readonly IMqttClient _mqttClient;
    private readonly MqttClientOptions _options;
    private readonly ILogger<MqttClientService> _logger;

    public MqttClientService(MqttClientOptions options, ILogger<MqttClientService> logger)
    {
        this._options = options;
        _mqttClient = new MqttFactory().CreateMqttClient();
        _logger = logger;
        ConfigureMqttClient();
    }

    private void ConfigureMqttClient()
    {
        _mqttClient.ConnectedAsync += HandleConnectedAsync;
        _mqttClient.DisconnectedAsync += HandleDisconnectedAsync;
        _mqttClient.ApplicationMessageReceivedAsync += HandleApplicationMessageReceivedAsync;
    }

    public async Task Publish(string topic, string value)
    {
        var message = new MqttApplicationMessageBuilder()
            .WithTopic($"{AppSettings.MqttBrokerSettings.RootTopic}/{topic}")
            .WithPayload(value)
            .Build();
        if (_mqttClient.IsConnected)
        {
            await _mqttClient.PublishAsync(message, CancellationToken.None);
        }
    }

    public Task HandleApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs eventArgs)
    {
        //throw new System.NotImplementedException();
        return Task.CompletedTask;
    }

    public async Task HandleConnectedAsync(MqttClientConnectedEventArgs eventArgs)
    {
        _logger.LogInformation("MQTT Client connected");
        await Task.CompletedTask;
        //await mqttClient.SubscribeAsync("hello/world");
    }

    public async Task HandleDisconnectedAsync(MqttClientDisconnectedEventArgs eventArgs)
    {

        _logger.LogInformation("MQTT Client disconnected: {Reason}", eventArgs.Reason);

        #region Reconnect_Using_Event :https: //github.com/dotnet/MQTTnet/blob/master/Samples/Client/Client_Connection_Samples.cs

        /*
        * This sample shows how to reconnect when the connection was dropped.
        * This approach uses one of the events from the client.
        * This approach has a risk of dead locks! Consider using the timer approach (see sample).
        * The following reconnection code "Reconnect_Using_Timer" is recommended
       */
        //if (eventArgs.ClientWasConnected)
        //{
        //    // Use the current options as the new options.
        //    await mqttClient.ConnectAsync(mqttClient.Options);
        //}

        #endregion

        await Task.CompletedTask;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _mqttClient.ConnectAsync(_options, cancellationToken);
        
#region Reconnect_Using_Timer:https: //github.com/dotnet/MQTTnet/blob/master/Samples/Client/Client_Connection_Samples.cs

        /* 
         * This sample shows how to reconnect when the connection was dropped.
         * This approach uses a custom Task/Thread which will monitor the connection status.
        * This is the recommended way but requires more custom code!
       */
        _ = Task.Run(async () =>
            {
                // // User proper cancellation and no while(true).
                while (true)
                {
                    try
                    {
                        // This code will also do the very first connect! So no call to _ConnectAsync_ is required in the first place.
                        if (!await _mqttClient.TryPingAsync(cancellationToken))
                        {
                            await _mqttClient.ConnectAsync(_mqttClient.Options, CancellationToken.None);

                            // Subscribe to topics when session is clean etc.
                            _logger.LogInformation("The MQTT client is connected");
                        }
                    }
                    catch (Exception ex)
                    {
                        // Handle the exception properly (logging etc.).
                        _logger.LogError(ex, "The MQTT client  connection failed");
                    }
                    finally
                    {
                        // Check the connection state every 5 seconds and perform a reconnect if required.
                        await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
                    }
                }
            }, cancellationToken);

#endregion

    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            var disconnectOption = new MqttClientDisconnectOptions
            {
                Reason = MqttClientDisconnectOptionsReason.NormalDisconnection,
                ReasonString = "Normal Disconnection"
            };
            await _mqttClient.DisconnectAsync(disconnectOption, cancellationToken);
        }

        await _mqttClient.DisconnectAsync(cancellationToken: cancellationToken);
    }
}