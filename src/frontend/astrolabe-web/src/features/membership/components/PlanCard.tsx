import { Box, Card, CardActionArea, Chip, Stack, Typography } from '@mui/material';
import { MaterialSymbol } from '../../../shared/components/MaterialSymbol';
import type { PlanOption } from '../api/membershipApi';
import { PLAN_BULLETS, PLAN_SUMMARY, money } from '../planCopy';

/**
 * One plan in the comparison row.
 *
 * The selected plan is marked with a radio glyph and a tinted border, exactly as the prototype does
 * — the tint is what makes the current plan readable at a glance without reading the badge.
 */
export interface PlanCardProps {
  option: PlanOption;
  onSelect: (option: PlanOption) => void;
}

export const PlanCard = ({ option, onSelect }: PlanCardProps) => {
  const badge = option.isCurrent
    ? 'Current plan'
    : option.direction === 'upgrade'
      ? 'Upgrade'
      : 'Downgrade';

  return (
    <Card
      variant="outlined"
      sx={{
        flex: 1,
        minWidth: 0,
        borderColor: option.isCurrent ? 'primary.main' : 'divider',
        borderWidth: option.isCurrent ? 2 : 1,
        bgcolor: option.isCurrent ? 'action.selected' : 'transparent',
      }}
    >
      {/* The current plan is not actionable: there is nothing to change it to. */}
      <CardActionArea
        disabled={option.isCurrent}
        onClick={() => onSelect(option)}
        sx={{ p: 2, height: '100%', alignItems: 'flex-start' }}
      >
        <Stack spacing={1.5} sx={{ width: '100%' }}>
          <Stack direction="row" sx={{ alignItems: 'center', justifyContent: 'space-between' }}>
            <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }}>
              <MaterialSymbol
                name={option.isCurrent ? 'radio_button_checked' : 'radio_button_unchecked'}
                size={20}
                sx={{ color: option.isCurrent ? 'primary.main' : 'text.secondary' }}
              />
              <Typography variant="subtitle1">{option.plan}</Typography>
            </Stack>
            <Chip
              size="small"
              label={badge}
              color={option.isCurrent ? 'primary' : 'default'}
              variant={option.isCurrent ? 'filled' : 'outlined'}
            />
          </Stack>

          <Stack direction="row" spacing={0.5} sx={{ alignItems: 'baseline' }}>
            <Typography variant="h5">{money(option.priceCents)}</Typography>
            <Typography variant="body2" color="text.secondary">
              / month
            </Typography>
          </Stack>

          <Typography variant="body2" color="text.secondary">
            {PLAN_SUMMARY[option.plan]}
          </Typography>

          <Box component="ul" sx={{ m: 0, pl: 0, listStyle: 'none' }}>
            {PLAN_BULLETS[option.plan].map((bullet) => (
              <Stack
                key={bullet}
                component="li"
                direction="row"
                spacing={1}
                sx={{ alignItems: 'flex-start', mb: 0.5 }}
              >
                <MaterialSymbol name="check" size={16} sx={{ color: 'primary.main', mt: '2px' }} />
                <Typography variant="body2">{bullet}</Typography>
              </Stack>
            ))}
          </Box>
        </Stack>
      </CardActionArea>
    </Card>
  );
};
