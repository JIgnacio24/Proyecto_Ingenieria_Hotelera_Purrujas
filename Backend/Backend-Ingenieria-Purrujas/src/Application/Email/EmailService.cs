using System.Globalization;
using Backend_Ingenieria_Purrujas.Application.Reservations.Dtos;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace Backend_Ingenieria_Purrujas.Application.Email;

public class EmailService(IConfiguration config, ILogger<EmailService> logger) : IEmailService
{
    private static readonly CultureInfo EsCr = new("es-CR");

    public async Task SendReservationConfirmationAsync(
        ReservationResponseDto reservation,
        CancellationToken cancellationToken = default)
    {
        var host = config["Smtp:Host"];
        var port = int.TryParse(config["Smtp:Port"], out var p) ? p : 587;
        var username = config["Smtp:Username"];
        var password = config["Smtp:Password"];
        var fromName = config["Smtp:FromName"] ?? "Hotel Las Purrujas";
        var fromAddress = config["Smtp:FromAddress"] ?? username;

        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning("SMTP no configurado — correo de confirmación omitido para reserva #{Id}.", reservation.ReservationId);
            return;
        }

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(fromName, fromAddress));
        message.To.Add(new MailboxAddress(reservation.CustomerFullName, reservation.CustomerEmail));
        message.Subject = $"Reserva #{reservation.ReservationId} recibida – Hotel Las Purrujas";
        message.Body = new TextPart("html") { Text = BuildHtml(reservation) };

        using var client = new SmtpClient();
        await client.ConnectAsync(host, port, SecureSocketOptions.Auto, cancellationToken);
        await client.AuthenticateAsync(username, password, cancellationToken);
        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);

        logger.LogInformation("Correo de confirmación enviado a {Email} para reserva #{Id}.",
            reservation.CustomerEmail, reservation.ReservationId);
    }

    public async Task SendReservationStatusUpdateAsync(
        ReservationResponseDto reservation,
        CancellationToken cancellationToken = default)
    {
        var host = config["Smtp:Host"];
        var port = int.TryParse(config["Smtp:Port"], out var p) ? p : 587;
        var username = config["Smtp:Username"];
        var password = config["Smtp:Password"];
        var fromName = config["Smtp:FromName"] ?? "Hotel Las Purrujas";
        var fromAddress = config["Smtp:FromAddress"] ?? username;

        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning("SMTP no configurado — correo de estado omitido para reserva #{Id}.", reservation.ReservationId);
            return;
        }

        var subject = reservation.Status == "Confirmada"
            ? $"¡Tu reserva #{reservation.ReservationId} fue confirmada! – Hotel Las Purrujas"
            : $"Reserva #{reservation.ReservationId} cancelada – Hotel Las Purrujas";

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(fromName, fromAddress));
        message.To.Add(new MailboxAddress(reservation.CustomerFullName, reservation.CustomerEmail));
        message.Subject = subject;
        message.Body = new TextPart("html") { Text = BuildStatusHtml(reservation) };

        using var client = new SmtpClient();
        await client.ConnectAsync(host, port, SecureSocketOptions.Auto, cancellationToken);
        await client.AuthenticateAsync(username, password, cancellationToken);
        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);

        logger.LogInformation("Correo de estado '{Status}' enviado a {Email} para reserva #{Id}.",
            reservation.Status, reservation.CustomerEmail, reservation.ReservationId);
    }

    private static string BuildHtml(ReservationResponseDto r)
    {
        var total = r.Currency == "CRC"
            ? $"&#8353;{r.TotalCrc:N2}"
            : $"${r.TotalUsd:N2}";

        var entrada = r.StartDate.ToString("dd 'de' MMMM 'de' yyyy", EsCr);
        var salida  = r.EndDate.ToString("dd 'de' MMMM 'de' yyyy", EsCr);

        return $"""
        <!DOCTYPE html>
        <html lang="es">
        <head><meta charset="utf-8"><title>Confirmación de reserva</title></head>
        <body style="margin:0;padding:0;background:#f5f5f0;font-family:Arial,sans-serif;">
          <table width="100%" cellpadding="0" cellspacing="0" style="background:#f5f5f0;padding:40px 0;">
            <tr><td align="center">
              <table width="600" cellpadding="0" cellspacing="0"
                style="background:#ffffff;border-radius:8px;overflow:hidden;box-shadow:0 2px 10px rgba(0,0,0,0.08);">

                <!-- Encabezado -->
                <tr><td style="background:#4a5c3e;padding:36px;text-align:center;">
                  <h1 style="margin:0;color:#ffffff;font-size:26px;letter-spacing:1px;">Hotel Las Purrujas</h1>
                  <p style="margin:10px 0 0;color:#c8d5b9;font-size:15px;">Solicitud de reserva recibida</p>
                </td></tr>

                <!-- Cuerpo -->
                <tr><td style="padding:36px;">
                  <p style="color:#333;font-size:16px;margin-top:0;">
                    Hola <strong>{r.CustomerFullName}</strong>,
                  </p>
                  <p style="color:#555;font-size:15px;line-height:1.7;margin-bottom:24px;">
                    Hemos recibido tu solicitud de reserva en el Hotel Las Purrujas.
                    Un representante se comunicará contigo en las próximas horas para confirmarla.
                  </p>

                  <!-- Número de reserva -->
                  <div style="background:#f0ebe0;border-left:4px solid #8a7050;padding:18px 20px;border-radius:4px;margin-bottom:28px;">
                    <p style="margin:0;font-size:12px;color:#7a6040;text-transform:uppercase;letter-spacing:0.8px;">Número de reserva</p>
                    <p style="margin:6px 0 0;font-size:30px;font-weight:bold;color:#4a3020;">#{r.ReservationId}</p>
                  </div>

                  <!-- Detalles -->
                  <table width="100%" cellpadding="12" cellspacing="0" style="border-collapse:collapse;">
                    <tr style="border-bottom:1px solid #e8e0d0;">
                      <td style="color:#777;font-size:14px;">Habitación</td>
                      <td style="color:#333;font-size:14px;text-align:right;">
                        <strong>{r.RoomTypeName}</strong> &mdash; {r.RoomNumber}
                      </td>
                    </tr>
                    <tr style="border-bottom:1px solid #e8e0d0;">
                      <td style="color:#777;font-size:14px;">Fecha de entrada</td>
                      <td style="color:#333;font-size:14px;text-align:right;">{entrada}</td>
                    </tr>
                    <tr style="border-bottom:1px solid #e8e0d0;">
                      <td style="color:#777;font-size:14px;">Fecha de salida</td>
                      <td style="color:#333;font-size:14px;text-align:right;">{salida}</td>
                    </tr>
                    <tr style="border-bottom:1px solid #e8e0d0;">
                      <td style="color:#777;font-size:14px;">Noches</td>
                      <td style="color:#333;font-size:14px;text-align:right;">{r.NightsTotal}</td>
                    </tr>
                    <tr style="background:#f9f6f0;">
                      <td style="color:#4a3020;font-size:15px;font-weight:bold;padding:16px 12px;">Total estimado</td>
                      <td style="color:#4a3020;font-size:20px;font-weight:bold;text-align:right;padding:16px 12px;">{total}</td>
                    </tr>
                  </table>

                  <p style="color:#888;font-size:13px;margin-top:28px;line-height:1.6;">
                    Tu reserva está en estado <strong>Pendiente</strong>. Recibirás confirmación cuando un
                    representante la procese. Si tienes alguna consulta, no dudes en contactarnos.
                  </p>
                </td></tr>

                <!-- Pie -->
                <tr><td style="background:#f0ebe0;padding:24px;text-align:center;">
                  <p style="margin:0;color:#8a7050;font-size:13px;">Hotel Las Purrujas &bull; Costa Rica</p>
                  <p style="margin:8px 0 0;color:#aaa;font-size:12px;">&#169; 2026 Hotel Las Purrujas. Todos los derechos reservados.</p>
                </td></tr>

              </table>
            </td></tr>
          </table>
        </body>
        </html>
        """;
    }

    private static string BuildStatusHtml(ReservationResponseDto r)
    {
        var confirmada = r.Status == "Confirmada";
        var headerColor  = confirmada ? "#2d6a4f" : "#7b2d2d";
        var badgeColor   = confirmada ? "#d8f3dc" : "#fde8e8";
        var badgeText    = confirmada ? "#1b4332" : "#7b2d2d";
        var badgeBorder  = confirmada ? "#52b788" : "#e57373";
        var badgeLabel   = confirmada ? "✓ Reserva Confirmada" : "✗ Reserva Cancelada";

        var entrada = r.StartDate.ToString("dd 'de' MMMM 'de' yyyy", EsCr);
        var salida  = r.EndDate.ToString("dd 'de' MMMM 'de' yyyy", EsCr);
        var total   = r.Currency == "CRC" ? $"&#8353;{r.TotalCrc:N2}" : $"${r.TotalUsd:N2}";

        var bodyMessage = confirmada
            ? $"Nos complace informarte que tu reserva ha sido <strong>confirmada</strong>. Te esperamos con gusto en las fechas indicadas."
            : $"Lamentamos informarte que tu reserva ha sido <strong>cancelada</strong>. Si tienes alguna consulta, no dudes en contactarnos.";

        return $"""
        <!DOCTYPE html>
        <html lang="es">
        <head><meta charset="utf-8"><title>Actualización de reserva</title></head>
        <body style="margin:0;padding:0;background:#f5f5f0;font-family:Arial,sans-serif;">
          <table width="100%" cellpadding="0" cellspacing="0" style="background:#f5f5f0;padding:40px 0;">
            <tr><td align="center">
              <table width="600" cellpadding="0" cellspacing="0"
                style="background:#ffffff;border-radius:8px;overflow:hidden;box-shadow:0 2px 10px rgba(0,0,0,0.08);">

                <!-- Encabezado -->
                <tr><td style="background:{headerColor};padding:36px;text-align:center;">
                  <h1 style="margin:0;color:#ffffff;font-size:26px;letter-spacing:1px;">Hotel Las Purrujas</h1>
                  <p style="margin:10px 0 0;color:#c8d5b9;font-size:15px;">Actualización de tu reserva</p>
                </td></tr>

                <!-- Cuerpo -->
                <tr><td style="padding:36px;">
                  <p style="color:#333;font-size:16px;margin-top:0;">
                    Hola <strong>{r.CustomerFullName}</strong>,
                  </p>
                  <p style="color:#555;font-size:15px;line-height:1.7;margin-bottom:24px;">{bodyMessage}</p>

                  <!-- Badge de estado -->
                  <div style="background:{badgeColor};border:1px solid {badgeBorder};padding:14px 20px;border-radius:6px;margin-bottom:28px;text-align:center;">
                    <span style="color:{badgeText};font-size:16px;font-weight:bold;">{badgeLabel}</span>
                  </div>

                  <!-- Número de reserva -->
                  <div style="background:#f0ebe0;border-left:4px solid #8a7050;padding:14px 20px;border-radius:4px;margin-bottom:24px;">
                    <p style="margin:0;font-size:12px;color:#7a6040;text-transform:uppercase;letter-spacing:0.8px;">Número de reserva</p>
                    <p style="margin:6px 0 0;font-size:28px;font-weight:bold;color:#4a3020;">#{r.ReservationId}</p>
                  </div>

                  <!-- Detalles -->
                  <table width="100%" cellpadding="12" cellspacing="0" style="border-collapse:collapse;">
                    <tr style="border-bottom:1px solid #e8e0d0;">
                      <td style="color:#777;font-size:14px;">Habitación</td>
                      <td style="color:#333;font-size:14px;text-align:right;">
                        <strong>{r.RoomTypeName}</strong> &mdash; {r.RoomNumber}
                      </td>
                    </tr>
                    <tr style="border-bottom:1px solid #e8e0d0;">
                      <td style="color:#777;font-size:14px;">Fecha de entrada</td>
                      <td style="color:#333;font-size:14px;text-align:right;">{entrada}</td>
                    </tr>
                    <tr style="border-bottom:1px solid #e8e0d0;">
                      <td style="color:#777;font-size:14px;">Fecha de salida</td>
                      <td style="color:#333;font-size:14px;text-align:right;">{salida}</td>
                    </tr>
                    <tr style="border-bottom:1px solid #e8e0d0;">
                      <td style="color:#777;font-size:14px;">Noches</td>
                      <td style="color:#333;font-size:14px;text-align:right;">{r.NightsTotal}</td>
                    </tr>
                    <tr style="background:#f9f6f0;">
                      <td style="color:#4a3020;font-size:15px;font-weight:bold;padding:16px 12px;">Total estimado</td>
                      <td style="color:#4a3020;font-size:20px;font-weight:bold;text-align:right;padding:16px 12px;">{total}</td>
                    </tr>
                  </table>

                  <p style="color:#888;font-size:13px;margin-top:28px;line-height:1.6;">
                    Si tienes alguna consulta sobre tu reserva, no dudes en contactarnos.
                  </p>
                </td></tr>

                <!-- Pie -->
                <tr><td style="background:#f0ebe0;padding:24px;text-align:center;">
                  <p style="margin:0;color:#8a7050;font-size:13px;">Hotel Las Purrujas &bull; Costa Rica</p>
                  <p style="margin:8px 0 0;color:#aaa;font-size:12px;">&#169; 2026 Hotel Las Purrujas. Todos los derechos reservados.</p>
                </td></tr>

              </table>
            </td></tr>
          </table>
        </body>
        </html>
        """;
    }
}
