namespace Prezentownik.WebApi.Modules.UserLists.DTOs;

public record ReorderItemsRequest(
    List<Guid> ItemIds);
