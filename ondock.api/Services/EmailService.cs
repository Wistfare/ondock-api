using Microsoft.Extensions.Options;
using ondock.api.Configuration;
using ondock.api.Services.Interfaces;
using Resend;

namespace ondock.api.Services;

public class EmailService : IEmailService
{
    private readonly EmailSettings _emailSettings;
    private readonly ILogger<EmailService> _logger;
    private readonly IResend _resendClient;

    public EmailService(
        IOptions<EmailSettings> emailSettings,
        ILogger<EmailService> logger,
        IResend resendClient)
    {
        _emailSettings = emailSettings.Value;
        _logger = logger;
        _resendClient = resendClient;
    }

    public async Task SendPasswordResetEmailAsync(string toEmail, string resetToken, string userName)
    {
        var resetUrl = $"{_emailSettings.ResetPasswordUrl}?token={Uri.EscapeDataString(resetToken)}&email={Uri.EscapeDataString(toEmail)}";

        var subject = "Reset Your OnDock Password";
        var body = GeneratePasswordResetHtml(userName, resetUrl, resetToken);

        await SendEmailAsync(toEmail, subject, body, isHtml: true);
    }

    public async Task SendEmailVerificationAsync(string toEmail, string verificationToken, string userName)
    {
        var verifyUrl = $"{_emailSettings.ResetPasswordUrl.Replace("/reset-password", "/verify-email")}?token={Uri.EscapeDataString(verificationToken)}&email={Uri.EscapeDataString(toEmail)}";

        var subject = "Verify Your OnDock Email";
        var body = GenerateEmailVerificationHtml(userName, verifyUrl);

        await SendEmailAsync(toEmail, subject, body, isHtml: true);
    }

    public async Task SendEmailConfirmationAsync(string toEmail, string confirmationToken, string userName)
    {
        var confirmUrl = $"{_emailSettings.ResetPasswordUrl.Replace("/reset-password", "/confirm-email")}?token={Uri.EscapeDataString(confirmationToken)}&email={Uri.EscapeDataString(toEmail)}";

        var subject = "Confirm Your OnDock Email Address";
        var body = GenerateEmailConfirmationHtml(userName, confirmUrl);

        await SendEmailAsync(toEmail, subject, body, isHtml: true);
    }

    public async Task SendEmailConfirmationCodeAsync(string toEmail, string confirmationCode, string userName, int expirationMinutes)
    {
        var subject = "Your OnDock Confirmation Code";
        var body = GenerateEmailConfirmationCodeHtml(userName, confirmationCode, expirationMinutes);

        await SendEmailAsync(toEmail, subject, body, isHtml: true);
    }

    public async Task SendEmailAsync(string toEmail, string subject, string body, bool isHtml = true)
    {
        try
        {
            var message = new EmailMessage
            {
                From = _emailSettings.FromEmail,
                To = toEmail,
                Subject = subject,
                HtmlBody = isHtml ? body : null,
                TextBody = isHtml ? null : body
            };

            var response = await _resendClient.EmailSendAsync(message);

            _logger.LogInformation("Email sent successfully to {Email}. Response: {Response}", toEmail, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Email}", toEmail);
            throw;
        }
    }

