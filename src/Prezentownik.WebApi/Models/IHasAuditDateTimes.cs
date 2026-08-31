namespace Prezentownik.WebApi.Models;

public interface IHasAuditDateTimes
{
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
