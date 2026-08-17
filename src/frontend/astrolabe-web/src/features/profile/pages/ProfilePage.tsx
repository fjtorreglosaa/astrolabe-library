import {
  Avatar,
  Chip,
  Divider,
  Pagination,
  Paper,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  Typography,
} from "@mui/material";
import { useQuery } from "@tanstack/react-query";
import { useState } from "react";
import { MaterialSymbol } from "../../../shared/components/MaterialSymbol";
import {
  EmptyState,
  ErrorState,
  LoadingState,
} from "../../../shared/components/StateViews";
import { useAuth } from "../../auth/components/AuthProvider";
import { getMyFines, getMyLedger } from "../../billing/api/billingApi";
import {
  formatDate,
  money,
  pendingChangeLine,
  planStatusLine,
} from "../../membership/planCopy";
import { getMyMembership } from "../../membership/api/membershipApi";
import {
  getDashboard,
  getMyReservations,
} from "../../reservations/api/reservationsApi";
import { getMyPoints } from "../../store/api/storeApi";
import { pointsAsMoney } from "../../store/storeCopy";

const LEDGER_PAGE_SIZE = 10;

/**
 * My profile.
 *
 * <p>
 * Everything here is read from a domain that already owns it — membership for the plan, billing for
 * the balance and the statement, reservations for the history and the topics, store for the points.
 * Nothing is recomputed: a figure shown here that disagreed with the screen that owns it would be
 * worse than not showing it.
 * </p>
 * <p>
 * The topics are <b>derived from what the member actually borrowed</b> rather than declared. The
 * prototype shows them as a list of interests; deriving them means they cannot go stale, and a
 * member who never sets them still has some.
 * </p>
 */
