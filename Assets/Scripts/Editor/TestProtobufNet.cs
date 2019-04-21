using System.Collections;
using System.IO;
using Net;
using NUnit.Framework;
using UnityEngine.TestTools;
using Google.Protobuf;

namespace Tests
{
    public class TestProtobufNet
    {
        // A Test behaves as an ordinary method
        [Test]
        public void TestProtobufNetSimplePasses()
        {
			// Use the Assert class to test conditions
			CommonRsp input = new CommonRsp();
			input.Success = true;
			input.ErrorCode = 38;
			input.ErrorStr = "hello world";

			MemoryStream stream = new MemoryStream();
			input.WriteTo(stream);

			stream.Position = 0;

			var output = CommonRsp.Parser.ParseFrom(stream);

			Assert.AreEqual(input.Success, output.Success);
			Assert.AreEqual(input.ErrorCode, output.ErrorCode);
			Assert.AreEqual(input.ErrorStr, output.ErrorStr);
		}

        // A UnityTest behaves like a coroutine in Play Mode. In Edit Mode you can use
        // `yield return null;` to skip a frame.
        [UnityTest]
        public IEnumerator TestProtobufNetWithEnumeratorPasses()
        {
            // Use the Assert class to test conditions.
            // Use yield to skip a frame.
            yield return null;
        }
    }
}
