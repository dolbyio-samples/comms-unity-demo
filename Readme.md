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
    "client_access_token": "",

    "pubnub": {
        "publish_key": "",
        "subscribe_key": "",
        "secret_key": ""
    }
}
```

You can use this file to configure the demo app to work with:

1. Your own token generation server and provide the URL in `token_server_url`
2. A Customer access token grabed from the [Dolby.io Dashboard](https://dashboard.dolby.io/)

## Injecting Media

If you need injecting media, whether it is audio or video, have a look to the [C++ SDK Media Injection Demo](https://github.com/dolbyio-samples/comms-cpp-injection-demo)

## Run

Clone the repository and open the scene at `Asets/Scenes/Playground.unity`