function scrollToBottom(element) {
    if (element) {
        setTimeout(() => {
            element.scrollTop = element.scrollHeight;
        }, 0);
    }
}
