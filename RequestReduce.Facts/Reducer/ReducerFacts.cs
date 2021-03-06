﻿using System;
using System.Text;
using Moq;
using RequestReduce.Configuration;
using RequestReduce.Reducer;
using RequestReduce.Store;
using RequestReduce.Utilities;
using Xunit;
using UriBuilder = RequestReduce.Utilities.UriBuilder;

namespace RequestReduce.Facts.Reducer
{
    public class ReducerFacts
    {
        private class TestableReducer : Testable<RequestReduce.Reducer.Reducer>
        {
            public TestableReducer()
            {
                Mock<IMinifier>().Setup(x => x.Minify(It.IsAny<string>())).Returns("minified");
                Inject<IUriBuilder>(new UriBuilder(Mock<IRRConfiguration>().Object));
            }

        }

        public class Process
        {
            [Fact]
            public void WillReturnProcessedCssUrlInCorrectConfigDirectory()
            {
                var testable = new TestableReducer();
                testable.Mock<IRRConfiguration>().Setup(x => x.SpriteVirtualPath).Returns("spritedir");

                var result = testable.ClassUnderTest.Process("http://host/css1.css::http://host/css2.css");

                Assert.True(result.StartsWith("spritedir/"));
            }

            [Fact]
            public void WillReturnProcessedCssUrlWithKeyInPath()
            {
                var testable = new TestableReducer();
                testable.Mock<IRRConfiguration>().Setup(x => x.SpriteVirtualPath).Returns("spritedir");
                var guid = Guid.NewGuid();
                var builder = new UriBuilder(testable.Mock<IRRConfiguration>().Object);

                var result = testable.ClassUnderTest.Process(guid, "http://host/css1.css::http://host/css2.css");

                Assert.Equal(guid, builder.ParseKey(result));
            }

            [Fact]
            public void WillSetSpriteManagerCssKey()
            {
                var testable = new TestableReducer();
                var guid = Guid.NewGuid();

                testable.ClassUnderTest.Process(guid, "http://host/css1.css::http://host/css2.css");

                testable.Mock<ISpriteManager>().VerifySet(x => x.SpritedCssKey = guid);
            }

            [Fact]
            public void WillUseHashOfUrlsIfNoKeyIsGiven()
            {
                var testable = new TestableReducer();
                testable.Mock<IRRConfiguration>().Setup(x => x.SpriteVirtualPath).Returns("spritedir");
                var guid = Hasher.Hash("http://host/css1.css::http://host/css2.css");
                var builder = new UriBuilder(testable.Mock<IRRConfiguration>().Object);

                var result = testable.ClassUnderTest.Process("http://host/css1.css::http://host/css2.css");

                Assert.Equal(guid, builder.ParseKey(result));
            }

            [Fact]
            public void WillReturnProcessedCssUrlWithARequestReducedFileName()
            {
                var testable = new TestableReducer();

                var result = testable.ClassUnderTest.Process("http://host/css1.css::http://host/css2.css");

                Assert.True(result.EndsWith("-" + UriBuilder.CssFileName));
            }

            [Fact]
            public void WillDownloadContentOfEachOriginalCSS()
            {
                var testable = new TestableReducer();

                var result = testable.ClassUnderTest.Process("http://host/css1.css::http://host/css2.css");

                testable.Mock<IWebClientWrapper>().Verify(x => x.DownloadString("http://host/css1.css"), Times.Once());
                testable.Mock<IWebClientWrapper>().Verify(x => x.DownloadString("http://host/css2.css"), Times.Once());
            }

            [Fact]
            public void WillSaveMinifiedAggregatedCSS()
            {
                var testable = new TestableReducer();
                testable.Mock<IWebClientWrapper>().Setup(x => x.DownloadString("http://host/css1.css")).Returns("css1");
                testable.Mock<IWebClientWrapper>().Setup(x => x.DownloadString("http://host/css2.css")).Returns("css2");
                testable.Mock<IMinifier>().Setup(x => x.Minify("css1css2")).Returns("min");

                var result = testable.ClassUnderTest.Process("http://host/css1.css::http://host/css2.css");

                testable.Mock<IStore>().Verify(
                    x =>
                    x.Save(Encoding.UTF8.GetBytes("min").MatchEnumerable(), result,
                           "http://host/css1.css::http://host/css2.css"), Times.Once());
            }

            [Fact]
            public void WillAddSpriteToSpriteManager()
            {
                var testable = new TestableReducer();
                testable.Mock<IWebClientWrapper>().Setup(x => x.DownloadString(It.IsAny<string>())).Returns("css");
                var image1 = new BackgroundImageClass("", "http://server/content/style.css") {ImageUrl = "image1"};
                var image2 = new BackgroundImageClass("", "http://server/content/style.css") { ImageUrl = "image2" };
                var css = "css";
                testable.Mock<ICssImageTransformer>().Setup(x => x.ExtractImageUrls(ref css, It.IsAny<string>())).Returns(new BackgroundImageClass[] { image1, image2 });

                testable.ClassUnderTest.Process("http://host/css2.css");

                testable.Mock<ISpriteManager>().Verify(x => x.Add(image1), Times.Once());
            }

            [Fact]
            public void WillInjectSpritesToCssAfterFlush()
            {
                var testable = new TestableReducer();
                var image1 = new BackgroundImageClass("", "http://server/content/style.css") {ImageUrl = "image1"};
                var image2 = new BackgroundImageClass("", "http://server/content/style.css") { ImageUrl = "image2" };
                var css = "css";
                testable.Mock<IWebClientWrapper>().Setup(x => x.DownloadString(It.IsAny<string>())).Returns(css);
                testable.Mock<ICssImageTransformer>().Setup(x => x.ExtractImageUrls(ref css, It.IsAny<string>())).Returns(new[] { image1, image2 });
                var sprite1 = new Sprite(-100, 1);
                var sprite2 = new Sprite(-100, 2);
                testable.Mock<ISpriteManager>().Setup(x => x[image1]).Returns(sprite1); 
                testable.Mock<ISpriteManager>().Setup(x => x[image2]).Returns(sprite2);
                bool flushIsCalled = false;
                bool flushCalled = false;
                testable.Mock<ISpriteManager>().Setup(x => x.Flush()).Callback(() => flushIsCalled = true);
                testable.Mock<ICssImageTransformer>().Setup(x => x.InjectSprite(It.IsAny<string>(), It.IsAny<BackgroundImageClass>(), It.IsAny<Sprite>())).Callback(() => flushCalled = flushIsCalled);

                testable.ClassUnderTest.Process("http://host/css2.css");

                testable.Mock<ICssImageTransformer>().Verify(x => x.InjectSprite(It.IsAny<string>(), image1, sprite1), Times.Once());
                testable.Mock<ICssImageTransformer>().Verify(x => x.InjectSprite(It.IsAny<string>(), image2, sprite2), Times.Once());
                Assert.True(flushCalled);
            }

        }
    }
}
