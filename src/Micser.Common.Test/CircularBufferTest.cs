using System;
using Xunit;

namespace Micser.Common.Test
{
    public class CircularBufferTest
    {
        [Fact]
        public void AddElements_WrapsAround()
        {
            var buffer = new CircularBuffer<int>(3);

            buffer.Add(1);
            Assert.Equal(1, buffer[0]);
            Assert.False(buffer.IsFull);

            buffer.Add(2);
            Assert.Equal(2, buffer[1]);
            Assert.False(buffer.IsFull);

            buffer.Add(3);
            Assert.Equal(3, buffer[2]);
            Assert.True(buffer.IsFull);

            buffer.Add(4);
            Assert.Equal(4, buffer[0]);
            Assert.True(buffer.IsFull);
        }

        [Fact]
        public void CreateWithInvalidCount_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new CircularBuffer<int>(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new CircularBuffer<int>(-5));
        }

        [Fact]
        public void CreateWithValidCount_IsOk()
        {
            // ReSharper disable once ObjectCreationAsStatement
            new CircularBuffer<int>(1);
            // ReSharper disable once ObjectCreationAsStatement
            new CircularBuffer<int>(42);
        }

        [Fact]
        public void InvalidIndexAccess_Throws()
        {
            var buffer = new CircularBuffer<int>(5);
            Assert.Throws<ArgumentOutOfRangeException>(() => buffer[5]);
            Assert.Throws<ArgumentOutOfRangeException>(() => buffer[-2]);
        }

        [Fact]
        public void SetValue_LeavesPosition()
        {
            var buffer = new CircularBuffer<int>(5)
            {
                [4] = 42
            };

            Assert.Equal(0, buffer.Position);
            Assert.False(buffer.IsFull);
        }
    }
}