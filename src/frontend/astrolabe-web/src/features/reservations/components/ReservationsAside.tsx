import { Button, Paper, Stack, Typography } from '@mui/material';
import { useQuery } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
import { MaterialSymbol } from '../../../shared/components/MaterialSymbol';
import { getMyFines } from '../../billing/api/billingApi';
import { FINE_RULE_NOTE } from '../../billing/billingCopy';
import { money } from '../../membership/planCopy';
import { useMemberDefaults } from '../../settings/memberDefaults';
import { DELIVERY_LABEL, RETURN_LABEL } from '../reservationCopy';
import { FULFILMENT_LABEL } from '../../store/storeCopy';

/**
 * The 320px column beside the reservations table: what is owed, and what the app will propose next
 * time.
 *
 * <p>
 * Both cards answer a question the table raises. A member looking at an overdue loan wants to know
 * what it is costing them, and one about to return a book wants to know which way it is going —
 * putting either behind a navigation makes them leave the screen to find out.
 * </p>
 */
export const ReservationsAside = () => {
  const navigate = useNavigate();
  const defaults = useMemberDefaults();

  const fines = useQuery({ queryKey: ['billing', 'fines'], queryFn: getMyFines });

  const owed = fines.data?.outstandingCents ?? 0;

  const summary = [
    { icon: 'local_shipping', label: 'Book delivery', value: DELIVERY_LABEL[defaults.delivery] },
    { icon: 'assignment_return', label: 'Returns', value: RETURN_LABEL[defaults.returns] },
    { icon: 'shopping_bag', label: 'Purchases', value: FULFILMENT_LABEL[defaults.purchase] },
  ];

  return (
    <Stack spacing={2}>
      <Paper variant="outlined" sx={{ p: 2.5 }}>
        <Typography variant="overline" color="text.secondary">
          Outstanding fines
        </Typography>
        <Typography
          variant="h1"
          sx={{ mt: 1.25, fontSize: '2.25rem', lineHeight: 1 }}
          // Red only when there is something to pay. A zero in alarm colours is the app shouting
          // about good news.
          color={owed > 0 ? '#B3261E' : 'text.primary'}
        >
          {money(owed)}
        </Typography>
        <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mt: 0.75 }}>
          {FINE_RULE_NOTE}
        </Typography>

        <Button
          variant={owed > 0 ? 'contained' : 'outlined'}
          color={owed > 0 ? 'primary' : 'inherit'}
          fullWidth
          onClick={() => navigate('/fines')}
          sx={{ mt: 2, height: 40 }}
        >
          {/* Paying happens on the fines screen, which owns the card selection and the receipt.
              A second payment flow here would be the same rules written twice. */}
          {owed > 0 ? 'Pay fines' : 'See payments'}
        </Button>
      </Paper>

      <Paper variant="outlined" sx={{ p: 2.5 }}>
        <Typography variant="body2" sx={{ fontWeight: 600 }}>
          Your defaults
        </Typography>
        <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mt: 0.5 }}>
          Applied to every reservation and purchase. You can change it at checkout.
        </Typography>

        <Stack spacing={1.5} sx={{ mt: 1.75 }}>
          {summary.map((entry) => (
            <Stack key={entry.label} direction="row" spacing={1.5} sx={{ alignItems: 'center' }}>
              <MaterialSymbol
                name={entry.icon}
                size={20}
                sx={{ color: 'primary.main', flexShrink: 0 }}
              />
              <Stack spacing={0.25} sx={{ minWidth: 0 }}>
                <Typography variant="overline" color="text.secondary">
                  {entry.label}
                </Typography>
                <Typography variant="body2" sx={{ fontWeight: 600 }}>
                  {entry.value}
                </Typography>
              </Stack>
            </Stack>
          ))}
        </Stack>

        <Button
          variant="outlined"
          color="inherit"
          fullWidth
          onClick={() => navigate('/settings')}
          sx={{ mt: 2, height: 38 }}
        >
          Change in settings
        </Button>
      </Paper>
    </Stack>
  );
};
