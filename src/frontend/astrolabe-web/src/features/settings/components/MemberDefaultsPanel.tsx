import { Box, Paper, Stack, Typography } from '@mui/material';
import { MaterialSymbol } from '../../../shared/components/MaterialSymbol';
import { useMemberDefaults } from '../memberDefaults';

/**
 * "Delivery, returns and purchases" — the three defaults the app proposes.
 *
 * <p>
 * The prototype's own three groups, its own labels and its own notes, including the fee and the
 * waiting time on each option. Those are what make the choice a choice: "Home delivery" alone does
 * not tell anybody it costs $3.99.
 * </p>
 */
interface DefaultOption {
  value: string;
  label: string;
  note: string;
  icon: string;
}

interface DefaultGroup {
  key: string;
  label: string;
  note: string;
  icon: string;
  selected: string;
  options: DefaultOption[];
  onSelect: (value: string) => void;
}

export const MemberDefaultsPanel = () => {
  const defaults = useMemberDefaults();

  const groups: DefaultGroup[] = [
    {
      key: 'delivery',
      label: 'Book delivery',
      note: 'How a reserved book reaches you',
      icon: 'local_shipping',
      selected: defaults.delivery,
      onSelect: (value) => defaults.setDelivery(value as typeof defaults.delivery),
      options: [
        {
          value: 'Collection',
          label: 'Pick up at library',
          note: 'Ready in 2 h · free',
          icon: 'store',
        },
        {
          value: 'HomeDelivery',
          label: 'Home delivery',
          note: '24–48 h · +$3.99',
          icon: 'local_shipping',
        },
      ],
    },
    {
      key: 'returns',
      label: 'Returns',
      note: 'How you give the book back',
      icon: 'assignment_return',
      selected: defaults.returns,
      onSelect: (value) => defaults.setReturns(value as typeof defaults.returns),
      options: [
        {
          value: 'CourierPickup',
          label: 'Courier pickup',
          note: 'A courier collects it at your door',
          icon: 'local_shipping',
        },
        {
          value: 'LibraryDropOff',
          label: 'Drop off at library',
          note: 'Hand it to the desk yourself',
          icon: 'store',
        },
      ],
    },
    {
      key: 'purchase',
      label: 'Purchases',
      note: 'How books you buy are fulfilled',
      icon: 'shopping_bag',
      selected: defaults.purchase,
      onSelect: (value) => defaults.setPurchase(value as typeof defaults.purchase),
      options: [
        {
          value: 'Collection',
          label: 'Collect at library',
          note: 'Ready in 2 h · free',
          icon: 'store',
        },
        {
          value: 'Shipping',
          label: 'Ship to my address',
          note: '3–5 days · +$3.99',
          icon: 'local_shipping',
        },
      ],
    },
  ];

  return (
    <Stack spacing={2}>
      <Stack spacing={0.25}>
        <Typography variant="h6">Delivery, returns and purchases</Typography>
        <Typography variant="body2" color="text.secondary">
          These are the options the app proposes by default. You can still switch them when you
          reserve or buy a book.
        </Typography>
      </Stack>

      {groups.map((group) => (
        <Stack key={group.key} spacing={1.25}>
          <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }}>
            <MaterialSymbol name={group.icon} size={20} sx={{ color: 'text.secondary' }} />
            <Stack spacing={0}>
              <Typography variant="subtitle2">{group.label}</Typography>
              <Typography variant="caption" color="text.secondary">
                {group.note}
              </Typography>
            </Stack>
          </Stack>

          <Stack
            direction={{ xs: 'column', sm: 'row' }}
            spacing={1.5}
            role="radiogroup"
            aria-label={group.label}
          >
            {group.options.map((option) => {
              const selected = group.selected === option.value;

              return (
                <Paper
                  key={option.value}
                  variant="outlined"
                  role="radio"
                  aria-checked={selected}
                  tabIndex={0}
                  onClick={() => group.onSelect(option.value)}
                  onKeyDown={(event) => {
                    // A div that behaves like a control has to answer the keyboard like one.
                    if (event.key === 'Enter' || event.key === ' ') {
                      event.preventDefault();
                      group.onSelect(option.value);
                    }
                  }}
                  sx={{
                    flex: 1,
                    p: 1.5,
                    cursor: 'pointer',
                    borderColor: selected ? 'primary.main' : 'divider',
                    bgcolor: selected ? 'action.selected' : 'transparent',
                  }}
                >
                  <Stack direction="row" spacing={1.25} sx={{ alignItems: 'flex-start' }}>
                    <MaterialSymbol
                      name={selected ? 'radio_button_checked' : 'radio_button_unchecked'}
                      size={20}
                      sx={{ color: selected ? 'primary.main' : 'text.disabled' }}
                    />
                    <Box sx={{ minWidth: 0 }}>
                      <Typography variant="body2">{option.label}</Typography>
                      <Typography variant="caption" color="text.secondary">
                        {option.note}
                      </Typography>
                    </Box>
                  </Stack>
                </Paper>
              );
            })}
          </Stack>
        </Stack>
      ))}
    </Stack>
  );
};
