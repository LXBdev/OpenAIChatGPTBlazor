using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Azure.AI.OpenAI;
using System.Globalization;

namespace OpenAIChatGPTBlazor.Pages
{
    public partial class GenerateImage
    {
        private CancellationTokenSource? _searchCancellationTokenSource;
        private string _warningMessage = string.Empty;
        private string _next = string.Empty;
        private bool _loading = true;
        private string[] _selectableModels = new string[0];
        private ElementReference _nextArea;

        private Uri? _imageUrl = null;
        private string _revisedPrompt = string.Empty;

        protected override void OnInitialized()
        {
            _selectableModels = OpenAIOptions.CurrentValue.SelectableModels?.Split(",") ?? _selectableModels;
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                _loading = false;
                this.StateHasChanged();
                await _nextArea.FocusAsync();
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

        private async Task OnNextKeydown(KeyboardEventArgs e)
        {
            if (e is { Key: "Enter" or "NumpadEnter", CtrlKey: true })
            {
                await RunSubmit();
            }
        }

        private async Task RunSubmit()
        {
            try
            {
                _loading = true;
                this.StateHasChanged();

                _searchCancellationTokenSource = new CancellationTokenSource();
                var res = await OpenAiClient.GetImageGenerationsAsync(new(_next) { DeploymentName = "Dalle3"}, _searchCancellationTokenSource.Token);

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