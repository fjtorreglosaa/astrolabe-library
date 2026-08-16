import {
  Alert,
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Divider,
  MenuItem,
  Stack,
  Step,
  StepLabel,
  Stepper,
  TextField,
  Typography,
} from '@mui/material';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useState } from 'react';
import { ConfirmDialog } from '../../../shared/components/ConfirmDialog';
import { MaterialSymbol } from '../../../shared/components/MaterialSymbol';
import { money } from '../../membership/planCopy';
import { getAdministeredLibraries } from '../../network/api/networkApi';
import type { PlanTier } from '../../membership/api/membershipApi';
import { createBookDraft, publishBook, type Genre } from '../api/adminCatalogApi';
import {
  DISCARD_BODY,
  DISCARD_TITLE,
  DRAFT_NOTE,
  GENRE_LABEL,
  PUBLISH_NOTE,
  WIZARD_STEPS,
} from '../adminCatalogCopy';

interface Draft {
  title: string;
  author: string;
  isbn: string;
  publisher: string;
  genre: Genre;
  tier: PlanTier;
  price: string;
  copies: string;
  libraryId: string;
}

const EMPTY: Draft = {
  title: '',
  author: '',
  isbn: '',
  publisher: '',
  genre: 'Fiction',
  tier: 'Plus',
  price: '',
  copies: '2',
  libraryId: '',
};

/**
 * Adding a book, in the prototype's three steps: details, copies and pricing, review.
 *
 * <p>
 * The book is created as a **draft** and published as a separate act, which is the prototype's own
 * shape and the reason the wizard has a Review step at all — somebody adding forty titles from a
 * delivery note should be able to stop halfway and come back, and nothing they half-finished should
 * reach a member in the meantime.
 * </p>
 * <p>
 * Money is entered in whole currency and sent in cents. The conversion happens once, here, at the
 * edge — the rest of the client never sees a decimal price.
 * </p>
 */
export interface BookWizardDialogProps {
  open: boolean;
  onClose: () => void;
  onSaved: (message: string) => void;
}

