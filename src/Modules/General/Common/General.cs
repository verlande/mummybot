using System;
using System.Collections.Generic;
using Newtonsoft.Json;
namespace mummybot.Modules.General.Common
{
    public class Xkcd
    {
        public int Num { get; set; }
        public string Month { get; set; }
        public string Year { get; set; }
        public string Day { get; set; }
        [JsonProperty("safe_title")]
        public string Title { get; set; }
        [JsonProperty("img")]
        public string ImageLink { get; set; }
        public string Alt { get; set; }
    }

    public class Bible
    {
        public string Bookname { get; set; }
        public string Chapter { get; set; }
        public string Verse { get; set; }
        public string Text { get; set; }
    }

    public class Cats
    {
        [JsonProperty("file")]
        public string File { get; set; }
    }

    public class Urban
    {
        [JsonProperty("list")]
        public List[] List { get; set; }
    }

    public class List
    {
        [JsonProperty("definition")]
        public string Definition { get; set; }

        [JsonProperty("permalink")]
        public Uri Permalink { get; set; }

        [JsonProperty("thumbs_up")]
        public long ThumbsUp { get; set; }

        [JsonProperty("sound_urls")]
        public List<object> SoundUrls { get; set; }

        [JsonProperty("author")]
        public string Author { get; set; }

        [JsonProperty("word")]
        public string Word { get; set; }

        [JsonProperty("defid")]
        public long Defid { get; set; }

        [JsonProperty("current_vote")]
        public string CurrentVote { get; set; }

        [JsonProperty("written_on")]
        public DateTimeOffset WrittenOn { get; set; }

        [JsonProperty("example")]
        public string Example { get; set; }

        [JsonProperty("thumbs_down")]
        public long ThumbsDown { get; set; }
    }
}
