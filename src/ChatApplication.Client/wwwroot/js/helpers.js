function scrollToBottom(id) {
    const element = document.getElementById(id);
    if (element) {
        element.scrollTop = element.scrollHeight;
    }
}

// In case the app needs other helpers from old version
window.scrollToBottom = scrollToBottom;
