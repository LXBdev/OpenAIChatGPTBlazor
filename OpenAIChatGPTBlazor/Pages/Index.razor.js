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

let dropArea = document.getElementById("drop-area");

dropArea.addEventListener("click", () => {
  fileElem.click();
});