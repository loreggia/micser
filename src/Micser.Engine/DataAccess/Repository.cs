using System;

namespace Micser.Engine.DataAccess
{
    public abstract class Repository<T> : IDisposable
    {
        private readonly Database _database;

        protected Repository(Database database)
        {
            _database = database;
        }

        ~Repository()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
            }
        }
    }
}