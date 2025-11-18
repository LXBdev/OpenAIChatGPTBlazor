// Video generation utility functions

window.downloadFile = (url, filename) => {
    const link = document.createElement('a');
    link.href = url;
    link.download = filename;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
};

// Video player utilities
window.videoUtils = {
    // Set video playback speed
    setPlaybackSpeed: (videoElement, speed) => {
        if (videoElement) {
            videoElement.playbackRate = speed;
        }
    },

    // Get video metadata
    getVideoMetadata: (videoElement) => {
        if (videoElement) {
            return {
                duration: videoElement.duration,
                currentTime: videoElement.currentTime,
                width: videoElement.videoWidth,
                height: videoElement.videoHeight,
                paused: videoElement.paused
            };
        }
        return null;
    },

    // Capture video frame as image
    captureFrame: (videoElement) => {
        if (videoElement) {
            const canvas = document.createElement('canvas');
            canvas.width = videoElement.videoWidth;
            canvas.height = videoElement.videoHeight;
            const ctx = canvas.getContext('2d');
            ctx.drawImage(videoElement, 0, 0, canvas.width, canvas.height);
            return canvas.toDataURL('image/png');
        }
        return null;
    }
};
