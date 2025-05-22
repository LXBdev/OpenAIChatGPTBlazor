// Handles paste event for images and sends the image to .NET
export function registerPasteHandler(textAreaElement, dotNetHelper) {
    if (!textAreaElement || !dotNetHelper) return;
    textAreaElement.addEventListener('paste', async (event) => {
        if (event.clipboardData && event.clipboardData.items) {
            for (let i = 0; i < event.clipboardData.items.length; i++) {
                const item = event.clipboardData.items[i];
                if (item.kind === 'file' && item.type.startsWith('image/')) {
                    const file = item.getAsFile();
                    const arrayBuffer = await file.arrayBuffer();
                    const base64 = btoa(String.fromCharCode(...new Uint8Array(arrayBuffer)));
                    await dotNetHelper.invokeMethodAsync('ReceivePastedImage', file.name, base64, file.type);
                    event.preventDefault();
                    break;
                }
            }
        }
    });
}

export function scrollElementToEnd (element) {
    element.scrollTop = element.scrollHeight;
}

export async function downloadFileFromStream (fileName, contentStreamReference) {
    const arrayBuffer = await contentStreamReference.arrayBuffer();
    const blob = new Blob([arrayBuffer]);
    const url = URL.createObjectURL(blob);
    const anchorElement = document.createElement('a');
    anchorElement.href = url;
    anchorElement.download = fileName ?? '';
    anchorElement.click();
    anchorElement.remove();
    URL.revokeObjectURL(url);
}
