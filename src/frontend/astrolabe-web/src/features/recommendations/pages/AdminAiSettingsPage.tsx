import {
  Alert,
  Button,
  Chip,
  Divider,
  Paper,
  Stack,
  TextField,
  ToggleButton,
  ToggleButtonGroup,
  Typography,
} from '@mui/material';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useState } from 'react';
import { MaterialSymbol } from '../../../shared/components/MaterialSymbol';
import { EmptyState, ErrorState, LoadingState } from '../../../shared/components/StateViews';
import {
  configureLibraryAi,
  disableLibraryAi,
  getLibraryAiStatus,
  type AiProvider,
  type LibraryAiStatus,
} from '../api/recommendationsApi';
import { CONFIG_INTRO, KEY_PRIVACY_NOTE } from '../recommendationsCopy';

/**
 * AI recommendations, per library. Staff only.
 *
 * <p>
 * The screen holds a key in a text field for exactly as long as it takes to submit it, and then
 * forgets it — the field is cleared on success and there is no endpoint that would return one, so
 * there is nothing to repopulate it from. That is BR-REC-004 as the operator experiences it: the
 * key is write-only, and the copy says so before they wonder.
 * </p>
 * <p>
 * "Save and test" is one action, not two. BR-REC-008 makes verification the thing that connects a
 * library, so a save that did not test would leave staff believing they had finished.
 * </p>
 */
export const AdminAiSettingsPage = () => {
  const queryClient = useQueryClient();
  const [notice, setNotice] = useState<string | null>(null);

  const libraries = useQuery({
    queryKey: ['recommendations', 'libraries'],
    queryFn: getLibraryAiStatus,
  });

  const refresh = () =>
    queryClient.invalidateQueries({ queryKey: ['recommendations', 'libraries'] });

  return (
    <Stack spacing={3}>
      <Stack spacing={0.5}>
        <Typography variant="h4">AI recommendations per library</Typography>
        <Typography variant="body2" color="text.secondary">
          {CONFIG_INTRO}
        </Typography>
      </Stack>

      {notice ? (
        <Alert severity="success" onClose={() => setNotice(null)}>
          {notice}
        </Alert>
      ) : null}

      {libraries.isLoading ? (
        <LoadingState label="Loading your libraries…" />
      ) : libraries.isError || !libraries.data ? (
        <ErrorState
          description="We could not load your libraries."
          onRetry={() => void libraries.refetch()}
        />
      ) : libraries.data.length === 0 ? (
        <EmptyState
          title="No libraries assigned"
          description="You administer no libraries, so there is nothing to configure here."
        />
      ) : (
        <Stack spacing={2}>
          {libraries.data.map((library) => (
            <LibraryRow
              key={library.libraryId}
              library={library}
              onDone={async (message) => {
                setNotice(message);
                await refresh();
              }}
            />
          ))}
        </Stack>
      )}
    </Stack>
  );
};

const LibraryRow = ({
  library,
  onDone,
}: {
  library: LibraryAiStatus;
  onDone: (message: string) => Promise<void>;
}) => {
  const [provider, setProvider] = useState<AiProvider>(library.provider ?? 'Claude');
  const [credential, setCredential] = useState('');
  const [failure, setFailure] = useState<string | null>(null);

  const save = useMutation({
    mutationFn: () => configureLibraryAi(library.libraryId, provider, credential),
    onSuccess: async () => {
      // Cleared the moment it succeeds. Nothing can repopulate it, and leaving a key sitting in a
      // DOM node after it is stored is a smaller version of the leak the whole rule is about.
      setCredential('');
      setFailure(null);
      await onDone(
        `${provider} key verified for ${library.libraryName}. Recommendations enabled for its members.`,
      );
    },
    onError: (error) =>
      setFailure(
        (error as { response?: { data?: { title?: string } } })?.response?.data?.title ??
          'We could not verify that key.',
      ),
  });

  const disable = useMutation({
    mutationFn: () => disableLibraryAi(library.libraryId),
    onSuccess: () => onDone(`AI recommendations turned off for ${library.libraryName}.`),
  });

  return (
    <Paper variant="outlined" sx={{ p: 2.5 }}>
      <Stack spacing={2}>
        <Stack
          direction={{ xs: 'column', sm: 'row' }}
          spacing={1}
          sx={{ justifyContent: 'space-between', alignItems: { sm: 'center' } }}
        >
          <Stack spacing={0.25}>
            <Typography variant="subtitle1">{library.libraryName}</Typography>
            <Typography variant="caption" color="text.secondary">
              {library.note}
            </Typography>
          </Stack>
          <Chip
            size="small"
            variant="outlined"
            color={library.isConnected ? 'success' : 'default'}
            icon={
              <MaterialSymbol
                name={library.isConnected ? 'check_circle' : 'link_off'}
                size={16}
              />
            }
            label={library.status}
          />
        </Stack>

        {/* A library that was verified and then refused says so, because the fix is a new key
            rather than a switch its staff never touched. */}
        {library.isEnabled && !library.isVerified ? (
          <Alert severity="warning">
            This library is switched on, but its key was refused. Members are seeing the fallback
            until a working one is saved.
          </Alert>
        ) : null}

        <Divider />

        <Stack direction={{ xs: 'column', md: 'row' }} spacing={2} sx={{ alignItems: 'center' }}>
          <ToggleButtonGroup
            exclusive
            size="small"
            value={provider}
            onChange={(_event, value) => value && setProvider(value as AiProvider)}
          >
            <ToggleButton value="Claude">Claude</ToggleButton>
            <ToggleButton value="OpenAI">OpenAI</ToggleButton>
          </ToggleButtonGroup>

          <TextField
            fullWidth
            size="small"
            type="password"
            label={library.isConnected ? 'Replace the key' : 'Provider key'}
            value={credential}
            onChange={(event) => setCredential(event.target.value)}
            helperText={KEY_PRIVACY_NOTE}
          />

          <Stack direction="row" spacing={1}>
            <Button
              variant="contained"
              disabled={!credential.trim()}
              loading={save.isPending}
              onClick={() => save.mutate()}
            >
              Save and test
            </Button>
            {library.isEnabled ? (
              <Button color="error" loading={disable.isPending} onClick={() => disable.mutate()}>
                Turn off
              </Button>
            ) : null}
          </Stack>
        </Stack>

        {failure ? <Alert severity="error">{failure}</Alert> : null}
      </Stack>
    </Paper>
  );
};
