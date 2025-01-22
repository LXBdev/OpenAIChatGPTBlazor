# OpenAIChatGPTBlazor

This is a Blazor WebAssembly application that uses OpenAI GPT-3.5 Turbo to enable a chat interface for generating human-like text. The application is designed to work with .NET and Azure, and it provides support for both local configurations and Azure App Configuration.

## Table of Contents

1. [Requirements](#Requirements)
2. [Installation](#Installation)
3. [Configuration](#Configuration)
   - [Local Configuration](#Local-Configuration)
   - [Azure App Configuration](#Azure-App-Configuration)
4. [Usage](#Usage)
5. [Contributing](#Contributing)
6. [License](#License)

## Requirements

- .NET 6.0 SDK
- Azure App Configuration (optional)

## Installation

1. Clone the repository:

```
git clone https://github.com/LXBdev/OpenAIChatGPTBlazor.git
```

2. Navigate to the `OpenAIChatGPTBlazor` folder:

```
cd OpenAIChatGPTBlazor
```

3. Restore the required packages:

```
dotnet restore
```

4. Build the application:

``` 
dotnet build
```

5. Run the application:

``` 
dotnet run
```

## Configuration

### Local Configuration

To use a local configuration, add user secrets with the following content:

```json
{
  "OpenAI": [
      {
        "DeploymentName": "gpt-4o",
        "Hint": "FastAndAccurate"
      }
  ],
  "ConnectionStrings": {
    //"OpenAi": "Endpoint=https://myoaiservice.openai.azure.com/;" // AAD Authentication
    //"OpenAi": "Endpoint=https://myoaiservice.openai.azure.com/;Key=xxx;"
  }
}
```

Provide your OpenAI API Key in the `Key` field, or remove the field to use AAD authentication.

### Azure App Configuration

To use a Azure App Configuration, add user secrets with the following content:

```json
{
  "AppConfig": {
    "Endpoint": "https://appcs-myinstance-weu.azconfig.io"
  }
}
```

Replace the `Endpoint` value with the URL of your Azure App Configuration instance. This is using AAD authentication.

## Usage

After configuring and running the application, open a web browser and navigate to `https://localhost:7128`. You should be able to use the chat interface to communicate with the GPT-3.5 Turbo model and generate human-like text.

## Contributing

We welcome contributions from the community! Feel free to submit Pull Requests and Issues to report bugs or request features.

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.
