# FlexRadioService

A utility API for integrating with Flex 6xxx radios. May work with 8xxx series, but has not been tested.

## REST

Simple REST API for discovering radios and connecting / disconnecting.

## CAT

Simple CAT functions to support WSJT and simple loggers to get / set frequency mode information and command the radio into and out of TX. It supports the Kenwood and Flex commands. It should mostly work with anything like SmartCAT does. CAT TCP ports are configurable and can follow any slice along with the transmit or active slice. The number of exposed ports with slice / client configuration is very flexible. This closely mirrors the functionality of SmartCAT without the need for a windows ui or machine.

## MQTT

Provides for a MQTT broker connection to publish some of the core state changes that occur in the radio.

## API

Provides many restful api endpoints to get the state of the radio like connected clients, overall slices, client slices as well as to be able to push spots to the radio.

## Full Duplex Mute issue

This feature provides for a work around for a bug in the radio firmware that causes the radio to to not mute a slice when using
split paths like a transverter. See the Wiki for details.

## Configuration

The service is configured via appsettings.user.json. See the Wiki for details.

## Docker

The application is packaged as a docker image on GHCR. Example docker-compose.yml is provided.

## Wiki

[Wiki](https://github.com/jeffu231/FlexRadioServices/wiki)