export const ProfilePage = () => {
  const { user, plan } = useAuth();
  const [ledgerPage, setLedgerPage] = useState(1);

  // No staff branch: the route is behind `MemberRoles`, and every one of these six endpoints
  // answers 403 to staff anyway.
  const membership = useQuery({
    queryKey: ["membership"],
    queryFn: getMyMembership,
  });
  const dashboard = useQuery({
    queryKey: ["reservations", "dashboard"],
    queryFn: getDashboard,
  });
  const fines = useQuery({
    queryKey: ["billing", "fines"],
    queryFn: getMyFines,
  });
  const points = useQuery({
    queryKey: ["store", "points"],
    queryFn: getMyPoints,
  });
  const ledger = useQuery({
    queryKey: ["billing", "ledger", ledgerPage],
    queryFn: () => getMyLedger(ledgerPage, LEDGER_PAGE_SIZE),
  });
  const history = useQuery({
    queryKey: ["reservations", "history"],
    queryFn: () => getMyReservations("Returned", 1, 8),
  });

  if (!user) {
    return <LoadingState label="Loading your profile…" />;
  }

  const initials = user.fullName
    .split(" ")
    .filter(Boolean)
    .slice(0, 2)
    .map((part) => part[0]?.toUpperCase() ?? "")
    .join("");

  const owed = fines.data?.outstandingCents ?? 0;

  // The prototype's own rows, in its own order.
  const rows: { label: string; value: string; tone?: "good" | "bad" }[] = [
    {
      label: "Plan",
      value: `${plan ?? "—"}${membership.data?.cityName ? ` · ${membership.data.cityName}` : ""}`,
      tone: "good",
    },
    {
      // Both lines come from `planCopy`, which the membership screen already uses. Rewriting
      // them here would let the same fact be worded two ways, and the free-plan case is decided
      // on the price rather than on the plan's name — a plan that becomes free still reads right.
      label: "Plan valid until",
      value: membership.data ? planStatusLine(membership.data) : "—",
    },
    {
      label: "Next renewal",
      value: membership.data
        ? (pendingChangeLine(membership.data) ?? "Nothing scheduled")
        : "—",
    },
    {
      label: "Reward points",
      // Both the count and what it is worth, the way the purchases screen puts it. A bare
      // number of points does not tell a member what they have.
      value: points.data
        ? points.data.balancePointCents > 0
          ? `${points.data.balancePointCents} pts · ${pointsAsMoney(points.data.balancePointCents)}`
          : "None yet"
        : "—",
    },
    {
      label: "Account status",
      value: owed > 0 ? "Fine outstanding" : "All settled",
      tone: owed > 0 ? "bad" : "good",
    },
    {
      label: "Fines owed",
      value: money(owed),
      tone: owed > 0 ? "bad" : "good",
    },
    {
      label: "Books reserved",
      value: dashboard.data ? String(dashboard.data.activeReservations) : "—",
    },
    {
      label: "Read this year",
      value: dashboard.data ? String(dashboard.data.readThisYear) : "—",
    },
    {
      label: "Returned all time",
      value: dashboard.data ? String(dashboard.data.returnedAllTime) : "—",
    },
  ];

  return (
    <Stack spacing={4}>
      <Stack direction="row" spacing={2} sx={{ alignItems: "center" }}>
        <Avatar
          sx={{ width: 64, height: 64, bgcolor: "primary.main", fontSize: 24 }}
        >
          {initials}
        </Avatar>
        <Stack spacing={0.25}>
          <Typography variant="h4">{user.fullName}</Typography>
          <Typography variant="body2" color="text.secondary">
            {user.email}
          </Typography>
        </Stack>
      </Stack>

      <Paper variant="outlined" sx={{ p: 2.5 }}>
        <Stack spacing={1.25}>
          {rows.map((row) => (
            <Stack
              key={row.label}
              direction="row"
              spacing={2}
              sx={{ justifyContent: "space-between" }}
            >
              <Typography variant="body2" color="text.secondary">
                {row.label}
              </Typography>
              <Typography
                variant="body2"
                sx={{ textAlign: "right" }}
                color={
                  row.tone === "bad"
                    ? "error.main"
                    : row.tone === "good"
                      ? "success.main"
                      : undefined
                }
              >
                {row.value}
              </Typography>
            </Stack>
          ))}
        </Stack>
      </Paper>

      <Stack spacing={1.5}>
        <Stack spacing={0.25}>
          <Typography variant="h6">Preferred topics</Typography>
          <Typography variant="body2" color="text.secondary">
            Worked out from what you have borrowed, so they stay current without
            you maintaining them. They are what the recommendations read.
          </Typography>
        </Stack>
        {dashboard.data && dashboard.data.topics.length > 0 ? (
          <Stack direction="row" spacing={1} sx={{ flexWrap: "wrap", gap: 1 }}>
            {dashboard.data.topics.map((topic) => (
              <Chip
                key={topic.genre}
                variant="outlined"
                label={`${topic.genre} · ${topic.percent}%`}
              />
            ))}
          </Stack>
        ) : (
          <Typography variant="body2" color="text.secondary">
            Borrow a few books and your topics appear here.
          </Typography>
        )}
      </Stack>

      <Divider />

      <Stack spacing={1.5}>
        <Typography variant="h6">Reading history</Typography>
        {history.isLoading ? (
          <LoadingState label="Loading your history…" />
        ) : !history.data || history.data.items.length === 0 ? (
          <EmptyState
            title="Nothing returned yet"
            description="Books you have finished and returned show up here."
          />
        ) : (
          <Stack spacing={1}>
            {history.data.items.map((reservation) => (
              <Stack
                key={reservation.id}
                direction="row"
                spacing={2}
                sx={{ justifyContent: "space-between", alignItems: "center" }}
              >
                <Stack spacing={0.25} sx={{ minWidth: 0 }}>
                  <Typography variant="body2" noWrap>
                    {reservation.title}
                  </Typography>
                  <Typography variant="caption" color="text.secondary" noWrap>
                    {reservation.author}
                  </Typography>
                </Stack>
                <Stack
                  direction="row"
                  spacing={0.5}
                  sx={{ alignItems: "center" }}
                >
                  <MaterialSymbol
                    name={reservation.isOverdue ? "schedule" : "task_alt"}
                    size={16}
                    sx={{
                      color: reservation.isOverdue
                        ? "error.main"
                        : "success.main",
                    }}
                  />
                  <Typography variant="caption" color="text.secondary">
                    {formatDate(reservation.dueOn)}
                  </Typography>
                </Stack>
              </Stack>
            ))}
          </Stack>
        )}
      </Stack>

      <Divider />

      <Stack spacing={1.5}>
        <Stack spacing={0.25}>
          <Typography variant="h6">Account statement</Typography>
          <Typography variant="body2" color="text.secondary">
            Every charge and every payment, newest first.
          </Typography>
        </Stack>

        {ledger.isLoading ? (
          <LoadingState label="Loading your statement…" />
        ) : ledger.isError || !ledger.data ? (
          <ErrorState
            description="We could not load your statement."
            onRetry={() => void ledger.refetch()}
          />
        ) : ledger.data.items.length === 0 ? (
          <EmptyState
            title="Nothing on your account"
            description="Charges and payments appear here as they happen."
          />
        ) : (
          <>
            <Paper variant="outlined" sx={{ overflowX: "auto" }}>
              <Table size="small">
                <TableHead>
                  <TableRow>
                    <TableCell>Date</TableCell>
                    <TableCell>What</TableCell>
                    <TableCell>Kind</TableCell>
                    <TableCell align="right">Amount</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {ledger.data.items.map((entry) => (
                    <TableRow key={entry.id} hover>
                      <TableCell>{formatDate(entry.occurredAt)}</TableCell>
                      <TableCell>{entry.description}</TableCell>
                      <TableCell>
                        <Chip
                          size="small"
                          variant="outlined"
                          label={entry.kind}
                        />
                      </TableCell>
                      <TableCell
                        align="right"
                        // The amount arrives signed — a charge is negative — so the sign is the
                        // server's answer rather than this table guessing which kinds are debits.
                        sx={{
                          color:
                            entry.amountCents < 0
                              ? "error.main"
                              : "success.main",
                        }}
                      >
                        {money(Math.abs(entry.amountCents))}
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </Paper>

            {ledger.data.totalPages > 1 ? (
              <Stack sx={{ alignItems: "center" }}>
                <Pagination
                  count={ledger.data.totalPages}
                  page={ledger.data.page}
                  onChange={(_event, next) => setLedgerPage(next)}
                  color="primary"
                />
              </Stack>
            ) : null}
          </>
        )}
      </Stack>
    </Stack>
  );
};
