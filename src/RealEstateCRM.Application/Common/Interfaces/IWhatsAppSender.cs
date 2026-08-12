namespace RealEstateCRM.Application.Common.Interfaces;

public interface IWhatsAppSender
{
    /// <summary>Returns true if the message was accepted for delivery.</summary>
    Task<bool> SendAsync(string toPhone, string body, CancellationToken cancellationToken = default);
}
