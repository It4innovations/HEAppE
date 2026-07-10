using System.Threading.Tasks;

namespace HEAppE.BusinessLogicTier;

public interface IHEAppEEventHub
{
    Task PublishEventAsync(long userId, string eventType, string source, object data);
}
