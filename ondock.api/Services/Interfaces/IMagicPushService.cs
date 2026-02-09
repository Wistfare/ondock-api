using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ondock.api.Data.Entities;

namespace ondock.api.Services.Interfaces;

public interface IMagicPushService
{
    Task SendMonitoringCompletedAsync(Guid userId, Guid sessionId);
    Task<string?> RegisterDeviceAsync(Guid userId, string deviceId, Platform platform, string token);
    Task SendChatMessagePushAsync(Guid chatId, IReadOnlyList<Guid> recipientUserIds);
}
