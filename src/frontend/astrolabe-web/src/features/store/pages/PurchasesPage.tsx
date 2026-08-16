import {
  Alert,
  Box,
  Chip,
  Pagination,
  Paper,
  Stack,
  Typography,
} from '@mui/material';
import { useQuery } from '@tanstack/react-query';
import { useState } from 'react';
import { MaterialSymbol } from '../../../shared/components/MaterialSymbol';
import { EmptyState, ErrorState, LoadingState } from '../../../shared/components/StateViews';
import { formatDate, money } from '../../membership/planCopy';
import { getMyOrders, getMyPoints } from '../api/storeApi';
import {
  FULFILMENT_LABEL,
  REDEMPTION_FLOOR_NOTE,
  REDEMPTION_NEEDS_MAX_NOTE,
  pointsAsMoney,
} from '../storeCopy';
import { useAuth } from '../../auth/components/AuthProvider';

const PAGE_SIZE = 10;

/**
 * My purchases — orders and the reward balance they earned.
 *
 * The balance is shown and cannot be spent. Saying so plainly is better than leaving a number on a
 * screen with no way to use it and no explanation: the points are safe, and a member is entitled to
 * know that rather than to wonder.
 */
export const PurchasesPage = () => {
  const [page, setPage] = useState(1);
  const { plan } = useAuth();

  const orders = useQuery({
    queryKey: ['store', 'orders', page],
    queryFn: () => getMyOrders(page, PAGE_SIZE),
  });

  const points = useQuery({ queryKey: ['store', 'points'], queryFn: getMyPoints });

  return (
    <Stack spacing={3}>
      <Stack spacing={0.5}>
        <Typography variant="h4">My purchases</Typography>
        <Typography variant="body2" color="text.secondary">
          Books you own, and what they earned you.
        </Typography>
      </Stack>

      {points.data ? (
        <Paper variant="outlined" sx={{ p: 2.5 }}>
          <Stack
            direction={{ xs: 'column', sm: 'row' }}
            spacing={2}
            sx={{ alignItems: { sm: 'center' }, justifyContent: 'space-between' }}
          >
            <Stack spacing={0.5}>
              <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }}>
                <MaterialSymbol name="stars" size={20} sx={{ color: 'primary.main' }} />
                <Typography variant="overline" color="text.secondary">
                  Reward points
                </Typography>
              </Stack>
              <Typography variant="h3">
                {points.data.balancePointCents}
                <Typography component="span" variant="body1" color="text.secondary" sx={{ ml: 1 }}>
                  pts · {pointsAsMoney(points.data.balancePointCents)}
                </Typography>
              </Typography>
              <Typography variant="caption" color="text.secondary">
                {points.data.note}
              </Typography>
            </Stack>

            <Stack direction="row" spacing={1}>
              {/* Two facts, not one. Earning is a Max benefit; spending what you already earned is
                  open to every plan, which is what makes BR-STR-008 mean anything. */}
              <Chip
                size="small"
                variant="outlined"
                color={points.data.earnsPoints ? 'primary' : 'default'}
                label={points.data.earnsPoints ? 'Earning' : 'Earning: Max only'}
              />
              {points.data.canRedeem ? (
                <Chip size="small" variant="outlined" color="success" label="Spendable" />
              ) : null}
            </Stack>
          </Stack>

          {/* A balance that cannot be spent needs a reason, or it reads as a fault — and the two
              reasons are different. Too few points is a matter of time; the wrong plan is
              BR-STR-008, and that sentence has to say the points are safe. */}
          {points.data.balancePointCents > 0 && !points.data.canRedeem ? (
            <Alert severity="info" sx={{ mt: 2 }} icon={<MaterialSymbol name="schedule" size={20} />}>
              {plan === 'Max' ? REDEMPTION_FLOOR_NOTE : REDEMPTION_NEEDS_MAX_NOTE}
            </Alert>
          ) : null}
        </Paper>
      ) : null}

      {orders.isLoading ? (
        <LoadingState label="Loading your purchases…" />
      ) : orders.isError || !orders.data ? (
        <ErrorState
          description="We could not load your purchases."
          onRetry={() => void orders.refetch()}
        />
      ) : orders.data.items.length === 0 ? (
        <EmptyState
          title="Nothing bought yet"
          description="Open a book in the catalogue and buy your own copy."
        />
      ) : (
        <>
          <Stack spacing={2}>
            {orders.data.items.map((order) => (
              <Paper key={order.id} variant="outlined" sx={{ p: 2 }}>
                <Stack spacing={1.5}>
                  <Stack
                    direction="row"
                    spacing={2}
                    sx={{ justifyContent: 'space-between', alignItems: 'flex-start' }}
                  >
                    <Stack spacing={0.25} sx={{ minWidth: 0 }}>
                      <Typography variant="subtitle2">{order.description}</Typography>
                      <Typography variant="caption" color="text.secondary">
                        {formatDate(order.placedAt)} · {FULFILMENT_LABEL[order.fulfilment]}
                      </Typography>
                    </Stack>
                    <Typography variant="h6">{money(order.totalCents)}</Typography>
                  </Stack>

                  <Box sx={{ pl: 1, borderLeft: 2, borderColor: 'divider' }}>
                    {order.lines.map((line) => (
                      <Stack
                        key={line.bookId}
                        direction="row"
                        spacing={2}
                        sx={{ justifyContent: 'space-between' }}
                      >
                        <Typography variant="body2" color="text.secondary" noWrap>
                          {line.quantity > 1 ? `${line.quantity} × ` : ''}
                          {line.bookTitle}
                        </Typography>
                        <Stack direction="row" spacing={1}>
                          {line.discountPercent > 0 ? (
                            <Typography variant="caption" color="success.main">
                              −{line.discountPercent}%
                            </Typography>
                          ) : null}
                          <Typography variant="body2">{money(line.lineTotalCents)}</Typography>
                        </Stack>
                      </Stack>
                    ))}
                  </Box>

                  <Stack direction="row" spacing={2} sx={{ flexWrap: 'wrap', gap: 1 }}>
                    {order.discountTotalCents > 0 ? (
                      <Chip
                        size="small"
                        color="success"
                        variant="outlined"
                        label={`Saved ${money(order.discountTotalCents)}`}
                      />
                    ) : null}
                    {order.shippingFeeCents > 0 ? (
                      <Chip
                        size="small"
                        variant="outlined"
                        label={`Delivery ${money(order.shippingFeeCents)}`}
                      />
                    ) : null}
                    {order.pointsRedeemed > 0 ? (
                      <Chip
                        size="small"
                        color="secondary"
                        variant="outlined"
                        label={`−${order.pointsRedeemed} pts · paid ${money(order.amountChargedCents)}`}
                      />
                    ) : null}
                    {order.pointsEarned > 0 ? (
                      <Chip
                        size="small"
                        color="primary"
                        variant="outlined"
                        label={`+${order.pointsEarned} pts`}
                      />
                    ) : null}
                  </Stack>
                </Stack>
              </Paper>
            ))}
          </Stack>

          {orders.data.totalPages > 1 ? (
            <Stack sx={{ alignItems: 'center' }}>
              <Pagination
                count={orders.data.totalPages}
                page={orders.data.page}
                onChange={(_event, next) => setPage(next)}
                color="primary"
              />
            </Stack>
          ) : null}
        </>
      )}
    </Stack>
  );
};
