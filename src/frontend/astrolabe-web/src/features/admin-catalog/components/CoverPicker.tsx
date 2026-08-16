import { Alert, Box, Button, Stack, Typography } from '@mui/material';
import { useRef, useState } from 'react';
import { MaterialSymbol } from '../../../shared/components/MaterialSymbol';
import { tintFor } from '../../catalog/catalogCopy';
import {
  COVER_HINT,
  COVER_NOT_AN_IMAGE,
  COVER_TOO_LARGE,
  MAX_COVER_BYTES,
  NO_IMAGE_NOTE,
  isAllowedCoverType,
} from '../adminCatalogCopy';

/**
 * Picks a cover image, or leaves the book to its generated tint.
 *
 * <p>
 * The two halves are not alternatives offered evenly. `BR-CAT-005` says a book <em>without</em> a
 * cover gets a colour derived from its identity — so the swatch is a preview of what will happen,
 * not a colour anybody chooses. Presenting it as a choice would promise control the rule does not
 * grant, and the colour would change the moment the book got its real identifier.
 * </p>
 * <p>
 * Validation runs here as well as on the server, and the messages are the prototype's own. The
 * server checks the file's leading bytes too — a browser reports whatever content type the operating
 * system guessed, and this side cannot tell a renamed file from a real one.
 * </p>
 */
export interface CoverPickerProps {
  /** The chosen file, or null for none. Owned by the caller so the wizard can reset it. */
  file: File | null;
  onChange: (file: File | null) => void;
  /** Used only to preview the tint the book would fall back to. */
  previewKey: string;
  title: string;
}

export const CoverPicker = ({ file, onChange, previewKey, title }: CoverPickerProps) => {
  const input = useRef<HTMLInputElement>(null);
  const [dragging, setDragging] = useState(false);
  const [failure, setFailure] = useState<string | null>(null);

  // Revoked implicitly when the component unmounts; a book wizard is short-lived and this avoids a
  // ref plus an effect to manage one object URL.
  const preview = file ? URL.createObjectURL(file) : null;

  const accept = (candidate: File | null | undefined) => {
    if (!candidate) {
      return;
    }

    if (!isAllowedCoverType(candidate.type)) {
      setFailure(COVER_NOT_AN_IMAGE);
      return;
    }

    if (candidate.size > MAX_COVER_BYTES) {
      setFailure(COVER_TOO_LARGE);
      return;
    }

    setFailure(null);
    onChange(candidate);
  };

  return (
    <Stack spacing={1.5}>
      <Typography variant="subtitle2">Cover</Typography>

      <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2}>
        {/* The 3:4 portrait the cards crop to, so what is shown here is what a card will show. */}
        <Box
          sx={{
            width: 108,
            height: 144,
            flexShrink: 0,
            borderRadius: 1.5,
            overflow: 'hidden',
            display: 'grid',
            placeItems: 'center',
            bgcolor: preview ? 'transparent' : tintFor(previewKey),
            backgroundImage: preview ? `url("${preview}")` : undefined,
            backgroundSize: 'cover',
            backgroundPosition: 'center',
          }}
        >
          {preview ? null : (
            <Typography variant="h5" sx={{ color: 'common.white', opacity: 0.9 }}>
              {(title.trim() || 'A')
                .split(' ')
                .filter(Boolean)
                .slice(0, 2)
                .map((word) => word[0]?.toUpperCase() ?? '')
                .join('')}
            </Typography>
          )}
        </Box>

        <Stack spacing={1} sx={{ flex: 1 }}>
          <Box
            onDragOver={(event) => {
              event.preventDefault();
              setDragging(true);
            }}
            onDragLeave={(event) => {
              event.preventDefault();
              setDragging(false);
            }}
            onDrop={(event) => {
              event.preventDefault();
              setDragging(false);
              accept(event.dataTransfer.files?.[0]);
            }}
            onClick={() => input.current?.click()}
            sx={{
              flex: 1,
              minHeight: 96,
              display: 'grid',
              placeItems: 'center',
              px: 2,
              textAlign: 'center',
              cursor: 'pointer',
              borderRadius: 1.5,
              border: 1,
              borderStyle: 'dashed',
              borderColor: dragging ? 'primary.main' : 'divider',
              bgcolor: dragging ? 'action.hover' : 'transparent',
            }}
          >
            <Stack spacing={0.5} sx={{ alignItems: 'center' }}>
              <MaterialSymbol
                name="add_photo_alternate"
                size={24}
                sx={{ color: 'text.secondary' }}
              />
              <Typography variant="body2">Drag an image here, or click to browse</Typography>
              <Typography variant="caption" color="text.secondary">
                {COVER_HINT}
              </Typography>
            </Stack>
          </Box>

          <input
            ref={input}
            type="file"
            hidden
            accept="image/jpeg,image/png,image/webp"
            onChange={(event) => {
              accept(event.target.files?.[0]);
              // Cleared so choosing the same file twice still fires a change.
              event.target.value = '';
            }}
          />

          {file ? (
            <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }}>
              <Typography variant="caption" color="text.secondary" noWrap sx={{ flex: 1 }}>
                {file.name}
              </Typography>
              <Button
                size="small"
                color="error"
                startIcon={<MaterialSymbol name="delete" size={18} />}
                onClick={() => {
                  setFailure(null);
                  onChange(null);
                }}
              >
                Remove image
              </Button>
            </Stack>
          ) : (
            <Typography variant="caption" color="text.secondary">
              {NO_IMAGE_NOTE}
            </Typography>
          )}
        </Stack>
      </Stack>

      {failure ? <Alert severity="error">{failure}</Alert> : null}
    </Stack>
  );
};
