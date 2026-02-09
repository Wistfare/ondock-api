namespace ondock.api.Services.Interfaces;

public interface IEmailService
{
    /// <summary>
    /// Send password reset email with token
    /// </summary>
    Task SendPasswordResetEmailAsync(string toEmail, string resetToken, string userName);
    
    /// <summary>
    /// Send email verification email
    /// </summary>
    Task SendEmailVerificationAsync(string toEmail, string verificationToken, string userName);
    
    /// <summary>
    /// Send email confirmation email after registration (link-based)
    /// </summary>
    Task SendEmailConfirmationAsync(string toEmail, string confirmationToken, string userName);
    
    /// <summary>
    /// Send email confirmation code after registration (code-based)
    /// </summary>
    Task SendEmailConfirmationCodeAsync(string toEmail, string confirmationCode, string userName, int expirationMinutes);
    
    /// <summary>
    /// Send generic email
    /// </summary>
    Task SendEmailAsync(string toEmail, string subject, string body, bool isHtml = true);
}
