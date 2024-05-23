using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using OpenAIChatGPTBlazor.Components;
using Azure.AI.OpenAI;

namespace OpenAIChatGPTBlazor.Pages
{
    public partial class GenerateImage
    {
        private CancellationTokenSource? _searchCancellationTokenSource;
        private string _warningMessage = string.Empty;
        private string _next = string.Empty;
        private bool _loading = true;
        private GenerateImageOptions _optionsComponent = new();

        private Uri? _imageUrl = null;
        private string _revisedPrompt = string.Empty;

        [Inject]
        public IDictionary<string, OpenAIClient> OpenAIClients { get; set; } = new Dictionary<string, OpenAIClient>();

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
                // TODO HACK
                var res = await OpenAIClients.First().Value.GetImageGenerationsAsync(_optionsComponent.AsAzureOptions("Dalle3"), _searchCancellationTokenSource.Token);

                foreach (var imageData in res.Value.Data)
                {
                    _imageUrl = imageData.Url;
                    _revisedPrompt = imageData.RevisedPrompt;
                }

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