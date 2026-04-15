using System.Threading.Channels;

namespace SearchTool_ServerSide.Logging
{
    public interface IUserLogQueue
    {
        ValueTask QueueAsync(UserLogQueueItem item, CancellationToken cancellationToken = default);
        ValueTask<UserLogQueueItem> DequeueAsync(CancellationToken cancellationToken = default);
    }

    public class UserLogQueue : IUserLogQueue
    {
        private readonly Channel<UserLogQueueItem> _queue;

        public UserLogQueue()
        {
            var options = new BoundedChannelOptions(10000)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = true,
                SingleWriter = false
            };

            _queue = Channel.CreateBounded<UserLogQueueItem>(options);
        }

        public async ValueTask QueueAsync(UserLogQueueItem item, CancellationToken cancellationToken = default)
        {
            await _queue.Writer.WriteAsync(item, cancellationToken);
        }

        public async ValueTask<UserLogQueueItem> DequeueAsync(CancellationToken cancellationToken = default)
        {
            return await _queue.Reader.ReadAsync(cancellationToken);
        }
    }
}