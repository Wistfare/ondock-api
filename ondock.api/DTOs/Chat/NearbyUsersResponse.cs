namespace ondock.api.DTOs.Chat;

public class NearbyUsersResponse
{
    public int Count { get; set; }
    public List<NearbyUserDto> Users { get; set; } = new();
}
