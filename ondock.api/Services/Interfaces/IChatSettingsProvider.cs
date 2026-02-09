using ondock.api.Configuration;

namespace ondock.api.Services.Interfaces;

public interface IChatSettingsProvider
{
    ChatSettings Get();
    void Update(ChatSettings settings);
}
