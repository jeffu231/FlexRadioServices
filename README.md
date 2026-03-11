# FlexRadioService

An API for integrating with a Flex 6xxx radio.

Simple REST API for discovering radios and connecting / disconnecting.

## CAT

Simple CAT functions to support WSJT and simple loggers to get / set frequency mode information and command the radio into and out of TX. It supports the Kenwood and Flex commands. It should mostly work with anything like SmartCAT does. CAT TCP ports are configurable and can follow any slice along with the transmit or active slice. The number of exposed ports with slice / client configuration is very flexible. This closely mirrors the functionality of SmartCAT without the need for a windows ui or machine.

## MQTT

Provides for a MQTT broker connection to publish some of the core state changes that occur in the radio.

### Topics

#### Radio Level

* {root topic}/radios/{Radio Serial}/meters   - Radio metering information  
Ex. hamradio/flexradio/radios/0000-0000-6600-0123/meters/pa_temp 26.1875
* {root topic}/radios/{Radio Serial}/mox   - Radio transmit state  
Ex. hamradio/flexradio/radios/0000-0000-6600-0123/mox
* {root topic}/radios/{Radio Serial}/tx_info  - Radio transmit slice information when transmitting
Ex. hamradio/flexradio/radios/0000-0000-6600-0123/tx_info {"slice":"B","freq":28.458,"txAnt":"ANT1"}


#### Slice Level
* {root topic}/radios/{Radio Serial}/{Client Id}/slice/{Letter}/{Slice Property}  
Ex. hamradio/flexradio/radios/0000-0000-6600-0123/slice/B/freq 222.174
*  {root topic}/radios/{Radio Serial}/{Client Id}/slice/{Letter}/tx_info  - Per client transmit slice information  
Ex. hamradio/flexradio/radios/0000-0000-6600-0123/tx_info {"slice":"B","freq":28.458,"txAnt":"ANT1"}

## API

Provides many restful api endpoints to get the state of the radio like connected clients, overall slices, client slices as well as to be able to push spots to the radio. 

See the swagger for details. myhost.com/api/frs/swagger/index.html

## Full Duplex Mute issue

This voodoo is to work around an issue in the Flex when Full duplex is on the transmitting slice is not muted. If you have split paths on that slice for something like a transverter, you hear own audio delayed. Full duplex should always mute the transmit slice. So the logic checks if Full Duplex is on and the transmitting slice has a different RxAnt and TxAnt, then it mutes the slice on Tx if not muted and restores the state on Rx.

This has been written to solve some integration needs and is largely experimental at
this stage. The code functions for what has been implemented and will likely be expanded upon as further needs arise.

## Configuration

The appsettings.user.json in the source tree is a working example of the user settings to establish working CAT ports and MQTT configuration. This file needs to be located in /app/appsettings/appsettings.user.json in the container. The compose file has a volume created in that location to store the file so it can persist and be updated without modifying the source tree.
