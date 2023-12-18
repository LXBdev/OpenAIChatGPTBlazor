using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Azure.AI.OpenAI;
using System.Globalization;

namespace OpenAIChatGPTBlazor.Pages
{
    public partial class Index
    {
        private readonly ChatCompletionsOptions _chat = new ChatCompletionsOptions
        {
            Messages =
            {
                new ChatMessage(ChatRole.System, $"You are the assistant of a software engineer mainly working with .NET and Azure. Today is {DateTimeOffset.UtcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}.")
            }
        };
        private CancellationTokenSource ?_searchCancellationTokenSource;
        private string _warningMessage = string.Empty;
        private string _next = string.Empty;
        private string _stream = string.Empty;
        private bool _loading = true;
        private string _selectedModel = string.Empty;
        private string[] _selectableModels = new string[0];
        private ElementReference _mainArea;

        protected override void OnInitialized()
        {
            _selectableModels = OpenAIOptions.CurrentValue.SelectableModels?.Split(",") ?? _selectableModels;
            _selectedModel = _selectableModels.FirstOrDefault(_selectedModel);
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                _loading = false;
                this.StateHasChanged();
                await _mainArea.FocusAsync();
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
                _chat.Messages.Add(new ChatMessage(ChatRole.User, _next));
                _next = string.Empty;
                _searchCancellationTokenSource = new CancellationTokenSource();
                var res = await OpenAiClient.GetChatCompletionsStreamingAsync(_selectedModel, _chat, _searchCancellationTokenSource.Token);
                await foreach (var choice in res.Value.GetChoicesStreaming(_searchCancellationTokenSource.Token))
                {
                    await foreach (var msg in choice.GetMessageStreaming(_searchCancellationTokenSource.Token))
                    {
                        _stream += msg.Content;
                        this.StateHasChanged();
                        await JS.InvokeVoidAsync("scrollElementToEnd", _mainArea);
                    }
                }

                _chat.Messages.Add(new ChatMessage(ChatRole.Assistant, _stream.ToString()));
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

        private void AbortSearch(){
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

        private void DeleteMessage(ChatMessage chatMessage)
        {
            _chat.Messages.Remove(chatMessage);
        }

        private async void CopyMessageToNext(ChatMessage chatMessage)
        {
            _next = chatMessage.Content;
            await _mainArea.FocusAsync();
        }

        private async Task DownloadConversation()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine("# ChatGPT Conversation");
            foreach (var message in _chat.Messages)
            {
                sb.AppendLine($"## {message.Role}");
                sb.AppendLine(message.Content);
            }

            using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(sb.ToString()));
            var fileName = "conversation.md";
            using var streamRef = new DotNetStreamReference(stream);
            await JS.InvokeVoidAsync("downloadFileFromStream", fileName, streamRef);
        }
    }
}