export const BookWizardDialog = ({ open, onClose, onSaved }: BookWizardDialogProps) => {
  const queryClient = useQueryClient();
  const [step, setStep] = useState(0);
  const [draft, setDraft] = useState<Draft>(EMPTY);
  const [dirty, setDirty] = useState(false);
  const [confirmingDiscard, setConfirmingDiscard] = useState(false);

  const scope = useQuery({
    queryKey: ['network', 'administered-libraries'],
    queryFn: getAdministeredLibraries,
    enabled: open,
  });

  const set = <K extends keyof Draft>(key: K) => (value: Draft[K]) => {
    setDraft((current) => ({ ...current, [key]: value }));
    setDirty(true);
  };

  const libraryId = draft.libraryId || scope.data?.[0]?.id || '';

  // Cents, never a float. A price typed as 18.99 must reach the API as 1899 and not as 1898.9999.
  const priceCents = Math.round(Number.parseFloat(draft.price || '0') * 100);
  const copies = Number.parseInt(draft.copies || '0', 10);

  const stepReady =
    step === 0
      ? Boolean(draft.title.trim() && draft.author.trim() && draft.isbn.trim())
      : step === 1
        ? Number.isFinite(priceCents) && priceCents > 0 && copies > 0 && Boolean(libraryId)
        : true;

  const reset = () => {
    setStep(0);
    setDraft(EMPTY);
    setDirty(false);
    setConfirmingDiscard(false);
  };

  const close = () => {
    reset();
    onClose();
  };

  const save = useMutation({
    mutationFn: async (publish: boolean) => {
      const id = await createBookDraft({
        isbn: draft.isbn.trim(),
        title: draft.title.trim(),
        author: draft.author.trim(),
        publisher: draft.publisher.trim() || null,
        genre: draft.genre,
        tier: draft.tier,
        retailPriceCents: priceCents,
        coverUrl: null,
        copies: [{ libraryId, quantity: copies }],
      });

      if (publish) {
        await publishBook(id);
      }

      return publish;
    },
    onSuccess: async (published) => {
      await queryClient.invalidateQueries({ queryKey: ['admin', 'catalog'] });
      onSaved(published ? `“${draft.title.trim()}” is live in the catalogue.` : DRAFT_NOTE);
      close();
    },
  });

  const requestClose = () => (dirty ? setConfirmingDiscard(true) : close());

  return (
    <>
      <Dialog
        open={open}
        onClose={save.isPending ? undefined : requestClose}
        maxWidth="sm"
        fullWidth
      >
        <DialogTitle>
          <Stack spacing={1.5}>
            <Typography variant="h6">Add a book to the catalogue</Typography>
            <Stepper activeStep={step} alternativeLabel>
              {WIZARD_STEPS.map((label) => (
                <Step key={label}>
                  <StepLabel>{label}</StepLabel>
                </Step>
              ))}
            </Stepper>
          </Stack>
        </DialogTitle>

        <DialogContent>
          {step === 0 ? (
            <Stack spacing={2} sx={{ pt: 1 }}>
              <TextField
                label="Title"
                required
                value={draft.title}
                onChange={(event) => set('title')(event.target.value)}
                fullWidth
              />
              <TextField
                label="Author"
                required
                value={draft.author}
                onChange={(event) => set('author')(event.target.value)}
                fullWidth
              />
              <TextField
                label="ISBN"
                required
                value={draft.isbn}
                onChange={(event) => set('isbn')(event.target.value)}
                helperText="13 digits. The API refuses anything that is not a valid ISBN."
                fullWidth
              />
              <TextField
                label="Publisher"
                value={draft.publisher}
                onChange={(event) => set('publisher')(event.target.value)}
                fullWidth
              />
              <TextField
                select
                label="Genre"
                value={draft.genre}
                onChange={(event) => set('genre')(event.target.value as Genre)}
                fullWidth
              >
                {(Object.keys(GENRE_LABEL) as Genre[]).map((genre) => (
                  <MenuItem key={genre} value={genre}>
                    {GENRE_LABEL[genre]}
                  </MenuItem>
                ))}
              </TextField>
            </Stack>
          ) : null}

          {step === 1 ? (
            <Stack spacing={2} sx={{ pt: 1 }}>
              <TextField
                label="Retail price"
                required
                value={draft.price}
                onChange={(event) => set('price')(event.target.value)}
                slotProps={{ input: { startAdornment: <span>$&nbsp;</span> } }}
                fullWidth
              />
              <TextField
                label="Copies"
                required
                type="number"
                value={draft.copies}
                onChange={(event) => set('copies')(event.target.value)}
                fullWidth
              />
              <TextField
                select
                label="Library"
                required
                value={libraryId}
                onChange={(event) => set('libraryId')(event.target.value)}
                // Only the branches this administrator holds. BR-NET-006 is enforced by the API
                // regardless; offering the others here would just invite a refusal.
                helperText="Only the libraries you administer."
                fullWidth
              >
                {(scope.data ?? []).map((library) => (
                  <MenuItem key={library.id} value={library.id}>
                    {library.name}
                  </MenuItem>
                ))}
              </TextField>
              <TextField
                select
                label="Plan tier"
                value={draft.tier}
                onChange={(event) => set('tier')(event.target.value as PlanTier)}
                helperText="A property of the book, not of a member. Decides who may borrow it."
                fullWidth
              >
                {(['Basic', 'Plus', 'Max'] as const).map((tier) => (
                  <MenuItem key={tier} value={tier}>
                    {tier}
                  </MenuItem>
                ))}
              </TextField>
            </Stack>
          ) : null}

          {step === 2 ? (
            <Stack spacing={1} sx={{ pt: 1 }}>
              <Row label="Title" value={draft.title || '—'} />
              <Row label="Author" value={draft.author || '—'} />
              <Row label="ISBN" value={draft.isbn || '—'} />
              <Row label="Genre" value={GENRE_LABEL[draft.genre]} />
              <Divider sx={{ my: 1 }} />
              <Row label="Price" value={priceCents > 0 ? money(priceCents) : '—'} />
              <Row label="Copies" value={String(copies)} />
              <Row
                label="Library"
                value={
                  scope.data?.find((library) => library.id === libraryId)?.name ?? '—'
                }
              />
              <Row label="Plan tier" value={draft.tier} />

              <Alert severity="info" sx={{ mt: 2 }} icon={<MaterialSymbol name="visibility" size={20} />}>
                {PUBLISH_NOTE}
              </Alert>
            </Stack>
          ) : null}

          {save.isError ? (
            <Alert severity="error" sx={{ mt: 2 }}>
              {(save.error as { response?: { data?: { title?: string } } })?.response?.data
                ?.title ?? 'We could not save that book.'}
            </Alert>
          ) : null}
        </DialogContent>

        <DialogActions sx={{ px: 3, pb: 2 }}>
          <Button onClick={requestClose} color="inherit" disabled={save.isPending}>
            Cancel
          </Button>
          <Stack direction="row" spacing={1} sx={{ ml: 'auto' }}>
            {step > 0 ? (
              <Button onClick={() => setStep(step - 1)} disabled={save.isPending}>
                Back
              </Button>
            ) : null}
            {step < 2 ? (
              <Button variant="contained" disabled={!stepReady} onClick={() => setStep(step + 1)}>
                Next
              </Button>
            ) : (
              <>
                <Button
                  onClick={() => save.mutate(false)}
                  loading={save.isPending && !save.variables}
                >
                  Save as draft
                </Button>
                <Button
                  variant="contained"
                  onClick={() => save.mutate(true)}
                  loading={save.isPending && save.variables === true}
                >
                  Publish
                </Button>
              </>
            )}
          </Stack>
        </DialogActions>
      </Dialog>

      <ConfirmDialog
        open={confirmingDiscard}
        title={DISCARD_TITLE}
        description={DISCARD_BODY}
        confirmLabel="Discard"
        destructive
        onConfirm={close}
        onCancel={() => setConfirmingDiscard(false)}
      />
    </>
  );
};

const Row = ({ label, value }: { label: string; value: string }) => (
  <Stack direction="row" spacing={2} sx={{ justifyContent: 'space-between' }}>
    <Typography variant="body2" color="text.secondary">
      {label}
    </Typography>
    <Typography variant="body2" sx={{ textAlign: 'right' }}>
      {value}
    </Typography>
  </Stack>
);
