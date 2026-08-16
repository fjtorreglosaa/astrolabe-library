import {
  Alert,
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Divider,
  Stack,
  Typography,
} from '@mui/material';
import { useState } from 'react';
import { MaterialSymbol } from '../../../shared/components/MaterialSymbol';
import { ErrorState, LoadingState } from '../../../shared/components/StateViews';
import type { Membership, PlanChangeQuote, PlanTier } from '../api/membershipApi';
import { lossSentence, quoteCopy } from '../planCopy';

/**
 * The two-step plan change, as the prototype stages it: review the arithmetic, then confirm.
 *
 * The second step exists because the two directions have opposite consequences — one charges money
 * now, the other silently gives something up at the renewal date — and a single button would let a
 * member schedule a downgrade believing they had upgraded.
 *
 * Every figure comes from the quote the API returned. Nothing is recomputed here: money arithmetic
 * in two languages is money arithmetic that will eventually disagree.
 */
export interface PlanChangeDialogProps {
  open: boolean;
  target: PlanTier | null;
  membership: Membership;
  quote: PlanChangeQuote | undefined;
  isLoadingQuote: boolean;
  quoteFailed: boolean;
  isSubmitting: boolean;
  onRetryQuote: () => void;
  onConfirm: (target: PlanTier) => void;
  onClose: () => void;
}

export const PlanChangeDialog = ({
  open,
  target,
  membership,
  quote,
  isLoadingQuote,
  quoteFailed,
  isSubmitting,
  onRetryQuote,
  onConfirm,
  onClose,
}: PlanChangeDialogProps) => {
  const [step, setStep] = useState<'review' | 'confirm'>('review');

  const close = () => {
    setStep('review');
    onClose();
  };

  if (!target) {
    return null;
  }

  const copy = quote ? quoteCopy(quote, membership) : null;

  return (
    <Dialog open={open} onClose={isSubmitting ? undefined : close} maxWidth="sm" fullWidth>
      {isLoadingQuote || !copy || !quote ? (
        <DialogContent>
          {quoteFailed ? (
            <ErrorState description="We could not price that change." onRetry={onRetryQuote} />
          ) : (
            <LoadingState label="Pricing your change…" />
          )}
        </DialogContent>
      ) : step === 'review' ? (
        <>
          <DialogTitle sx={{ pb: 1 }}>
            <Stack spacing={0.5}>
              <Typography variant="overline" color="primary.main">
                {copy.kicker}
              </Typography>
              <Typography variant="h6">{copy.title}</Typography>
            </Stack>
          </DialogTitle>

          <DialogContent>
            <Stack spacing={2}>
              <Typography variant="body2" color="text.secondary">
                {copy.sub}
              </Typography>

              <Stack spacing={1}>
                {copy.rows.map((row) => (
                  <Stack
                    key={row.label}
                    direction="row"
                    spacing={2}
                    sx={{ justifyContent: 'space-between', alignItems: 'baseline' }}
                  >
                    <Typography variant="body2" color="text.secondary">
                      {row.label}
                    </Typography>
                    <Typography variant="body2">{row.value}</Typography>
                  </Stack>
                ))}
              </Stack>

              <Divider />

              <Stack
                direction="row"
                spacing={2}
                sx={{ justifyContent: 'space-between', alignItems: 'baseline' }}
              >
                <Typography variant="subtitle2">{copy.dueLabel}</Typography>
                <Typography variant="h6">{copy.due}</Typography>
              </Stack>

              <Typography variant="body2" color="text.secondary">
                {copy.after}
              </Typography>

              {/* BR-MBR-020: the member must see what they give up before they can confirm. */}
              {quote.whatYouLose.length > 0 ? (
                <Alert severity="warning" icon={<MaterialSymbol name="info" size={20} />}>
                  <Stack spacing={0.5}>
                    <Typography variant="subtitle2">What you lose</Typography>
                    {quote.whatYouLose.map((loss) => (
                      <Typography key={loss} variant="body2">
                        {lossSentence(loss, membership)}
                      </Typography>
                    ))}
                  </Stack>
                </Alert>
              ) : null}
            </Stack>
          </DialogContent>

          <DialogActions>
            <Button onClick={close} color="inherit">
              Cancel
            </Button>
            <Button variant="contained" onClick={() => setStep('confirm')}>
              {copy.cta}
            </Button>
          </DialogActions>
        </>
      ) : (
        <>
          <DialogTitle>{copy.confirmTitle}</DialogTitle>

          <DialogContent>
            <Typography variant="body2" color="text.secondary">
              {copy.confirmBody}
            </Typography>
          </DialogContent>

          <DialogActions>
            <Button onClick={() => setStep('review')} color="inherit" disabled={isSubmitting}>
              Back
            </Button>
            <Button
              variant="contained"
              onClick={() => onConfirm(target)}
              loading={isSubmitting}
            >
              Confirm
            </Button>
          </DialogActions>
        </>
      )}
    </Dialog>
  );
};
