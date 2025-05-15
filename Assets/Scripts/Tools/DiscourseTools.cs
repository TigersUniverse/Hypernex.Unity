using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Hypernex.CCK;
using SimpleJSON;

namespace Hypernex.Tools
{
    public static class DiscourseTools
    {
        public static async Task<DiscoursePost?> GetLatestAnnouncement()
        {
            try
            {
                using HttpClient client = new HttpClient();
                string latestUrl = Path.Combine(Init.FORUM_URL, "c", "announcements", "5", "l", "latest.json");
                string latestRes = await client.GetStringAsync(latestUrl);
                JSONNode latestNode = JSONNode.Parse(latestRes);
                JSONArray topic = latestNode["topic_list"]["topics"].AsArray;
                if (topic.Count <= 0) return null;
                JSONNode latestPost = null;
                foreach (JSONNode topicValue in topic.Values)
                {
                    if(topicValue["pinned"].AsBool) continue;
                    latestPost = topicValue;
                    break;
                }
                if (latestPost == null) return null;
                int postId = latestPost["id"].AsInt;
                string postTitle = latestPost["title"].Value;
                string postUrl = Path.Combine(Init.FORUM_URL, "t", postId.ToString());
                string postInfoUrl = Path.Combine(Init.FORUM_URL, "t", postId + ".json");
                string postRes = await client.GetStringAsync(postInfoUrl);
                JSONNode postNode = JSONNode.Parse(postRes);
                JSONArray postPosts = postNode["post_stream"]["posts"].AsArray;
                if (postPosts.Count <= 0) return null;
                JSONNode postInfo = postPosts[0];
                string creator = postInfo["username"].Value;
                int realPostId = postInfo["id"].AsInt;
                string realPostUrl = Path.Combine(Init.FORUM_URL, "posts", realPostId + ".json");
                string realRes = await client.GetStringAsync(realPostUrl);
                JSONNode realNode = JSONNode.Parse(realRes);
                string rawText = realNode["raw"].Value;
                return new DiscoursePost(creator, postTitle, rawText, postUrl);
            }
            catch (Exception e)
            {
                Logger.CurrentLogger.Error("Failed to get latest announcements! " + e);
                return null;
            }
        }
    }

    public struct DiscoursePost
    {
        public string PostCreator;
        public string PostTitle;
        public string PostText;
        public string PostURL;

        public DiscoursePost(string pc, string pt, string ptxt, string url)
        {
            PostCreator = pc;
            PostTitle = pt;
            PostText = ptxt;
            PostURL = url;
        }
    }
}