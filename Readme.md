# :warning: This repository is no longer maintained :warning:

## Dolby.io Virtual World plugin for Unity Demo

This repository contains a demo application that uses the [Dolby.io Virtual World plugin for Unity](https://github.com/DolbyIO/comms-sdk-unity). 

## Configuration

The configuration of the demo application is located in the `config.json` file. The location of the file depends on the system you are using:

- Unity Editor: `<path_to_the_project_folder>/Assets/config.json`
- MacOS: `<path_to_the_player_app_bundle>/Contents/config.json`
- Windows: `<path_to_the_executablename_Data_folder>/config.json`

If the file does not exist, create it and add the following data to the file:

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

You can use this file to configure the demo application:

1. Provide the URL of your own token generation server in `token_server_url`.
2. Provide a customer access token from the [Dolby.io Dashboard](https://dashboard.dolby.io/).

## Media injection

If you want to inject media, whether it is audio or video, see more information in the [C++ SDK Media Injection Demo](https://github.com/dolbyio-samples/comms-cpp-injection-demo).

## Run

To run the application, clone the repository and open the scene in `Assets/Scenes/Playground.unity`.
