using ChatVox.Filtering; using ChatVox.Queue;
namespace ChatVox.Twitch;
public sealed class ChatPipeline(ChatFilter filter,FreshQueue queue,EventDeduplicator dedup){
 public bool ReadUsernames {get;set;}=true;
 public int MaxMessageLength {get;set;}=200;
 public bool Accept(ChatEvent e){if(string.IsNullOrWhiteSpace(e.Text)||!dedup.First(e.MessageId,e.Received)||e.Text.Length>MaxMessageLength||!filter.Accept(e.Chatter,e.Text))return false;queue.Add(ReadUsernames?$"{e.Chatter} says, {e.Text}":e.Text,e.Received);return true;}}
