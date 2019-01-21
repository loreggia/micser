using System;
using System.Collections.Generic;
using System.IO;
using Xunit.Abstractions;

namespace Micser.Infrastructure.Test
{
    public class TestFileManager
    {
        private readonly List<string> _fileNames;
        private readonly ITestOutputHelper _testOutputHelper;

        public TestFileManager(ITestOutputHelper testOutputHelper)
        {
            _fileNames = new List<string>();

            _testOutputHelper = testOutputHelper;
        }

        public string GetFileName()
        {
            var fn = Guid.NewGuid() + ".tmp";
            _fileNames.Add(fn);
            return fn;
        }

        public void DeleteFiles()
        {
            foreach (var fileName in _fileNames)
            {
                try
                {
                    File.Delete(fileName);
                }
                catch (Exception ex)
                {
                    _testOutputHelper.WriteLine(ex.ToString());
                }
            }

            _fileNames.Clear();
        }
    }
}
