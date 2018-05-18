﻿using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tweetinvi;
using TwitterStream.Config;

namespace TwitterStream
{
    class Program
    {
        static void Main(string[] args)
        {
            var credentials = ConfigManager.LoadConfig<TwitterCredentials>();

            Auth.SetUserCredentials(credentials.ConsumerKey, credentials.ConsumerSecret, credentials.UserAccessToken, credentials.UserAccessSecret);

            //var user = User.GetAuthenticatedUser();         // user information
            //var userSettings = user.GetAccountSettings();   // user settings information

            var subscription = ConfigManager.LoadConfig<TwitterSubscription>();

            PublisherFactory.LoadRegistered();

            var groupTasks = new List<Task>();
            foreach (var group in subscription.Groups)
            {
                if (!group.Enabled)
                    continue;

                var groupTask = Task.Run(async () =>
                {
                    var stream = Tweetinvi.Stream.CreateFilteredStream();

                    // Subscribe to group topics.
                    foreach (var topic in group.Topics)
                    {
                        stream.AddTrack(topic);
                    }

                    // Subscribe to group users.
                    foreach (var userName in group.Users)
                    {
                        var user = User.GetUserFromScreenName(userName);
                        stream.AddFollow(user);
                    }

                    foreach (var publisherName in group.Publishers)
                    {
                        if (PublisherFactory.TryGetPublisher(publisherName, out var pub))
                        {
                            stream.MatchingTweetReceived += (sender, argx) =>
                            {
                                var tweet = new Tweet()
                                {
                                    Message = argx.Tweet.ToString(),
                                    IsRetweet = argx.Tweet.IsRetweet,
                                    ScreenName = argx.Tweet.CreatedBy.ScreenName,
                                    Url = argx.Tweet.Url
                                };

                                TweetDispatcher.Dispatch(tweet, pub);
                            };
                        }   
                    }

                    await stream.StartStreamMatchingAnyConditionAsync();
                });

                groupTasks.Add(groupTask);
            }

            Task.WaitAll(groupTasks.ToArray());
        }
        
    }
}