    private string GeneratePasswordResetHtml(string userName, string resetUrl, string resetToken)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <style>
        body {{
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Oxygen, Ubuntu, Cantarell, sans-serif;
            line-height: 1.6;
            color: #333;
            max-width: 600px;
            margin: 0 auto;
            padding: 20px;
        }}
        .container {{
            background: #ffffff;
            border: 2px solid #000000;
            padding: 30px;
            margin: 20px 0;
        }}
        .header {{
            text-align: center;
            border-bottom: 2px solid #000000;
            padding-bottom: 20px;
            margin-bottom: 30px;
        }}
        .header h1 {{
            margin: 0;
            font-size: 32px;
            color: #000000;
        }}
        .content {{
            margin-bottom: 30px;
        }}
        .button {{
            display: inline-block;
            padding: 15px 30px;
            background: #000000;
            color: #ffffff;
            text-decoration: none;
            border-radius: 0;
            font-weight: 600;
            text-align: center;
            margin: 20px 0;
        }}
        .token {{
            background: #f5f5f5;
            border: 1px solid #ddd;
            padding: 15px;
            font-family: monospace;
            word-break: break-all;
            margin: 20px 0;
        }}
        .footer {{
            border-top: 2px solid #000000;
            padding-top: 20px;
            margin-top: 30px;
            font-size: 14px;
            color: #666;
            text-align: center;
        }}
        .warning {{
            background: #fff3cd;
            border: 1px solid #ffc107;
            padding: 15px;
            margin: 20px 0;
            border-radius: 4px;
        }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>🔐 OnDock</h1>
        </div>
        
        <div class=""content"">
            <h2>Password Reset Request</h2>
            <p>Hello {userName},</p>
            <p>We received a request to reset your password for your OnDock account.</p>
            
            <p>Click the button below to reset your password:</p>
            
            <div style=""text-align: center;"">
                <a href=""{resetUrl}"" class=""button"">Reset Password</a>
            </div>
            
            <div class=""warning"">
                <strong>⚠️ Security Notice:</strong>
                <ul style=""margin: 10px 0; padding-left: 20px;"">
                    <li>This link will expire in 1 hour</li>
                    <li>If you didn't request this, please ignore this email</li>
                    <li>Never share this link with anyone</li>
                    <li>For security reasons, we cannot reset your password by replying to this email</li>
                </ul>
            </div>
            
            <p style=""margin-top: 20px; font-size: 14px; color: #666;"">
                If the button above doesn't work, copy and paste this link into your browser:
            </p>
            <div class=""token"" style=""word-break: break-all; font-size: 11px;"">{resetUrl}</div>
        </div>
        
        <div class=""footer"">
            <p><strong>OnDock Platform</strong></p>
            <p>This is an automated message, please do not reply.</p>
            <p style=""font-size: 12px; color: #999; margin-top: 10px;"">
                If you're having trouble clicking the button, copy and paste the URL into your web browser.
            </p>
        </div>
    </div>
</body>
</html>";
    }

    private string GenerateEmailVerificationHtml(string userName, string verifyUrl)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <style>
        body {{
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Oxygen, Ubuntu, Cantarell, sans-serif;
            line-height: 1.6;
            color: #333;
            max-width: 600px;
            margin: 0 auto;
            padding: 20px;
        }}
        .container {{
            background: #ffffff;
            border: 2px solid #000000;
            padding: 30px;
            margin: 20px 0;
        }}
        .header {{
            text-align: center;
            border-bottom: 2px solid #000000;
            padding-bottom: 20px;
            margin-bottom: 30px;
        }}
        .header h1 {{
            margin: 0;
            font-size: 32px;
            color: #000000;
        }}
        .content {{
            margin-bottom: 30px;
        }}
        .button {{
            display: inline-block;
            padding: 15px 30px;
            background: #000000;
            color: #ffffff;
            text-decoration: none;
            border-radius: 0;
            font-weight: 600;
            text-align: center;
            margin: 20px 0;
        }}
        .footer {{
            border-top: 2px solid #000000;
            padding-top: 20px;
            margin-top: 30px;
            font-size: 14px;
            color: #666;
            text-align: center;
        }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>✉️ OnDock</h1>
        </div>
        
        <div class=""content"">
            <h2>Verify Your Email</h2>
            <p>Hello {userName},</p>
            <p>Welcome to OnDock! Please verify your email address to complete your registration.</p>
            
            <div style=""text-align: center;"">
                <a href=""{verifyUrl}"" class=""button"">Verify Email</a>
            </div>
            
            <p>Or copy and paste this link into your browser:</p>
            <div style=""background: #f5f5f5; border: 1px solid #ddd; padding: 15px; font-family: monospace; word-break: break-all; margin: 20px 0;"">
                {verifyUrl}
            </div>
        </div>
        
        <div class=""footer"">
            <p><strong>OnDock Platform</strong></p>
            <p>This is an automated message, please do not reply.</p>
        </div>
    </div>
</body>
</html>";
    }

