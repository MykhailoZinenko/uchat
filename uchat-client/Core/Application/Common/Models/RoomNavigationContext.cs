namespace uchat_client.Core.Application.Common.Models;

public record RoomNavigationContext(int RoomId, string RoomName, bool IsGlobal, int? CreatedByUserId);
