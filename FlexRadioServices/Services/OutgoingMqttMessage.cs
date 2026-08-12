namespace FlexRadioServices.Services;

internal sealed record OutgoingMqttMessage(string Topic, string Payload, MqttMessageKind Kind);
