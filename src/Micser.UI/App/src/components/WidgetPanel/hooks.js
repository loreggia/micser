import { useCallback, useState, useRef, useEffect } from "react";

export const useDragging = ({ onDragStart, onDrag, onDragEnd }) => {
    const elementRef = useRef();

    const [isDragging, setIsDragging] = useState();
    const [startPos, setStartPos] = useState();
    const [origPos, setOrigPos] = useState();

    const handleMouseDown = useCallback(
        (e) => {
            e.preventDefault();
            const element = e.currentTarget;
            elementRef.current = element;
            setOrigPos({ left: element.offsetLeft, top: element.offsetTop });
            setStartPos({ left: e.clientX, top: e.clientY });
            setIsDragging(true);
            onDragStart(element);
        },
        [onDragStart]
    );

    const handleMouseMove = useCallback(
        (e) => {
            if (!isDragging || !elementRef.current) {
                return;
            }

            const element = elementRef.current;
            const { clientX, clientY } = e;
            const left = origPos.left - (startPos.left - clientX);
            const top = origPos.top - (startPos.top - clientY);
            element.style.left = Math.max(left, 0) + "px";
            element.style.top = Math.max(top, 0) + "px";
            onDrag(element);
        },
        [isDragging, startPos, origPos, onDrag]
    );

    const handleMouseUp = useCallback(() => {
        setIsDragging(false);
        onDragEnd(elementRef.current);
    }, [onDragEnd]);

    useEffect(() => {
        const removeEvents = () => {
            window.removeEventListener("mousemove", handleMouseMove);
            window.removeEventListener("mouseup", handleMouseUp);
        };

        if (isDragging) {
            window.addEventListener("mousemove", handleMouseMove);
            window.addEventListener("mouseup", handleMouseUp);
        } else {
            removeEvents();
        }

        return removeEvents;
    }, [handleMouseMove, handleMouseUp, isDragging]);

    const registerElement = useCallback(
        (element) => {
            if (element) {
                element.removeEventListener("mousedown", handleMouseDown);
                element.addEventListener("mousedown", handleMouseDown);
            }
        },
        [handleMouseDown]
    );

    return registerElement;
};
