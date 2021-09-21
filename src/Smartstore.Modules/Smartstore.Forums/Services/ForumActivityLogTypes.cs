namespace Smartstore.Forums.Services
{
    internal static class ForumActivityLogTypes
    {
        public const string PublicStoreSendPM = "PublicStore.SendPM";
        public const string PublicStoreAddForumTopic = "PublicStore.AddForumTopic";
        public const string PublicStoreEditForumTopic = "PublicStore.EditForumTopic";
        public const string PublicStoreDeleteForumTopic = "PublicStore.DeleteForumTopic";
        public const string PublicStoreAddForumPost = "PublicStore.AddForumPost";
        public const string PublicStoreEditForumPost = "PublicStore.EditForumPost";
        public const string PublicStoreDeleteForumPost = "PublicStore.DeleteForumPost";

        public static string[] All => new[] { PublicStoreSendPM, PublicStoreAddForumTopic, PublicStoreEditForumTopic, PublicStoreDeleteForumTopic,
            PublicStoreAddForumPost, PublicStoreEditForumPost, PublicStoreDeleteForumPost };
    }
}
