using System.Text.RegularExpressions;
namespace ChatVox.Filtering;
public sealed class ChatFilter {
 readonly HashSet<string> ignored;
 public bool IgnoreCommands{get;set;}=true; public bool IgnoreUrls{get;set;}=true;
 public ChatFilter(IEnumerable<string>? users=null)=>ignored=new(users??[],StringComparer.OrdinalIgnoreCase);
 public void SetIgnoredUsers(IEnumerable<string>? users){ignored.Clear();foreach(var user in users??[])if(!string.IsNullOrWhiteSpace(user))ignored.Add(user.Trim());}
 public bool Accept(string user,string text) {
  text=string.Join(' ',text.Split((char[]?)null,StringSplitOptions.RemoveEmptyEntries));
  var rejected=string.IsNullOrWhiteSpace(text)||ignored.Contains(user)||(IgnoreCommands&&text.StartsWith('!'))||(IgnoreUrls&&Regex.IsMatch(text,@"(https?://|www\.)\S+",RegexOptions.IgnoreCase))||!text.Any(char.IsLetterOrDigit);
  // EventSub message-ID deduplication handles delivery retries. Never suppress
  // legitimate repeated chat text forever (for example, a viewer typing "1"
  // again in a later test or stream moment).
  return !rejected;
 }
}
