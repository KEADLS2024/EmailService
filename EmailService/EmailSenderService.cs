using MimeKit;
using MailKit.Net.Smtp;
using MailKit.Security;



using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using Microsoft.Extensions.Hosting;

namespace EmailService;

public class EmailSenderService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory() { HostName = "localhost" };
        using (var connection = factory.CreateConnection())
        using (var channel = connection.CreateModel())
        {
            channel.QueueDeclare(queue: "UserEmailQueue", durable: false, exclusive: false, autoDelete: false, arguments: null);

            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var emailAddress = Encoding.UTF8.GetString(body);
                SendEmail(emailAddress); // Implementér logikken til at sende e-mail her
            };
            channel.BasicConsume(queue: "UserEmailQueue", autoAck: true, consumer: consumer);

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
            }
        }
    }

    private void SendEmail(string toAddress)
    {
        var email = new MimeMessage();
        email.From.Add(new MailboxAddress("Your Name or Company", "usama-ahmad@outlook.com")); // Ændre afsenderens detaljer
        email.To.Add(MailboxAddress.Parse(toAddress));
        email.Subject = "Test Email from Your Service";
        email.Body = new TextPart("plain") { Text = "Hello, this is a test email from your service." };

        // Opsæt SMTP klient
        using var smtp = new SmtpClient();
        try
        {
            smtp.Connect("smtp-mail.outlook.com", 587, SecureSocketOptions.StartTls); // Erstat med din SMTP-host og port
            smtp.Authenticate("usama-ahmad@outlook.com", "Talhahuzaifa1"); // Erstat med dine legitimationer
            smtp.Send(email);
            Console.WriteLine($"Email sent successfully to {toAddress}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to send email to {toAddress}. Error: {ex.Message}");
        }
        finally
        {
            smtp.Disconnect(true);
        }
    }
}
