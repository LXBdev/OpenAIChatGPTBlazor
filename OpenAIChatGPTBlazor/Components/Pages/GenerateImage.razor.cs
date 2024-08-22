using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Azure.AI.OpenAI;
using OpenAI;
using OpenAIChatGPTBlazor.Components;

namespace OpenAIChatGPTBlazor.Components.Pages
{
    public partial class GenerateImage
    {
        private CancellationTokenSource? _searchCancellationTokenSource;
        private string _warningMessage = string.Empty;
        private bool _loading = true;
        private GenerateImageOptions _optionsComponent = new();

        private Uri? _imageUrl = null;
        private string _revisedPrompt = string.Empty;

        [Inject]
        public OpenAIClient OpenAIClient { get; set; } = null!;

        protected override void OnAfterRender(bool firstRender)
        {
            if (firstRender)
            {
                _loading = false;
                this.StateHasChanged();
            }
        }

        private async Task OnSubmitClick()
        {
            await RunSubmit();
        }

        private void OnAbortClick()
        {
            AbortSearch();
        }

        private async Task RunSubmit()
        {
            try
            {
                _loading = true;
                this.StateHasChanged();

                _searchCancellationTokenSource = new CancellationTokenSource();

                var imageClient = OpenAIClient.GetImageClient("Dalle3");
                // TODO Move prompt out of component into dedicated prompt component now?
                var res = await imageClient.GenerateImageAsync(_optionsComponent.Prompt, _optionsComponent.AsAzureOptions("Dalle3"), _searchCancellationTokenSource.Token);

                _imageUrl = res.Value.ImageUri;
                _revisedPrompt = res.Value.RevisedPrompt;

                _loading = false;
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
    }
}