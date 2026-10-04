import { Body1Strong, Input, Tooltip, makeStyles, mergeClasses } from "@fluentui/react-components";
import { useRef, useState } from "react";

/** Longest name the engine accepts. */
const maxNameLength = 100;

const useStyles = makeStyles({
  title: {
    cursor: "text",
  },
  titleInput: {
    width: "100%",
  },
});

/**
 * The title of a module or subgraph. Double-click to rename: Enter or leaving the field saves, Escape cancels, and an empty name goes
 * back to the fallback title.
 */
export function ModuleTitle({
  name,
  fallback,
  label,
  onRename,
}: {
  name: string | null;
  /** Shown without a name, e.g. the module type's title. */
  fallback: string;
  /** The field's accessible name. */
  label: string;
  onRename: (name: string | null) => void;
}) {
  const styles = useStyles();
  const [draft, setDraft] = useState<string>();
  // the field may also lose focus when it's removed after Enter or Escape
  const isFinished = useRef(false);
  const title = name || fallback;

  if (draft === undefined) {
    return (
      <Tooltip content="Double-click to rename" relationship="description">
        <Body1Strong
          className={styles.title}
          onDoubleClick={() => {
            isFinished.current = false;
            setDraft(name ?? "");
          }}
        >
          {title}
        </Body1Strong>
      </Tooltip>
    );
  }

  const finish = () => {
    isFinished.current = true;
    setDraft(undefined);
  };

  const save = () => {
    if (isFinished.current) {
      return;
    }

    const renamed = draft.trim() || null;
    finish();
    if (renamed !== name) {
      onRename(renamed);
    }
  };

  return (
    <Input
      className={mergeClasses(styles.titleInput, "nodrag")}
      size="small"
      autoFocus
      value={draft}
      placeholder={fallback}
      maxLength={maxNameLength}
      aria-label={label}
      onChange={(_, data) => setDraft(data.value)}
      onBlur={save}
      onKeyDown={(event) => {
        // keep keys like Delete from reaching the graph, which would remove the node
        event.stopPropagation();
        if (event.key === "Enter") {
          save();
        } else if (event.key === "Escape") {
          finish();
        }
      }}
    />
  );
}
