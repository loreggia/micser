export function draggable(element, left, top) {
    var isDragging;
    var origLeft;
    var origTop;
    var startLeft;
    var startTop;

    function handleMouseDown(e) {
        isDragging = true;
        startLeft = e.ClientX;
        startTop = e.ClientY;
        origLeft = e.offsetLeft;
        origTop = e.offsetTop;

        document.addEventListener("mousemove", handleMouseMove);
        document.addEventListener("mouseup", handleMouseUp);
    }

    function handleMouseMove(e) {
        if (!isDragging) {
            return;
        }

        const left = origLeft - (startLeft - e.clientX);
        const top = origTop - (startTop - e.clientY);
        element.style.left = left + "px";
        element.style.top = top + "px";
    }

    function handleMouseUp() {
        isDragging = false;
        DotNet.invokeMethodAsync()
    }

    element.addEventListener("mousedown", handleMouseDown);
    element.style.left = left + "px";
    element.style.top = top + "px";
}