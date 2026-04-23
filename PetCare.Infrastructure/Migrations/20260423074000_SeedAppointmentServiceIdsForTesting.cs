using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PetCare.Infrastructure.Data;

#nullable disable

namespace PetCare.Infrastructure.Migrations;

[DbContext(typeof(PetCareDbContext))]
[Migration("20260423074000_SeedAppointmentServiceIdsForTesting")]
public class SeedAppointmentServiceIdsForTesting : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
            UPDATE petcare.appointments AS appointment
            SET service_id = source.service_id,
                service_name = COALESCE(appointment.service_name, source.service_name),
                updated_at = NOW()
            FROM (
                SELECT DISTINCT ON (used_service.appointment_id)
                    used_service.appointment_id,
                    used_service.service_id,
                    used_service.service_name
                FROM petcare.appointment_used_services AS used_service
                ORDER BY used_service.appointment_id, used_service.created_at, used_service.id
            ) AS source
            WHERE appointment.id = source.appointment_id
              AND appointment.service_id IS NULL;

            WITH service_pool AS (
                SELECT
                    service.id,
                    service.service_name,
                    ROW_NUMBER() OVER (ORDER BY service.created_at, service.id) AS rn,
                    COUNT(*) OVER () AS total_services
                FROM petcare.services AS service
                WHERE service.is_active = TRUE
            ),
            appointments_without_service AS (
                SELECT
                    appointment.id,
                    ROW_NUMBER() OVER (ORDER BY appointment.appointment_date, appointment.id) AS rn
                FROM petcare.appointments AS appointment
                WHERE appointment.service_id IS NULL
            )
            UPDATE petcare.appointments AS appointment
            SET service_id = service_pool.id,
                service_name = COALESCE(appointment.service_name, service_pool.service_name),
                updated_at = NOW()
            FROM appointments_without_service
            JOIN service_pool
                ON service_pool.rn = ((appointments_without_service.rn - 1) % service_pool.total_services) + 1
            WHERE appointment.id = appointments_without_service.id
              AND service_pool.total_services > 0;
        ");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // No-op. This migration seeds test data only.
    }
}