    private string GenerateEmailConfirmationHtml(string userName, string confirmUrl)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <style>
        body {{
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Oxygen, Ubuntu, Cantarell, sans-serif;
            line-height: 1.6;
            color: #333;
            max-width: 600px;
            margin: 0 auto;
            padding: 20px;
        }}
        .container {{
            background: #ffffff;
            border: 2px solid #000000;
            padding: 30px;
            margin: 20px 0;
        }}
        .header {{
            text-align: center;
            border-bottom: 2px solid #000000;
            padding-bottom: 20px;
            margin-bottom: 30px;
        }}
        .header h1 {{
            margin: 0;
            font-size: 32px;
            color: #000000;
        }}
        .content {{
            margin-bottom: 30px;
        }}
        .button {{
            display: inline-block;
            padding: 15px 30px;
            background: #000000;
            color: #ffffff;
            text-decoration: none;
            border-radius: 0;
            font-weight: 600;
            text-align: center;
            margin: 20px 0;
        }}
        .footer {{
            border-top: 2px solid #000000;
            padding-top: 20px;
            margin-top: 30px;
            font-size: 14px;
            color: #666;
            text-align: center;
        }}
        .info-box {{
            background: #e3f2fd;
            border: 1px solid #2196f3;
            padding: 15px;
            margin: 20px 0;
            border-radius: 4px;
        }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>✉️ OnDock</h1>
        </div>
        
        <div class=""content"">
            <h2>Welcome to OnDock!</h2>
            <p>Hello {userName},</p>
            <p>Thank you for registering with OnDock. To complete your registration and start using all features, please confirm your email address.</p>
            
            <div style=""text-align: center;"">
                <a href=""{confirmUrl}"" class=""button"">Confirm Email Address</a>
            </div>
            
            <div class=""info-box"">
                <strong>📌 Important:</strong>
                <ul style=""margin: 10px 0; padding-left: 20px;"">
                    <li>This confirmation link will expire in 24 hours</li>
                    <li>You need to confirm your email to access all features</li>
                    <li>If you didn't create an account, please ignore this email</li>
                </ul>
            </div>
            
            <p style=""margin-top: 20px; font-size: 14px; color: #666;"">
                If the button above doesn't work, copy and paste this link into your browser:
            </p>
            <div style=""background: #f5f5f5; border: 1px solid #ddd; padding: 15px; font-family: monospace; word-break: break-all; margin: 20px 0; font-size: 11px;"">
                {confirmUrl}
            </div>
        </div>
        
        <div class=""footer"">
            <p><strong>OnDock Platform</strong></p>
            <p>This is an automated message, please do not reply.</p>
            <p style=""font-size: 12px; color: #999; margin-top: 10px;"">
                If you're having trouble clicking the button, copy and paste the URL into your web browser.
            </p>
        </div>
    </div>
</body>
</html>";
    }

    private string GenerateEmailConfirmationCodeHtml(string userName, string confirmationCode, int expirationMinutes)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <style>
        body {{
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Oxygen, Ubuntu, Cantarell, sans-serif;
            line-height: 1.6;
            color: #333;
            max-width: 600px;
            margin: 0 auto;
            padding: 20px;
        }}
        .container {{
            background: #ffffff;
            border: 2px solid #000000;
            padding: 30px;
            margin: 20px 0;
        }}
        .header {{
            text-align: center;
            border-bottom: 2px solid #000000;
            padding-bottom: 20px;
            margin-bottom: 30px;
        }}
        .header h1 {{
            margin: 0;
            font-size: 32px;
            color: #000000;
        }}
        .content {{
            margin-bottom: 30px;
        }}
        .code-box {{
            background: #f5f5f5;
            border: 2px solid #000000;
            padding: 20px;
            text-align: center;
            margin: 30px 0;
        }}
        .code {{
            font-size: 36px;
            font-weight: bold;
            letter-spacing: 8px;
            font-family: monospace;
            color: #000000;
        }}
        .footer {{
            border-top: 2px solid #000000;
            padding-top: 20px;
            margin-top: 30px;
            font-size: 14px;
            color: #666;
            text-align: center;
        }}
        .info-box {{
            background: #e3f2fd;
            border: 1px solid #2196f3;
            padding: 15px;
            margin: 20px 0;
            border-radius: 4px;
        }}
        .warning {{
            background: #fff3cd;
            border: 1px solid #ffc107;
            padding: 15px;
            margin: 20px 0;
            border-radius: 4px;
        }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>🔐 OnDock</h1>
        </div>
        
        <div class=""content"">
            <h2>Email Confirmation Code</h2>
            <p>Hello {userName},</p>
            <p>Thank you for registering with OnDock. Use the following code to confirm your email address:</p>
            
            <div class=""code-box"">
                <div class=""code"">{confirmationCode}</div>
            </div>
            
            <div class=""info-box"">
                <strong>📌 Important:</strong>
                <ul style=""margin: 10px 0; padding-left: 20px;"">
                    <li>This code will expire in <strong>{expirationMinutes} minutes</strong></li>
                    <li>Enter this code in the app to verify your email</li>
                    <li>Do not share this code with anyone</li>
                </ul>
            </div>
            
            <div class=""warning"">
                <strong>⚠️ Security Notice:</strong>
                <p style=""margin: 5px 0;"">If you didn't create an OnDock account, please ignore this email.</p>
            </div>
        </div>
        
        <div class=""footer"">
            <p><strong>OnDock Platform</strong></p>
            <p>This is an automated message, please do not reply.</p>
        </div>
    </div>
</body>
</html>";
    }
}
