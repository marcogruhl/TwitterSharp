﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TwitterSharp.ApiEndpoint;
using TwitterSharp.Client;
using TwitterSharp.Request;
using TwitterSharp.Response;
using TwitterSharp.Rule;

namespace TwitterSharp.UnitTests
{
    [TestClass]
    public class TestStream
    {
        [TestMethod]
        public async Task TestStreamProcess()
        {
            List<RateLimit> rateLimitEvents = new();

            var client = new TwitterClient(Environment.GetEnvironmentVariable("TWITTER_TOKEN"));
            
            var answer = await client.GetInfoTweetStreamAsync();
            rateLimitEvents.Add(answer.RateLimit);
            var res = answer.Data;

            var elem = res.FirstOrDefault(x => x.Tag == "TwitterSharp UnitTest");

            var objectiveCount = res.Length + 1;

            if (elem != null)
            {
                await client.DeleteTweetStreamAsync(elem.Id);
                objectiveCount--;
            }

            var exp = Expression.Author("arurandeisu");
            res = await client.AddTweetStreamAsync(new StreamRequest(exp, "TwitterSharp UnitTest"));

            Assert.IsTrue(res.Length == 1);
            Assert.IsTrue(res[0].Tag == "TwitterSharp UnitTest");
            Assert.IsTrue(res[0].Value.ToString() == exp.ToString());

            answer = await client.GetInfoTweetStreamAsync();
            rateLimitEvents.Add(answer.RateLimit);
            res = answer.Data;
            
            Assert.IsTrue(CheckGetInfoTweetStreamAsyncRateLimit(rateLimitEvents));

            elem = res.FirstOrDefault(x => x.Tag == "TwitterSharp UnitTest");
            Assert.IsTrue(res.Length == objectiveCount);
            Assert.IsNotNull(elem.Id);
            Assert.IsTrue(elem.Tag == "TwitterSharp UnitTest");
            Assert.IsTrue(elem.Value.ToString() == exp.ToString());

            objectiveCount--;

            Assert.IsTrue(await client.DeleteTweetStreamAsync(elem.Id) == 1);

            answer = await client.GetInfoTweetStreamAsync();
            rateLimitEvents.Add(answer.RateLimit);
            res = answer.Data;

            Assert.IsTrue(CheckGetInfoTweetStreamAsyncRateLimit(rateLimitEvents));

            Assert.IsTrue(res.Length == objectiveCount);
            elem = res.FirstOrDefault(x => x.Tag == "TwitterSharp UnitTest");
            Assert.IsNull(elem);
        }

        [TestMethod]
        public async Task TestStreamCancellation()
        {
            var client = new TwitterClient(Environment.GetEnvironmentVariable("TWITTER_TOKEN"));
            var requestSucceeded = false;
            var streamFinished = false;
            TaskStatus streamResult = TaskStatus.Created;
            
            _ = Task.Run(async () =>
            {
                await client.NextTweetStreamAsync(_ => { }, _ => requestSucceeded = true);
            }).ContinueWith(t =>
            {
                streamResult = t.Status;
                streamFinished = true;
            });
        
            // Test - IsStreaming
            while (!requestSucceeded)
            {
                await Task.Delay(25);
            }
        
            Assert.IsTrue(TwitterClient.IsTweetStreaming);
        
            // Test - two streams same client -> Exception
            try
            {
                await client.NextTweetStreamAsync(_ => { });
            }
            catch (Exception e)
            {
                Assert.IsInstanceOfType(e, typeof(TwitterException));
            }
        
            Assert.IsTrue(TwitterClient.IsTweetStreaming);
        
            // Test - two streams - different client -> Exception
            var client2 = new TwitterClient(Environment.GetEnvironmentVariable("TWITTER_TOKEN"));
            try
            {
                await client2.NextTweetStreamAsync(_ => { });
            }
            catch (Exception e)
            {
                Assert.IsInstanceOfType(e, typeof(TwitterException));
            }
        
            // Test - Cancel stream
            client.CancelTweetStream();
        
            Assert.IsFalse(TwitterClient.IsTweetStreaming);
        
            while (!streamFinished)
            {
                await Task.Delay(25);
            }
        
            Assert.IsTrue(streamResult == TaskStatus.RanToCompletion);
        }

        [TestMethod]
        public async Task TestStreamErrorRule()
        {
            var client = new TwitterClient(Environment.GetEnvironmentVariable("TWITTER_TOKEN"));

            // Faulty expression
            var expression = Expression.Keyword("Faulty expression").And(Expression.PlaceCountry("xxxx"));

            try
            {
                await client.AddTweetStreamAsync(new StreamRequest(expression, "Test Error"));
            }
            catch (TwitterException e)
            {
                Assert.IsTrue(e.Errors != null);
                Assert.IsTrue(e.Errors.Length == 1);
                Assert.AreEqual("UnprocessableEntity", e.Errors.First().Title);
                Assert.IsTrue(e.Errors.First().Details.Length == 1);
            }

            // double faulty expression
            var expression2 = Expression.Keyword("double faulty expression").And(Expression.PlaceCountry("xxxx"), Expression.Sample(200));

            try
            {
                await client.AddTweetStreamAsync(new StreamRequest(expression2, "Test Error 2"));
            }
            catch (TwitterException e)
            {
                Assert.IsTrue(e.Errors != null);
                Assert.IsTrue(e.Errors.Length == 1);
                Assert.AreEqual("UnprocessableEntity", e.Errors.First().Title);
                Assert.IsTrue(e.Errors.First().Details.Length == 2);
            }
        }

        private bool CheckGetInfoTweetStreamAsyncRateLimit(List<RateLimit> rateLimitEvents)
        {
            var rateLimits = rateLimitEvents.Where(x => x.Endpoint == Endpoint.ListingFilters).ToList();

            return rateLimits[^1].Remaining == rateLimits[^2].Remaining - 1;
        }
    }
}
