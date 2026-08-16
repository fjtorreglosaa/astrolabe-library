using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Astrolabe.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// GLOBAL-019. Collapses the three member roles into one.
    ///
    /// <para>
    /// <c>user_role</c> used to hold <c>Basic = 0, Plus = 1, Max = 2</c> alongside the staff values,
    /// so a member's role doubled as their subscription. It now holds <c>Member = 0</c> and says
    /// nothing about what anyone bought; <c>subscriptions.plan</c> is the sole authority.
    /// </para>
    /// <para>
    /// No schema change — the column keeps its type and its name. The whole migration is data, and
    /// it is written in two steps whose order matters: rescue the plan first, then discard the
    /// column that carried it.
    /// </para>
    /// </summary>
    public partial class SeparateRoleFromPlan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Step 1 — rescue. A member on role 1 or 2 with no active subscription holds their plan
            // in exactly one place, and step 2 is about to overwrite it. In practice MembershipSeeder
            // has already given every member a subscription, so this should match nothing; it is
            // written anyway because "should match nothing" and "cannot lose a paid plan" are not
            // the same guarantee, and only the second one is worth having in a migration.
            migrationBuilder.Sql(
                """
                INSERT INTO astrolabe.subscriptions (
                    id, member_id, plan, cycle_started_on, cycle_renews_on, cycle_anchor_day,
                    started_at, ended_at, city_changes_this_cycle)
                SELECT
                    gen_random_uuid(),
                    u.id,
                    CASE u.role WHEN 1 THEN 'Plus' ELSE 'Max' END,
                    now(),
                    now() + interval '1 month',
                    EXTRACT(DAY FROM now())::int,
                    now(),
                    NULL,
                    0
                FROM astrolabe.users u
                WHERE u.role IN (1, 2)
                  AND NOT EXISTS (
                      SELECT 1 FROM astrolabe.subscriptions s
                      WHERE s.member_id = u.id AND s.ended_at IS NULL);
                """);

            // Step 2 — collapse. Staff values (10, 20) are deliberately untouched, and 0 already
            // means what it will go on meaning, so only the two paid tiers move.
            migrationBuilder.Sql("UPDATE astrolabe.users SET role = 0 WHERE role IN (1, 2);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Genuinely reversible, and only because of step 1 above: every member that had a paid
            // tier now has a subscription recording it, so the old encoding can be reconstructed
            // rather than guessed. A member who has since downgraded comes back as 0, which is
            // correct — the subscription was always going to be the more current of the two.
            migrationBuilder.Sql(
                """
                UPDATE astrolabe.users u
                SET role = CASE s.plan WHEN 'Plus' THEN 1 WHEN 'Max' THEN 2 ELSE 0 END
                FROM astrolabe.subscriptions s
                WHERE s.member_id = u.id
                  AND s.ended_at IS NULL
                  AND u.role = 0;
                """);
        }
    }
}
