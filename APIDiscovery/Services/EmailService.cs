using System.Net;
using System.Net.Mail;

namespace APIDiscovery.Services;

    public class EmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendVerificationCodeAsync(string email, string code)
        {
            var smtpSettings = _configuration.GetSection("SmtpSettings");
            var client = new SmtpClient(smtpSettings["Server"])
            {
                Port = int.Parse(smtpSettings["Port"]),
                Credentials = new NetworkCredential(smtpSettings["Username"], smtpSettings["Password"]),
                EnableSsl = bool.Parse(smtpSettings["EnableSsl"])
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(smtpSettings["FromEmail"], smtpSettings["FromName"]),
                Subject = "Código de verificación para restablecer contraseña",
                Body = $@"
                    <html>
                    <body style='font-family: Arial, sans-serif; color: #333;'>
                        <div style='max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #ddd; border-radius: 5px;'>
                            <h2 style='color: #4285f4;'>Código de verificación</h2>
                            <p>Hemos recibido una solicitud para restablecer tu contraseña.</p>
                            <p>Tu código de verificación es:</p>
                            <div style='background-color: #f5f5f5; padding: 15px; text-align: center; font-size: 24px; letter-spacing: 5px; font-weight: bold; margin: 20px 0;'>
                                {code}
                            </div>
                            <p>Este código es válido por 15 minutos.</p>
                            <p>Si no solicitaste restablecer tu contraseña, puedes ignorar este correo.</p>
                            <p>Saludos,<br>El equipo de matticry</p>
                        </div>
                    </body>
                    </html>
                ",
                IsBodyHtml = true
            };

            mailMessage.To.Add(email);
            await client.SendMailAsync(mailMessage);
        }
    }