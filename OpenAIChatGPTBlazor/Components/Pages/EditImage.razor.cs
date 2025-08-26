using System;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using OpenAI;
using OpenAI.Images;

namespace OpenAIChatGPTBlazor.Components.Pages
{
    public partial class EditImage : ComponentBase, IAsyncDisposable
    {
        [Inject(Key = "OpenAi_Image")]
        public OpenAIClient OpenAIClient { get; set; } = null!;

        private CancellationTokenSource? _editCancellationTokenSource;
        private string _warningMessage = string.Empty;
        private bool _loading = false;
        private EditImageOptions _optionsComponent = new();
        private IJSObjectReference? _module;

        private string _prompt = string.Empty;
        private IBrowserFile? _uploadedFile;
        private string? _originalImageDataUrl;
        private string? _editedImageDataUrl;
        private BinaryData? _editedImageData;

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                _module = await JS.InvokeAsync<IJSObjectReference>(
                    "import",
                    $"./Components/Pages/EditImage.razor.js?v={DateTime.Now.Ticks}"
                );
            }
        }

        private async Task OnFileSelected(InputFileChangeEventArgs e)
        {
            try
            {
                _warningMessage = string.Empty;
                _uploadedFile = e.File;

                if (_uploadedFile != null)
                {
                    // Validate file size (50 MB limit for GPT-image-1)
                    const long maxFileSize = 50 * 1024 * 1024; // 50 MB
                    if (_uploadedFile.Size > maxFileSize)
                    {
                        _warningMessage = "File size must be less than 50 MB.";
                        _uploadedFile = null;
                        return;
                    }

                    // Validate file type
                    if (!_uploadedFile.ContentType.StartsWith("image/"))
                    {
                        _warningMessage = "Please select a valid image file (PNG or JPG).";
                        _uploadedFile = null;
                        return;
                    }

                    // Read and display the uploaded image (read full stream)
                    using var stream = _uploadedFile.OpenReadStream(maxFileSize);
                    var buffer = new byte[_uploadedFile.Size];
                    int totalRead = 0;
                    while (totalRead < buffer.Length)
                    {
                        int read = await stream.ReadAsync(
                            buffer,
                            totalRead,
                            buffer.Length - totalRead
                        );
                        if (read == 0)
                            break;
                        totalRead += read;
                    }

                    var base64 = Convert.ToBase64String(buffer);
                    _originalImageDataUrl = $"data:{_uploadedFile.ContentType};base64,{base64}";

                    // Clear previous edit result
                    _editedImageDataUrl = null;
                    _editedImageData = null;
                }
            }
            catch (Exception ex)
            {
                _warningMessage = $"Error uploading file: {ex.Message}";
                _uploadedFile = null;
            }
            finally
            {
                StateHasChanged();
            }
        }

        private async Task OnEditClick() => await RunEdit();

        private async Task OnPromptKeydown(KeyboardEventArgs e)
        {
            if ((e.Key == "Enter" || e.Key == "NumpadEnter") && e.CtrlKey)
            {
                await RunEdit();
            }
        }

        private void OnAbortClick() => AbortEdit();

        private async Task RunEdit()
        {
            if (_uploadedFile == null || string.IsNullOrWhiteSpace(_prompt))
                return;

            try
            {
                _loading = true;
                _warningMessage = string.Empty;
                StateHasChanged();

                _editCancellationTokenSource?.Dispose();
                _editCancellationTokenSource = new CancellationTokenSource();

                // Read the image file (read full stream)
                const long maxFileSize = 50 * 1024 * 1024; // 50 MB
                using var stream = _uploadedFile.OpenReadStream(maxFileSize);
                var buffer = new byte[_uploadedFile.Size];
                int totalRead = 0;
                while (totalRead < buffer.Length)
                {
                    int read = await stream.ReadAsync(
                        buffer,
                        totalRead,
                        buffer.Length - totalRead,
                        _editCancellationTokenSource.Token
                    );
                    if (read == 0)
                        break;
                    totalRead += read;
                }
                var imageStream = new System.IO.MemoryStream(buffer);

                // Use the OpenAI SDK for image editing
                var imageClient = OpenAIClient.GetImageClient("gpt-image-1");

                // Create image edit options
                var editOptions = new ImageEditOptions()
                {
                    Size = _optionsComponent.GetImageSize(),
                    // ResponseFormat = GeneratedImageFormat.Bytes,
                };

                // Call the SDK method for image editing
                var result = await imageClient.GenerateImageEditAsync(
                    imageStream,
                    _uploadedFile.Name,
                    _prompt,
                    editOptions,
                    _editCancellationTokenSource.Token
                );

                if (result?.Value?.ImageBytes != null)
                {
                    _editedImageData = result.Value.ImageBytes;
                    var base64 = Convert.ToBase64String(_editedImageData.ToArray());
                    _editedImageDataUrl = $"data:image/png;base64,{base64}";
                }

                _loading = false;
            }
            catch (TaskCanceledException)
                when (_editCancellationTokenSource?.IsCancellationRequested == true)
            {
                // Gracefully handle cancellation
                _loading = false;
            }
            catch (Exception ex)
            {
                _warningMessage = ex.Message;
                _loading = false;
            }
            finally
            {
                StateHasChanged();
            }
        }

        private void AbortEdit()
        {
            try
            {
                if (
                    _editCancellationTokenSource?.Token != null
                    && _editCancellationTokenSource.Token.CanBeCanceled
                )
                {
                    _editCancellationTokenSource.Cancel();
                }
            }
            catch (Exception ex)
            {
                _warningMessage = ex.Message;
            }
        }

        private async Task DownloadEditedImage()
        {
            if (_editedImageData == null)
                return;

            try
            {
                var fileName = $"edited_image_{DateTime.Now:yyyyMMdd_HHmmss}.png";
                var streamRef = new DotNetStreamReference(_editedImageData.ToStream());

                if (_module is not null)
                {
                    await _module.InvokeVoidAsync("downloadFileFromStream", fileName, streamRef);
                }
                else
                {
                    _warningMessage = "JavaScript module not loaded. Please refresh the page.";
                }
            }
            catch (Exception ex)
            {
                _warningMessage = $"Error downloading image: {ex.Message}";
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (_module is not null)
            {
                await _module.DisposeAsync();
            }
        }
    }
}
