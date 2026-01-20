// Domain/Common/IHasTimestamps.cs (if not exists)
namespace POS.Domain.Common
{
    public interface IHasTimestamps
    {
        DateTimeOffset CreatedDate { get; set; }
        DateTimeOffset UpdatedDate { get; set; }
    }
}