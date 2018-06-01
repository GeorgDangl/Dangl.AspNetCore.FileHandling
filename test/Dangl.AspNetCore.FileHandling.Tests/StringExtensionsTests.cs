using Xunit;

namespace Dangl.AspNetCore.FileHandling.Tests
{
    public class StringExtensionsTests
    {
        [Fact]
        public void DoesNotTrimServSpecDescription()
        {
            var input = "0123456789012345678901234567890123456789";
            var actual = input.WithMaxLength(40);
            Assert.Equal(input, actual);
        }

        [Fact]
        public void TrimsTooLongServSpecDescription()
        {
            var input = "0123456789012345678901234567890123456789";
            var actual = input.WithMaxLength(40);
            var expected = "0123456789012345678901234567890123456789";
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void ReturnsNullForNullInput()
        {
            var actual = ((string)null).WithMaxLength(10);
            Assert.Null(actual);
        }
    }
}
