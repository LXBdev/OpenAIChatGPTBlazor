using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using OpenAI;

namespace OpenAIChatGPTBlazor.Components.Pages
{
    public partial class GenerateImage
    {
        private CancellationTokenSource? _searchCancellationTokenSource;
        private string _warningMessage = string.Empty;
        private bool _loading = true;
        private GenerateImageOptions _optionsComponent = new();

        private string _prompt = string.Empty;

        private Uri? _imageUrl = null;
        private BinaryData? _imageBytes = null;
        private string _revisedPrompt = string.Empty;

        [Inject(Key = "OpenAi_Image")]
        public OpenAIClient OpenAIClient { get; set; } = null!;

        protected override void OnAfterRender(bool firstRender)
        {
            if (firstRender)
            {
                _loading = false;
                StateHasChanged();
            }
        }

        private async Task OnSubmitClick() => await RunSubmit();

        private async Task OnPromptKeydown(KeyboardEventArgs e)
        {
            if ((e.Key == "Enter" || e.Key == "NumpadEnter") && e.CtrlKey)
            {
                await RunSubmit();
            }
        }

        private void OnAbortClick() => AbortSearch();

        private async Task RunSubmit()
        {
            try
            {
                _loading = true;
                StateHasChanged();

                _searchCancellationTokenSource = new CancellationTokenSource();

                var imageClient = OpenAIClient.GetImageClient("gpt-image-1");
                var res = await imageClient.GenerateImageAsync(
                    _prompt,
                    _optionsComponent.AsAzureOptions("gpt-image-1"),
                    _searchCancellationTokenSource.Token
                );

                _imageUrl = res.Value.ImageUri;
                _imageBytes = res.Value.ImageBytes;
                _revisedPrompt = res.Value.RevisedPrompt;

                _loading = false;
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
    }
}
