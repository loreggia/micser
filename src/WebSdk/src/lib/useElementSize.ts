import { useLayoutEffect, useState, type RefObject } from "react";

export interface ElementSize {
  width: number;
  height: number;
}

/**
 * The layout size of an element's content box in CSS pixels, unaffected by transforms such as the graph's zoom; `fallback` until it is
 * measured.
 */
export function useElementSize(ref: RefObject<HTMLElement | null>, fallback: ElementSize): ElementSize {
  const { width: fallbackWidth, height: fallbackHeight } = fallback;
  const [size, setSize] = useState(fallback);

  useLayoutEffect(() => {
    const element = ref.current;
    if (!element) {
      return;
    }

    const update = (width: number, height: number) => {
      const next = { width: width || fallbackWidth, height: height || fallbackHeight };
      setSize((current) => (current.width === next.width && current.height === next.height ? current : next));
    };
    update(element.clientWidth, element.clientHeight);
    const observer = new ResizeObserver(([entry]) => update(entry.contentRect.width, entry.contentRect.height));
    observer.observe(element);
    return () => observer.disconnect();
  }, [ref, fallbackWidth, fallbackHeight]);

  return size;
}
