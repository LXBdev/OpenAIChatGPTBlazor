using System.Globalization;
using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;
using OpenAI;
using OpenAI.Chat;

namespace OpenAIChatGPTBlazor.Components.Pages
{
    public partial class Index : IAsyncDisposable
    {
        private const string SELECTED_MODEL = "SelectedModel";
        private const string IS_AUTOSCROLL_ENABLED = "IsAutoscrollEnabled";
        private const string CHAT_HISTORY = "ChatHistoryV1";
        private const string ROLE_SYSTEM = "system";
        private const string ROLE_USER = "user";
        private const string ROLE_ASSISTANT = "assistant";
        private List<ChatMessage> _chatMessages = new List<ChatMessage>();
        private CancellationTokenSource? _searchCancellationTokenSource;
        private string _warningMessage = string.Empty;
        private string _next = string.Empty;
        private string _stream = string.Empty;
        private bool _loading = true;
        private bool _isAutoscrollEnabled = true;
        private ElementReference _nextArea;
        private ElementReference _mainArea;
        private IJSObjectReference? _module;
        private DotNetObjectReference<Index>? _dotNetHelper;
        private bool _isTopRowToggled;
        private string _additionalTopRowClass = string.Empty;
        private string _SelectedOptionKey = string.Empty;
        private (string filename, BinaryData data, string mimeType)? _file = null;
        private string? _imagePreviewUrl;

        [Inject(Key = "OpenAi")]
        public OpenAIClient OpenAIClient { get; set; } = null!;

        [Inject]
        public IOptionsMonitor<List<OpenAIOptions>> OpenAIOptions { get; set; } = null!;

        [Inject]
        public ILogger<Index> Logger { get; set; } = null!;

        protected override void OnInitialized()
        {
            var options = OpenAIOptions.CurrentValue.FirstOrDefault();
            _SelectedOptionKey = options != null ? options.Key : _SelectedOptionKey;
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                // Dynamically load JavaScript with cache busting
                _module = await JS.InvokeAsync<IJSObjectReference>(
                    "import",
                    $"./Components/Pages/Index.razor.js?v={DateTime.Now.Ticks}"
                );
                _SelectedOptionKey =
                    await LocalStorage.GetItemAsync<string>(SELECTED_MODEL) ?? _SelectedOptionKey;
                _isAutoscrollEnabled =
                    await LocalStorage.GetItemAsync<bool?>(IS_AUTOSCROLL_ENABLED)
                    ?? _isAutoscrollEnabled;
                await InitiateChat();

                _loading = false;
                this.StateHasChanged();
                await _nextArea.FocusAsync();

                // Register paste handler for images
                _dotNetHelper = DotNetObjectReference.Create(this);
                if (_module != null)
                {
                    await _module.InvokeVoidAsync("registerPasteHandler", _nextArea, _dotNetHelper);
                }
            }

            // Highlight after load finished to avoid excessive flickering
            if (!_loading)
            {
                await JS.InvokeVoidAsync("window.Prism.highlightAll");
            }
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

        private void ToggleTopRow(MouseEventArgs e)
        {
            _isTopRowToggled = !_isTopRowToggled;
            _additionalTopRowClass = _isTopRowToggled ? "show-top-row" : "";
        }

        private async Task OnSettingsChanged()
        {
            await LocalStorage.SetItemAsync<bool>(IS_AUTOSCROLL_ENABLED, _isAutoscrollEnabled);
            await LocalStorage.SetItemAsync<string>(SELECTED_MODEL, _SelectedOptionKey);
        }

        [JSInvokable]
        public Task ReceivePastedImage(string fileName, string base64, string mimeType)
        {
            var buffer = Convert.FromBase64String(base64);
            _file = (fileName, new BinaryData(buffer), mimeType);
            _imagePreviewUrl = $"data:{mimeType};base64,{base64}";
            StateHasChanged();
            return Task.CompletedTask;
        }

        public void ClearFile()
        {
            _file = null;
            _imagePreviewUrl = null;
            StateHasChanged();
        }

