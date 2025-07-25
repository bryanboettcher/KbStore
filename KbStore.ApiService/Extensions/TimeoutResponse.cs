namespace KbStore.ApiService.Extensions;

using MassTransit;


public sealed class TimeoutResponse : Response
{
    private TimeoutResponse() { }
    
    public Guid? MessageId { get; } = null;
    public Guid? RequestId { get; } = null;
    public Guid? CorrelationId { get; } = null;
    public Guid? ConversationId { get; } = null;
    public Guid? InitiatorId { get; } = null;
    public DateTime? ExpirationTime { get; } = null;
    public Uri? SourceAddress { get; } = null;
    public Uri? DestinationAddress { get; } = null;
    public Uri? ResponseAddress { get; } = null;
    public Uri? FaultAddress { get; } = null;
    public DateTime? SentTime { get; } = null;
    public Headers Headers { get; } = null!;
    public HostInfo Host { get; } = null!;
    public object Message { get; } = null!;

    public static Response Instance { get; } = new TimeoutResponse();
}