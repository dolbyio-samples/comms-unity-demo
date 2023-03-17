## Dolby.io Unity Virtual Worlds SDK Demo

This repository contains a demo using the [Dolby.io Unity Virtual Worlds SDK](https://github.com/DolbyIO/comms-sdk-unity). 

## Configuration

The configuration is done via a json file named `config.json` located at various locations based on which system you are using:

1. Unity Editor
`<path to project folder>/Assets/config.json`
2. MacOSX
`<path to player app bundle>/Contents/config.json`
3. Windows
`<path to executablename_Data folder>/config.json`

```json
{
    "token_server_url": "",
    "app_key": "",

    "pubnub": {
        "publish_key": "",
        "subscribe_key": "",
        "secret_key": ""
    }
}
```

## Run

Clone the repository and open the scene at `Asets/Scenes/Playground.unity`