        private async Task RunSearch()
        {
            try
            {
                _loading = true;
                this.StateHasChanged();

                if (_file == null)
                {
                    _chatMessages.Add(new UserChatMessage(_next));
                }
                else
                {
                    _chatMessages.Add(
                        new UserChatMessage(
                            ChatMessageContentPart.CreateTextPart(_next),
                            ChatMessageContentPart.CreateImagePart(
                                _file.Value.data,
                                _file.Value.mimeType
                            )
                        )
                    );
                    _file = null;
                }

                _next = string.Empty;

                _searchCancellationTokenSource = new CancellationTokenSource();
                var selectedOption =
                    OpenAIOptions.CurrentValue.FirstOrDefault(x => x.Key == _SelectedOptionKey)
                    ?? throw new InvalidOperationException("Selected model was not found.");

                var client = OpenAIClient.GetChatClient(selectedOption.DeploymentName);

                if (!selectedOption.HasSystemMessageSupport)
                {
                    // Not ideal solution, but will do for now
                    // Proper solution will be to use "developer" messages instead of system once they become available
                    _chatMessages = _chatMessages
                        .Select(x =>
                            x switch
                            {
                                SystemChatMessage message => new UserChatMessage(message.Content),
                                _ => x,
                            }
                        )
                        .ToList();
                }

                if (selectedOption.HasStreamingSupport)
                {
                    var updates = client.CompleteChatStreamingAsync(_chatMessages);
                    await foreach (StreamingChatCompletionUpdate update in updates)
                    {
                        foreach (ChatMessageContentPart updatePart in update.ContentUpdate)
                        {
                            _stream += updatePart.Text;
                            this.StateHasChanged();
                            if (_isAutoscrollEnabled && _module is not null)
                            {
                                await _module.InvokeVoidAsync("scrollElementToEnd", _mainArea);
                            }
                        }
                    }

                    _chatMessages.Add(new AssistantChatMessage(_stream));
                }
                else
                {
                    var result = await client.CompleteChatAsync(_chatMessages);
                    _chatMessages.Add(new AssistantChatMessage(result.Value.Content));
                }

                await StoreChatHistory();

                _loading = false;
                _stream = string.Empty;
                _warningMessage = string.Empty;
            }
            catch (TaskCanceledException)
                when (_searchCancellationTokenSource?.IsCancellationRequested == true)
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

        private async Task OnFileSelected(
            Microsoft.AspNetCore.Components.Forms.InputFileChangeEventArgs e
        )
        {
            var file = e.File;
            if (file != null)
            {
                var buffer = new byte[file.Size];
                using var stream = file.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024);
                int totalRead = 0;
                while (totalRead < buffer.Length)
                {
                    int read = await stream.ReadAsync(buffer, totalRead, buffer.Length - totalRead);
                    if (read == 0)
                        break;
                    totalRead += read;
                }
                _file = (file.Name, new BinaryData(buffer), file.ContentType);
                _imagePreviewUrl =
                    $"data:{file.ContentType};base64,{Convert.ToBase64String(buffer)}";
                StateHasChanged();
            }
        }

        private void AbortSearch()
        {
            try
            {
                if (
                    _searchCancellationTokenSource?.Token != null
                    && _searchCancellationTokenSource.Token.CanBeCanceled
                )
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
            _chatMessages.Remove(chatMessage);
        }

        private async void CopyMessageToNext(ChatMessage chatMessage)
        {
            _next = GetChatMessageContent(chatMessage);
            await _nextArea.FocusAsync();
        }

        private async Task DownloadConversation()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine("# ChatGPT Conversation");
            foreach (var message in _chatMessages)
            {
                sb.AppendLine($"## {GetChatMessageRole(message)}");
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

        private async Task ResetChat()
        {
            _chatMessages.Clear();
            _chatMessages.Add(
                new SystemChatMessage(
                    $"You are the assistant of a software engineer mainly working with .NET and Azure. Today is {DateTimeOffset.UtcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}."
                )
            );

            await StoreChatHistory();
        }

        private async Task StoreChatHistory()
        {
            var mapped = new List<MyChatMessage>();
            foreach (var item in _chatMessages)
            {
                var a = item switch
                {
                    SystemChatMessage message => new MyChatMessage(
                        ROLE_SYSTEM,
                        GetChatMessageContent(message)
                    ),
                    UserChatMessage message => new MyChatMessage(
                        ROLE_USER,
                        GetChatMessageContent(message)
                    ),
                    AssistantChatMessage message => new MyChatMessage(
                        ROLE_ASSISTANT,
                        GetChatMessageContent(message)
                    ),
                    _ => new MyChatMessage("", ""),
                };
                mapped.Add(a);
            }
            var json = JsonSerializer.Serialize(mapped);
            await LocalStorage.SetItemAsStringAsync(CHAT_HISTORY, json);
        }

        private async Task InitiateChat()
        {
            IList<ChatMessage> chat;
            try
            {
                var chatHistory = await LocalStorage.GetItemAsync<string>(CHAT_HISTORY) ?? "[]";
                chat = JsonToChat(chatHistory);
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Failed to load chat history. Resetting chat.");
                chat = new List<ChatMessage>();
            }
            if (chat.Count > 0)
            {
                _chatMessages.Clear();
                foreach (var item in chat)
                {
                    _chatMessages.Add(item);
                }
            }
            else
            {
                await ResetChat();
            }
        }

        private static string GetChatMessageContent(ChatMessage message)
        {
            return message.Content.FirstOrDefault()?.Text ?? "[No Text]";
        }

        private string GetChatMessageRole(ChatMessage message)
        {
            return message switch
            {
                SystemChatMessage => ROLE_SYSTEM,
                UserChatMessage => ROLE_USER,
                AssistantChatMessage => ROLE_ASSISTANT,
                _ => "unknown",
            };
        }

        private IList<ChatMessage> JsonToChat(string json)
        {
            List<ChatMessage> result = [];
            var messages = JsonSerializer.Deserialize<IList<MyChatMessage>>(json) ?? [];
            foreach (var item in messages)
            {
                ChatMessage a = item switch
                {
                    { role: ROLE_SYSTEM } message => new SystemChatMessage(message.message),
                    { role: ROLE_ASSISTANT } message => new AssistantChatMessage(message.message),
                    _ => new UserChatMessage(item.message),
                };

                result.Add(a);
            }

            return result;
        }

        record MyChatMessage(string role, string message);
    }
}
