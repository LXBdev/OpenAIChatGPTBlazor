using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Azure.AI.OpenAI;
using System.Globalization;

namespace OpenAIChatGPTBlazor.Pages
{
    public partial class Index : IAsyncDisposable
    {
        private const string SELECTED_MODEL = "SelectedModel";
        private const string IS_AUTOSCROLL_ENABLED = "IsAutoscrollEnabled";

        private readonly ChatCompletionsOptions _chat = new ChatCompletionsOptions
        {
            Messages =
            {
                new ChatRequestSystemMessage($"You are the assistant of a software engineer mainly working with .NET and Azure. Today is {DateTimeOffset.UtcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}.")
            }
        };
        private CancellationTokenSource? _searchCancellationTokenSource;
        private string _warningMessage = string.Empty;
        private string _next = string.Empty;
        private string _stream = string.Empty;
        private bool _loading = true;
        private string[] _selectableModels = new string[0];
        private bool _isAutoscrollEnabled = true;
        private ElementReference _nextArea;
        private ElementReference _mainArea;
        private IJSObjectReference? _module;
        private bool _isTopRowToggled;
        private string _additionalTopRowClass = string.Empty;

        protected override void OnInitialized()
        {
            _selectableModels = OpenAIOptions.CurrentValue.SelectableModels?.Split(",") ?? _selectableModels;
            _chat.DeploymentName = _selectableModels.FirstOrDefault();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                _module = await JS.InvokeAsync<IJSObjectReference>("import",
                    "./Pages/Index.razor.js");
                _chat.DeploymentName = await LocalStorage.GetItemAsync<string>(SELECTED_MODEL) ?? _chat.DeploymentName;
                _isAutoscrollEnabled = await LocalStorage.GetItemAsync<bool?>(IS_AUTOSCROLL_ENABLED) ?? _isAutoscrollEnabled;

                _loading = false;
                this.StateHasChanged();
                await _nextArea.FocusAsync();
            }
            if (!_loading)
            {
                // Highlight after load finished to avoid excessive flickering
                await JS.InvokeVoidAsync("window.Prism.highlightAll");
            }
        }

        private async Task OnSearchClick()
        {
            await RunSearch();
        }

        private void OnAbortClick()
        {
            AbortSearch();
        }

        private async Task OnNextKeydown(KeyboardEventArgs e)
        {
            if (e is { Key: "Enter" or "NumpadEnter", CtrlKey: true })
            {
                await RunSearch();
            }
        }

        private async Task RunSearch()
        {
            try
            {
                _loading = true;
                this.StateHasChanged();
                _chat.Messages.Add(new ChatRequestUserMessage(_next));
                _next = string.Empty;

                _searchCancellationTokenSource = new CancellationTokenSource();
                var res = await OpenAiClient.GetChatCompletionsStreamingAsync(_chat, _searchCancellationTokenSource.Token);
                await foreach (var choice in res.WithCancellation(_searchCancellationTokenSource.Token))
                {
                    _stream += choice.ContentUpdate;
                    this.StateHasChanged();
                    if (_isAutoscrollEnabled && _module is not null)
                    {
                        await _module.InvokeVoidAsync("scrollElementToEnd", _mainArea);
                    }
                }

                _chat.Messages.Add(new ChatRequestAssistantMessage(_stream));
                _loading = false;
                _stream = string.Empty;
                _warningMessage = string.Empty;
            }
            catch (TaskCanceledException) when (_searchCancellationTokenSource?.IsCancellationRequested == true)
            {
                // Gracefully handle cancellation
            }
            catch (Exception ex)
            {
                _warningMessage = ex.Message;
            }
            finally
            {
                _loading = false;
            }
        }

        private void AbortSearch()
        {
            try
            {
                if (_searchCancellationTokenSource?.Token != null && _searchCancellationTokenSource.Token.CanBeCanceled)
                {
                    _searchCancellationTokenSource.Cancel();
                }
            }
            catch (Exception ex)
            {
                _warningMessage = ex.Message;
            }
        }

        private void DeleteMessage(ChatRequestMessage chatMessage)
        {
            _chat.Messages.Remove(chatMessage);
        }

        private async void CopyMessageToNext(ChatRequestMessage chatMessage)
        {
            _next = GetChatMessageContent(chatMessage);
            await _nextArea.FocusAsync();
        }

        private async Task DownloadConversation()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine("# ChatGPT Conversation");
            foreach (var message in _chat.Messages)
            {
                sb.AppendLine($"## {message.Role}");
                sb.AppendLine(GetChatMessageContent(message));
            }

            using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(sb.ToString()));
            var fileName = "conversation.md";
            using var streamRef = new DotNetStreamReference(stream);

            if (_module is not null)
            {
                await _module.InvokeVoidAsync("downloadFileFromStream", fileName, streamRef);
            }
        }

        private static string GetChatMessageContent(ChatRequestMessage message)
        {
            return message switch
            {
                ChatRequestUserMessage userMessage => userMessage.Content,
                ChatRequestSystemMessage systemMessage => systemMessage.Content,
                ChatRequestAssistantMessage assistantMessage => assistantMessage.Content,
                _ => string.Empty
            };
        }

        private void ToggleTopRow(MouseEventArgs e)
        {
            _isTopRowToggled = !_isTopRowToggled;
            _additionalTopRowClass = _isTopRowToggled ? "show-top-row" : "";
        }

        private async Task OnSettingsChanged()
        {
            await LocalStorage.SetItemAsync<bool>(IS_AUTOSCROLL_ENABLED, _isAutoscrollEnabled);
            await LocalStorage.SetItemAsync<string>(SELECTED_MODEL, _chat.DeploymentName);
        }

        async ValueTask IAsyncDisposable.DisposeAsync()
        {
            if (_module is not null)
            {
                try
                {
                    await _module.DisposeAsync();
                }
                catch (JSDisconnectedException)
                {
                    // Thrown if user reloads the page, can be ignored. JS can't be called after SignalR has been disconnected
                }
            }
        }
    }
}