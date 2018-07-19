using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Dangl.AspNetCore.FileHandling.Tests
{
    public class TimeStampedFilePathBuilderTests
    {
        [Theory]
        [InlineData(2018, 7, 19, 15, 5, 20, "file.dat", "2018/07/19/15/2018-07-19-15-05-20_file.dat")]
        [InlineData(1, 1, 1, 1, 1, 1, "file.dat", "0001/01/01/01/0001-01-01-01-01-01_file.dat")]
        [InlineData(9999, 12, 31, 23, 59, 59, "file.dat", "9999/12/31/23/9999-12-31-23-59-59_file.dat")]
        [InlineData(9999, 12, 31, 23, 59, 59, "", "9999/12/31/23/9999-12-31-23-59-59_")]
        [InlineData(9999, 12, 31, 23, 59, 59, null, "9999/12/31/23/9999-12-31-23-59-59_")]
        public void BuildCorrectPath(int year, int month, int day, int hour, int minute, int second, string fileName, string expectedFilePath)
        {
            var fileDate = new DateTime(year, month, day, hour, minute, second);
            var actual = TimeStampedFilePathBuilder.GetTimeStampedFilePath(fileDate, fileName);
            Assert.Equal(expectedFilePath, actual);
        }
    }
}
