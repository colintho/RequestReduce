﻿using System.IO;
using System.Text;
using Moq;
using RequestReduce.Module;
using Xunit;

namespace RequestReduce.Facts.Module
{
    public class ResponseFilterFacts
    {
        private class TestableResponseFilter : Testable<ResponseFilter>
        {
            public TestableResponseFilter()
            {
                Inject(Encoding.UTF8);
                Mock<Stream>().Setup(x => x.Write(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>())).
                    Callback<byte[], int, int>((buf, off, len) =>
                    {
                        FilteredResult += Encoding.UTF8.GetString(buf, off, len);
                    });

            }

            public string FilteredResult { get; set; }
        }

        public class Write
        {
            [Fact]
            public void WillTransformHeadInSingleWrite()
            {
                var testable = new TestableResponseFilter();
                var testBuffer = "before<head>head</head>after";
                var testTransform = "<head>head</head>";
                testable.Mock<IResponseTransformer>().Setup(x => x.Transform(testTransform)).Returns("thead</head>");

                testable.ClassUnderTest.Write(Encoding.UTF8.GetBytes(testBuffer), 0, testBuffer.Length);

                Assert.Equal("before<head>thead</head>after", testable.FilteredResult);
            }

            [Fact]
            public void WillTransformHeadAtBeginningOfResponse()
            {
                var testable = new TestableResponseFilter();
                var testBuffer = "<head>head</head>after";
                var testTransform = "<head>head</head>";
                testable.Mock<IResponseTransformer>().Setup(x => x.Transform(testTransform)).Returns("thead</head>");

                testable.ClassUnderTest.Write(Encoding.UTF8.GetBytes(testBuffer), 0, testBuffer.Length);

                Assert.Equal("<head>thead</head>after", testable.FilteredResult);
            }

            [Fact]
            public void WillTransformHeadAtEndOfResponse()
            {
                var testable = new TestableResponseFilter();
                var testBuffer = "before<head>head</head>";
                var testTransform = "<head>head</head>";
                testable.Mock<IResponseTransformer>().Setup(x => x.Transform(testTransform)).Returns("thead</head>");

                testable.ClassUnderTest.Write(Encoding.UTF8.GetBytes(testBuffer), 0, testBuffer.Length);

                Assert.Equal("before<head>thead</head>", testable.FilteredResult);
            }

            [Fact]
            public void WillTransformHeadWhenAllResponse()
            {
                var testable = new TestableResponseFilter();
                var testBuffer = "<head>head</head>";
                var testTransform = "<head>head</head>";
                testable.Mock<IResponseTransformer>().Setup(x => x.Transform(testTransform)).Returns("thead</head>");

                testable.ClassUnderTest.Write(Encoding.UTF8.GetBytes(testBuffer), 0, testBuffer.Length);

                Assert.Equal("<head>thead</head>", testable.FilteredResult);
            }

            [Fact]
            public void WillTransformHeadInmultipleWritesBrokenAtStartToken()
            {
                var testable = new TestableResponseFilter();
                var testBuffer = "before<head>head</head>after";
                var testTransform = "<head>head</head>";
                testable.Mock<IResponseTransformer>().Setup(x => x.Transform(testTransform)).Returns("thead</head>");

                testable.ClassUnderTest.Write(Encoding.UTF8.GetBytes(testBuffer), 0, 9);
                testable.ClassUnderTest.Write(Encoding.UTF8.GetBytes(testBuffer), 9, testBuffer.Length-9);

                Assert.Equal("before<head>thead</head>after", testable.FilteredResult);
            }

            [Fact]
            public void WillTransformHeadInmultipleWritesBrokenBeforeStartToken()
            {
                var testable = new TestableResponseFilter();
                var testBuffer = "before<head>head</head>after";
                var testTransform = "<head>head</head>";
                testable.Mock<IResponseTransformer>().Setup(x => x.Transform(testTransform)).Returns("thead</head>");

                testable.ClassUnderTest.Write(Encoding.UTF8.GetBytes(testBuffer), 0, 3);
                testable.ClassUnderTest.Write(Encoding.UTF8.GetBytes(testBuffer), 3, testBuffer.Length - 3);

                Assert.Equal("before<head>thead</head>after", testable.FilteredResult);
            }

