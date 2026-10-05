import { Menu, MenuDivider, MenuItem, MenuList, MenuPopover, MenuTrigger, Tooltip } from "@fluentui/react-components";
import { useGetSubgraphTemplates } from "@micser/web-sdk";
import { useTranslation } from "../i18n";
import { useSubgraphActions } from "./subgraphActions";

/**
 * A "Templates" item for the add menus, opening a submenu of the subgraph templates (sorted by name) and "Manage templates…". A
 * template whose plugins aren't all loaded is disabled.
 */
export function TemplatesSubmenu({ onInstantiate }: { onInstantiate: (templateId: string) => void }) {
  const { t, language } = useTranslation();
  const { data: templates = [] } = useGetSubgraphTemplates();
  const { manageTemplates } = useSubgraphActions();

  return (
    <Menu>
      <MenuTrigger disableButtonEnhancement>
        <MenuItem>{t("templates.submenu")}</MenuItem>
      </MenuTrigger>
      <MenuPopover>
        <MenuList>
          {templates.length === 0 && <MenuItem disabled>{t("templates.none")}</MenuItem>}
          {[...templates]
            .sort((a, b) => a.name.localeCompare(b.name, language))
            .map((template) =>
              template.unavailableTypes.length > 0 ? (
                <Tooltip
                  key={template.id}
                  content={t("common.pluginsNotLoaded", { types: [...new Set(template.unavailableTypes)].join(", ") })}
                  relationship="description"
                >
                  <MenuItem disabled>{template.name}</MenuItem>
                </Tooltip>
              ) : (
                <MenuItem key={template.id} onClick={() => onInstantiate(template.id)}>
                  {template.name}
                </MenuItem>
              )
            )}
          <MenuDivider />
          <MenuItem onClick={manageTemplates}>{t("templates.manage")}</MenuItem>
        </MenuList>
      </MenuPopover>
    </Menu>
  );
}
