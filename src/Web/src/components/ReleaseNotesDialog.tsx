import {
  Body1,
  Button,
  Dialog,
  DialogActions,
  DialogBody,
  DialogContent,
  DialogSurface,
  DialogTitle,
  Link,
  Spinner,
  Subtitle2,
  makeStyles,
  tokens,
  typographyStyles,
} from "@fluentui/react-components";
import { useQuery } from "@tanstack/react-query";
import Markdown, { type MarkdownToJSX } from "markdown-to-jsx/react";
import { useTranslation } from "../i18n";
import { shell } from "../shell";

const useStyles = makeStyles({
  notes: {
    ...typographyStyles.body1,
    "& ul, & ol": {
      paddingInlineStart: tokens.spacingHorizontalXXL,
    },
    "& li + li": {
      marginTop: tokens.spacingVerticalXS,
    },
    "& code": {
      fontFamily: tokens.fontFamilyMonospace,
    },
  },
  heading: {
    display: "block",
    marginTop: tokens.spacingVerticalL,
  },
  empty: {
    color: tokens.colorNeutralForeground3,
  },
});

/** Shows the release notes of a version (the installed one or the downloaded update), which the shell provides. */
export function ReleaseNotesDialog({
  version,
  open,
  onClose,
}: {
  version: string;
  open: boolean;
  onClose: () => void;
}) {
  const styles = useStyles();
  const { t } = useTranslation();
  const { data: notes, isPending } = useQuery({
    queryKey: ["releaseNotes", version],
    queryFn: () => shell!.getReleaseNotes(version),
    enabled: open && !!shell,
    staleTime: Infinity,
  });

  const heading = { component: Subtitle2, props: { as: "h3", className: styles.heading } };
  // raw HTML shows as text; links open in the browser (the shell opens new windows there)
  const options: MarkdownToJSX.Options = {
    disableParsingRawHTML: true,
    forceWrapper: true,
    overrides: {
      h1: heading,
      h2: heading,
      h3: heading,
      h4: heading,
      a: { component: Link, props: { target: "_blank" } },
    },
  };

  return (
    <Dialog open={open} onOpenChange={(_, data) => !data.open && onClose()}>
      <DialogSurface>
        <DialogBody>
          <DialogTitle>{t("releaseNotes.title", { version })}</DialogTitle>
          <DialogContent>
            {isPending ? (
              <Spinner size="small" label={t("releaseNotes.loading")} />
            ) : notes ? (
              <Markdown className={styles.notes} options={options}>
                {notes}
              </Markdown>
            ) : (
              <Body1 className={styles.empty}>{t("releaseNotes.none")}</Body1>
            )}
          </DialogContent>
          <DialogActions>
            <Button appearance="secondary" onClick={onClose}>
              {t("common.close")}
            </Button>
          </DialogActions>
        </DialogBody>
      </DialogSurface>
    </Dialog>
  );
}