            [Fact]
            public void WillTransformHeadInmultipleWritesBrokenBetweenToken()
            {
                var testable = new TestableResponseFilter();
                var testBuffer = "before<head>head</head>after";
                var testTransform = "<head>head</head>";
                testable.Mock<IResponseTransformer>().Setup(x => x.Transform(testTransform)).Returns("thead</head>");

                testable.ClassUnderTest.Write(Encoding.UTF8.GetBytes(testBuffer), 0, 15);
                testable.ClassUnderTest.Write(Encoding.UTF8.GetBytes(testBuffer), 15, testBuffer.Length - 15);

                Assert.Equal("before<head>thead</head>after", testable.FilteredResult);
            }

            [Fact]
            public void WillTransformHeadInmultipleWritesBrokenAtEndToken()
            {
                var testable = new TestableResponseFilter();
                var testBuffer = "before<head>head</head>after";
                var testTransform = "<head>head</head>";
                testable.Mock<IResponseTransformer>().Setup(x => x.Transform(testTransform)).Returns("thead</head>");

                testable.ClassUnderTest.Write(Encoding.UTF8.GetBytes(testBuffer), 0, 26);
                testable.ClassUnderTest.Write(Encoding.UTF8.GetBytes(testBuffer), 26, testBuffer.Length - 26);

                Assert.Equal("before<head>thead</head>after", testable.FilteredResult);
            }

            [Fact]
            public void WillTransformHeadInsingleWriteWithPartialMatchBeforeStart()
            {
                var testable = new TestableResponseFilter();
                var testBuffer = "be<h1>fo</h1>re<head>head</head>after";
                var testTransform = "<head>head</head>";
                testable.Mock<IResponseTransformer>().Setup(x => x.Transform(testTransform)).Returns("thead</head>");

                testable.ClassUnderTest.Write(Encoding.UTF8.GetBytes(testBuffer), 0, testBuffer.Length);

                Assert.Equal("be<h1>fo</h1>re<head>thead</head>after", testable.FilteredResult);
            }

            [Fact]
            public void WillTransformHeadInsingleWriteWithPartialMatchBeforeEnd()
            {
                var testable = new TestableResponseFilter();
                var testBuffer = "before<head>h<h1>ea</h1>d</head>after";
                var testTransform = "<head>h<h1>ea</h1>d</head>";
                testable.Mock<IResponseTransformer>().Setup(x => x.Transform(testTransform)).Returns("thead</head>");

                testable.ClassUnderTest.Write(Encoding.UTF8.GetBytes(testBuffer), 0, testBuffer.Length);

                Assert.Equal("before<head>thead</head>after", testable.FilteredResult);
            }

            public void WillTransformHeadInMultipleWritesWithPartialMatchBeforeStart()
            {
                var testable = new TestableResponseFilter();
                var testBuffer = "be<h1>fo</h1>re<head>head</head>after";
                var testTransform = "<head>head</head>";
                testable.Mock<IResponseTransformer>().Setup(x => x.Transform(testTransform)).Returns("thead</head>");

                testable.ClassUnderTest.Write(Encoding.UTF8.GetBytes(testBuffer), 0, 4);
                testable.ClassUnderTest.Write(Encoding.UTF8.GetBytes(testBuffer), 4, 10);
                testable.ClassUnderTest.Write(Encoding.UTF8.GetBytes(testBuffer), 14, testBuffer.Length - 14);

                Assert.Equal("be<h1>fo</h1>re<head>thead</head>after", testable.FilteredResult);
            }

            [Fact]
            public void WillTransformHeadWithAttribute()
            {
                var testable = new TestableResponseFilter();
                var testBuffer = @"before<head id=""id"">head</head>after";
                var testTransform = @"<head id=""id"">head</head>";
                testable.Mock<IResponseTransformer>().Setup(x => x.Transform(testTransform)).Returns(@"id=""id"">thead</head>");

                testable.ClassUnderTest.Write(Encoding.UTF8.GetBytes(testBuffer), 0, testBuffer.Length);

                Assert.Equal(@"before<head id=""id"">thead</head>after", testable.FilteredResult);
            }

        }
    }
}