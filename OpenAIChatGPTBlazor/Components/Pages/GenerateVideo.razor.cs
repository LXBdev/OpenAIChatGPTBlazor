using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using OpenAIChatGPTBlazor.Services;
using OpenAIChatGPTBlazor.Services.Models;

namespace OpenAIChatGPTBlazor.Components.Pages;

public partial class GenerateVideo : ComponentBase, IDisposable
{
    private CancellationTokenSource? _cancellationTokenSource;
    private string _warningMessage = string.Empty;
    private string _successMessage = string.Empty;
    private bool _loading = false;

    private string _prompt = string.Empty;
    private int _width = 720;
    private int _height = 1280;
    private int _nSeconds = 4;

    private readonly List<VideoGeneration> _videoResults = new();
    private readonly List<byte[]> _videoBytes = new();

    // Job tracking
    private string _currentJobId = string.Empty;
    private string _jobStatus = "Unknown";
    private int _jobProgress = 0;

    [Inject]
    public required IVideoGenerationService VideoService { get; set; }

    protected override void OnInitialized()
    {
        _loading = false;
    }

    private async Task OnSubmitClick() => await RunSubmit();

    private async Task OnPromptKeydown(KeyboardEventArgs e)
    {
        if ((e.Key == "Enter" || e.Key == "NumpadEnter") && e.CtrlKey)
        {
            await RunSubmit();
        }
    }

    private void OnAbortClick() => AbortGeneration();

    private async Task RunSubmit()
    {
        if (string.IsNullOrWhiteSpace(_prompt))
        {
            _warningMessage = "Please enter a prompt for video generation.";
            return;
        }

        try
        {
            _loading = true;
            _warningMessage = string.Empty;
            _successMessage = string.Empty;
            _videoResults.Clear();
            _videoBytes.Clear();
            _currentJobId = string.Empty;
            _jobStatus = "Starting...";
            _jobProgress = 0;

            StateHasChanged();

            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = new CancellationTokenSource();

            // Create video generation request
            var request = new VideoGenerationRequest
            {
                Prompt = _prompt,
                Width = _width,
                Height = _height,
                NSeconds = _nSeconds,
                Model = "sora-2",
            };

            // Submit the job
            var jobResponse = await VideoService.CreateVideoJobAsync(
                request,
                _cancellationTokenSource.Token
            );
            _currentJobId = jobResponse.JobId;
            _jobStatus = jobResponse.Status;

            StateHasChanged();

            // Start polling for job completion
            await PollJobStatus();
        }
        catch (TaskCanceledException)
            when (_cancellationTokenSource?.IsCancellationRequested == true)
        {
            _warningMessage = "Video generation was cancelled.";
        }
        catch (Exception ex)
        {
            _warningMessage = $"Error generating video: {ex.Message}";
        }
        finally
        {
            _loading = false;
            StateHasChanged();
        }
    }

    private async Task PollJobStatus()
    {
        const int maxPollingAttempts = 120; // 10 minutes with 5-second intervals
        var attempts = 0;

        while (
            attempts < maxPollingAttempts
            && !_cancellationTokenSource!.Token.IsCancellationRequested
        )
        {
            try
            {
                var statusResponse = await VideoService.GetJobStatusAsync(
                    _currentJobId,
                    _cancellationTokenSource.Token
                );
                _jobStatus = statusResponse.Status;

                // Update progress from API response or estimate
                _jobProgress = statusResponse.Progress > 0
                    ? statusResponse.Progress
                    : EstimateProgress(statusResponse.Status, attempts, maxPollingAttempts);

                StateHasChanged();

                // Check if job completed - new API uses "completed" status
                if (statusResponse.Status == "succeeded" || statusResponse.Status == "completed")
                {
                    // New Sora-2 API: use the job ID directly as the video ID
                    if (statusResponse.Generations != null && statusResponse.Generations.Count > 0)
                    {
                        // Legacy format with generations array
                        _videoResults.AddRange(statusResponse.Generations);
                        _successMessage =
                            $"Video generation completed successfully! Generated {statusResponse.Generations.Count} video(s).";
                        await RetrieveVideoContent();
                    }
                    else
                    {
                        // New format: use job ID directly
                        var generation = new VideoGeneration
                        {
                            GenerationId = statusResponse.JobId,
                            Duration = double.TryParse(statusResponse.Seconds, out var sec) ? sec : 0,
                            Resolution = statusResponse.Size ?? "720x1280",
                            Format = "mp4"
                        };
                        _videoResults.Add(generation);
                        _successMessage = "Video generation completed successfully!";
                        await RetrieveVideoContent();
                    }
                    return;
                }
                else if (statusResponse.Status == "failed")
                {
                    _warningMessage = statusResponse.Error?.Message ?? "Video generation failed.";
                    return;
                }
                else if (statusResponse.Status == "cancelled")
                {
                    _warningMessage = "Video generation was cancelled.";
                    return;
                }

                // Continue polling for pending/running status
                await Task.Delay(5000, _cancellationTokenSource.Token);
                attempts++;
            }
            catch (TaskCanceledException)
                when (_cancellationTokenSource.Token.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                _warningMessage = $"Error checking job status: {ex.Message}";
                return;
            }
        }

        if (attempts >= maxPollingAttempts)
        {
            _warningMessage = "Video generation timed out. Please try again.";
        }
    }

    private async Task RetrieveVideoContent()
    {
        try
        {
            // Download video content for each generation
            foreach (var generation in _videoResults)
            {
                var videoContent = await VideoService.GetVideoContentAsync(
                    generation.GenerationId,
                    _cancellationTokenSource!.Token
                );
                _videoBytes.Add(videoContent);
            }

            _successMessage =
                $"Video generation completed! Downloaded {_videoBytes.Count} video(s)";
        }
        catch (Exception ex)
        {
            _warningMessage = $"Error retrieving video content: {ex.Message}";
        }
    }

    private static int EstimateProgress(string status, int attempts, int maxAttempts)
    {
        return status switch
        {
            "pending" => Math.Min(10, (attempts * 5)),
            "running" => Math.Min(90, 20 + (attempts * 60 / maxAttempts)),
            "succeeded" => 100,
            "failed" => 0,
            "cancelled" => 0,
            _ => Math.Min(50, attempts * 100 / maxAttempts),
        };
    }

    private void AbortGeneration()
    {
        try
        {
            _cancellationTokenSource?.Cancel();
            _loading = false;
            _jobStatus = "Cancelled";
            _warningMessage = "Video generation cancelled.";
        }
        catch (Exception ex)
        {
            _warningMessage = $"Error cancelling generation: {ex.Message}";
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _cancellationTokenSource?.Dispose();
        }
    }
}
