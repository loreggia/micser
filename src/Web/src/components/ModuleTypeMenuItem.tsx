import { MenuItem, Tooltip } from "@fluentui/react-components";
import { useShowHelp } from "@micser/web-sdk";

/**
 * A module type in an "add module" menu; with help shown, its description is a tooltip.
 */
export function ModuleTypeMenuItem({
  title,
  description,
  onClick,
}: {
  title: string;
  description?: string;
  onClick: () => void;
}) {
  const showHelp = useShowHelp();
  const item = <MenuItem onClick={onClick}>{title}</MenuItem>;

  return description && showHelp ? (
    <Tooltip content={description} relationship="description" positioning="after">
      {item}
    </Tooltip>
  ) : (
    item
  );
}
