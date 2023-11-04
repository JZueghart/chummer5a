/*  This file is part of Chummer5a.
 *
 *  Chummer5a is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  Chummer5a is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with Chummer5a.  If not, see <http://www.gnu.org/licenses/>.
 *
 *  You can obtain the full source code for Chummer5a at
 *  https://github.com/chummer5a/chummer5a
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Chummer
{
    /// <summary>
    /// Thread-safe-wrapped version of CachedBindingList, but also with constraints on the generic type so that it can only be used on a class with INotifyPropertyChanged.
    /// Use ThreadSafeObservableCollection instead for classes without INotifyPropertyChanged.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ThreadSafeBindingList<T> : IAsyncList<T>, IAsyncReadOnlyList<T>, IBindingList, ICancelAddNew, IRaiseItemChangedEvents, IHasLockObject, IAsyncProducerConsumerCollection<T> where T : INotifyPropertyChanged
    {
        /// <inheritdoc />
        public AsyncFriendlyReaderWriterLock LockObject { get; } = new AsyncFriendlyReaderWriterLock();

        private readonly CachedBindingList<T> _lstData;

        public ThreadSafeBindingList()
        {
            _lstData = new CachedBindingList<T>();
        }

        public ThreadSafeBindingList(IList<T> list)
        {
            _lstData = new CachedBindingList<T>(list);
        }

        /// <inheritdoc cref="List{T}.Count" />
        public int Count
        {
            get
            {
                using (LockObject.EnterReadLock())
                    return _lstData.Count;
            }
        }

        public async ValueTask<int> GetCountAsync(CancellationToken token = default)
        {
            using (await LockObject.EnterReadLockAsync(token).ConfigureAwait(false))
            {
                token.ThrowIfCancellationRequested();
                return _lstData.Count;
            }
        }

        /// <inheritdoc />
        bool IList.IsFixedSize => false;

        /// <inheritdoc />
        bool ICollection<T>.IsReadOnly => false;

        /// <inheritdoc />
        bool IList.IsReadOnly => false;

        /// <inheritdoc />
        bool ICollection.IsSynchronized => true;

        /// <inheritdoc />
        object ICollection.SyncRoot => LockObject;

        public T this[int index]
        {
            get
            {
                using (LockObject.EnterReadLock())
                    return _lstData[index];
            }
            set
            {
                using (LockObject.EnterUpgradeableReadLock())
                {
                    if (_lstData[index].Equals(value))
                        return;
                    using (LockObject.EnterWriteLock())
                        _lstData[index] = value;
                }
            }
        }

        public async ValueTask<T> GetValueAtAsync(int index, CancellationToken token = default)
        {
            using (await LockObject.EnterReadLockAsync(token).ConfigureAwait(false))
            {
                token.ThrowIfCancellationRequested();
                return _lstData[index];
            }
        }

        public async ValueTask SetValueAtAsync(int index, T value, CancellationToken token = default)
        {
            using (await LockObject.EnterUpgradeableReadLockAsync(token).ConfigureAwait(false))
            {
                token.ThrowIfCancellationRequested();
                if (_lstData[index].Equals(value))
                    return;
                IAsyncDisposable objLocker = await LockObject.EnterWriteLockAsync(token).ConfigureAwait(false);
                try
                {
                    token.ThrowIfCancellationRequested();
                    _lstData[index] = value;
                }
                finally
                {
                    await objLocker.DisposeAsync().ConfigureAwait(false);
                }
            }
        }

        object IList.this[int index]
        {
            get
            {
                using (LockObject.EnterReadLock())
                    return _lstData[index];
            }
            set
            {
                using (LockObject.EnterUpgradeableReadLock())
                {
                    if (_lstData[index].Equals(value))
                        return;
                    using (LockObject.EnterWriteLock())
                        _lstData[index] = (T)value;
                }
            }
        }

        public int Add(object value)
        {
            if (!(value is T objValue))
                return -1;
            using (LockObject.EnterUpgradeableReadLock())
            {
                Add(objValue);
                return _lstData.Count - 1;
            }
        }

        /// <inheritdoc />
        public void Add(T item)
        {
            using (LockObject.EnterWriteLock())
                _lstData.Add(item);
        }

        public async ValueTask AddAsync(T item, CancellationToken token = default)
        {
            IAsyncDisposable objLocker = await LockObject.EnterWriteLockAsync(token).ConfigureAwait(false);
            try
            {
                token.ThrowIfCancellationRequested();
                _lstData.Add(item);
            }
            finally
            {
                await objLocker.DisposeAsync().ConfigureAwait(false);
            }
        }

        /// <inheritdoc cref="List{T}.AddRange" />
        public void AddRange(IEnumerable<T> collection)
        {
            using (LockObject.EnterWriteLock())
                _lstData.AddRange(collection);
        }

        public async ValueTask AddRangeAsync(IEnumerable<T> collection, CancellationToken token = default)
        {
            IAsyncDisposable objLocker = await LockObject.EnterWriteLockAsync(token).ConfigureAwait(false);
            try
            {
                token.ThrowIfCancellationRequested();
                _lstData.AddRange(collection);
            }
            finally
            {
                await objLocker.DisposeAsync().ConfigureAwait(false);
            }
        }

        /// <inheritdoc cref="List{T}.BinarySearch(int, int, T, IComparer{T})" />
        public int BinarySearch(int index, int count, T item, IComparer<T> comparer)
        {
            using (LockObject.EnterReadLock())
                return _lstData.BinarySearch(index, count, item, comparer);
        }

        /// <inheritdoc cref="List{T}.BinarySearch(T, IComparer{T})" />
        public int BinarySearch(T item, IComparer<T> comparer)
        {
            using (LockObject.EnterReadLock())
                return _lstData.BinarySearch(item, comparer);
        }

        /// <inheritdoc cref="List{T}.BinarySearch(int, int, T, IComparer{T})" />
        public async ValueTask<int> BinarySearchAsync(int index, int count, T item, IComparer<T> comparer, CancellationToken token = default)
        {
            using (await LockObject.EnterReadLockAsync(token).ConfigureAwait(false))
            {
                token.ThrowIfCancellationRequested();
                return _lstData.BinarySearch(index, count, item, comparer);
            }
        }

        /// <inheritdoc cref="List{T}.BinarySearch(T, IComparer{T})" />
        public async ValueTask<int> BinarySearchAsync(T item, IComparer<T> comparer, CancellationToken token = default)
        {
            using (await LockObject.EnterReadLockAsync(token).ConfigureAwait(false))
            {
                token.ThrowIfCancellationRequested();
                return _lstData.BinarySearch(item, comparer);
            }
        }

        /// <inheritdoc cref="List{T}.Clear" />
        public void Clear()
        {
            using (LockObject.EnterWriteLock())
                _lstData.Clear();
        }

        /// <inheritdoc cref="List{T}.Clear" />
        public async ValueTask ClearAsync(CancellationToken token = default)
        {
            IAsyncDisposable objLocker = await LockObject.EnterWriteLockAsync(token).ConfigureAwait(false);
            try
            {
                token.ThrowIfCancellationRequested();
                _lstData.Clear();
            }
            finally
            {
                await objLocker.DisposeAsync().ConfigureAwait(false);
            }
        }

        public bool Contains(object value)
        {
            if (!(value is T objValue))
                return false;
            return Contains(objValue);
        }

        /// <inheritdoc cref="List{T}.Contains" />
        public bool Contains(T item)
        {
            using (LockObject.EnterReadLock())
                return _lstData.Contains(item);
        }

        /// <inheritdoc cref="List{T}.Contains" />
        public async ValueTask<bool> ContainsAsync(T item, CancellationToken token = default)
        {
            using (await LockObject.EnterReadLockAsync(token).ConfigureAwait(false))
            {
                token.ThrowIfCancellationRequested();
                return _lstData.Contains(item);
            }
        }

        public void CopyTo(Array array, int index)
        {
            using (LockObject.EnterReadLock())
            {
                foreach (T objItem in _lstData)
                {
                    array.SetValue(objItem, index);
                    ++index;
                }
            }
        }

        public async ValueTask CopyToAsync(Array array, int index, CancellationToken token = default)
        {
            using (await LockObject.EnterReadLockAsync(token).ConfigureAwait(false))
            {
                token.ThrowIfCancellationRequested();
                foreach (T objItem in _lstData)
                {
                    array.SetValue(objItem, index);
                    ++index;
                }
            }
        }

        /// <inheritdoc cref="List{T}.CopyTo(T[], int)" />
        public void CopyTo(T[] array, int arrayIndex)
        {
            using (LockObject.EnterReadLock())
                _lstData.CopyTo(array, arrayIndex);
        }

        /// <inheritdoc cref="List{T}.CopyTo(T[], int)" />
        public async ValueTask CopyToAsync(T[] array, int index, CancellationToken token = default)
        {
            using (await LockObject.EnterReadLockAsync(token).ConfigureAwait(false))
            {
                token.ThrowIfCancellationRequested();
                _lstData.CopyTo(array, index);
            }
        }

        /// <inheritdoc />
        public bool TryAdd(T item)
        {
            Add(item);
            return true;
        }

        public async ValueTask<bool> TryAddAsync(T item, CancellationToken token = default)
        {
            await AddAsync(item, token).ConfigureAwait(false);
            return true;
        }

        /// <inheritdoc />
        public async ValueTask<Tuple<bool, T>> TryTakeAsync(CancellationToken token = default)
        {
            using (await LockObject.EnterReadLockAsync(token).ConfigureAwait(false))
            {
                token.ThrowIfCancellationRequested();
                if (_lstData.Count == 0)
                    return new Tuple<bool, T>(false, default);
            }
            IAsyncDisposable objLocker = await LockObject.EnterWriteLockAsync(token).ConfigureAwait(false);
            try
            {
                token.ThrowIfCancellationRequested();
                if (_lstData.Count > 0)
                {
                    // FIFO to be compliant with how the default for BlockingCollection<T> is ConcurrentQueue
                    T objReturn = _lstData[0];
                    _lstData.RemoveAt(0);
                    return new Tuple<bool, T>(true, objReturn);
                }
            }
            finally
            {
                await objLocker.DisposeAsync().ConfigureAwait(false);
            }

            return new Tuple<bool, T>(false, default);
        }

        /// <inheritdoc />
        public bool TryTake(out T item)
        {
            using (LockObject.EnterReadLock())
            {
                if (_lstData.Count == 0)
                {
                    item = default;
                    return false;
                }
            }
            using (LockObject.EnterWriteLock())
            {
                if (_lstData.Count > 0)
                {
                    // FIFO to be compliant with how the default for BlockingCollection<T> is ConcurrentQueue
                    item = _lstData[0];
                    _lstData.RemoveAt(0);
                    return true;
                }
            }

            item = default;
            return false;
        }

        /// <inheritdoc cref="List{T}.Exists" />
        public bool Exists(Predicate<T> match)
        {
            using (LockObject.EnterReadLock())
                return _lstData.Exists(match);
        }

        /// <inheritdoc cref="List{T}.Exists" />
        public async ValueTask<bool> ExistsAsync(Predicate<T> match, CancellationToken token = default)
        {
            using (await LockObject.EnterReadLockAsync(token).ConfigureAwait(false))
            {
                token.ThrowIfCancellationRequested();
                return _lstData.Exists(match);
            }
        }

        /// <inheritdoc cref="List{T}.Find" />
        public T Find(Predicate<T> match)
        {
            using (LockObject.EnterReadLock())
                return _lstData.Find(match);
        }

        /// <inheritdoc cref="List{T}.Find" />
        public async ValueTask<T> FindAsync(Predicate<T> match, CancellationToken token = default)
        {
            using (await LockObject.EnterReadLockAsync(token).ConfigureAwait(false))
            {
                token.ThrowIfCancellationRequested();
                return _lstData.Find(match);
            }
        }

        /// <inheritdoc cref="List{T}.FindAll" />
        public List<T> FindAll(Predicate<T> match)
        {
            using (LockObject.EnterReadLock())
                return _lstData.FindAll(match);
        }

        /// <inheritdoc cref="List{T}.FindAll" />
        public async ValueTask<List<T>> FindAllAsync(Predicate<T> match, CancellationToken token = default)
        {
            using (await LockObject.EnterReadLockAsync(token).ConfigureAwait(false))
            {
                token.ThrowIfCancellationRequested();
                return _lstData.FindAll(match);
            }
        }

        /// <inheritdoc cref="List{T}.FindIndex(Predicate{T})" />
        public int FindIndex(Predicate<T> match)
        {
            using (LockObject.EnterReadLock())
                return _lstData.FindIndex(match);
        }

        /// <inheritdoc cref="List{T}.FindIndex(int, Predicate{T})" />
        public int FindIndex(int startIndex, Predicate<T> match)
        {
            using (LockObject.EnterReadLock())
                return _lstData.FindIndex(startIndex, match);
        }

        /// <inheritdoc cref="List{T}.FindIndex(int, int, Predicate{T})" />
        public int FindIndex(int startIndex, int count, Predicate<T> match)
        {
            using (LockObject.EnterReadLock())
                return _lstData.FindIndex(startIndex, count, match);
        }

        /// <inheritdoc cref="List{T}.FindIndex(Predicate{T})" />
        public async ValueTask<int> FindIndexAsync(Predicate<T> match, CancellationToken token = default)
        {
            using (await LockObject.EnterReadLockAsync(token).ConfigureAwait(false))
            {
                token.ThrowIfCancellationRequested();
                return _lstData.FindIndex(match);
            }
        }

        /// <inheritdoc cref="List{T}.FindIndex(int, Predicate{T})" />
        public async ValueTask<int> FindIndexAsync(int startIndex, Predicate<T> match, CancellationToken token = default)
        {
            using (await LockObject.EnterReadLockAsync(token).ConfigureAwait(false))
            {
                token.ThrowIfCancellationRequested();
                return _lstData.FindIndex(startIndex, match);
            }
        }

        /// <inheritdoc cref="List{T}.FindIndex(int, int, Predicate{T})" />
        public async ValueTask<int> FindIndexAsync(int startIndex, int count, Predicate<T> match, CancellationToken token = default)
        {
            using (await LockObject.EnterReadLockAsync(token).ConfigureAwait(false))
            {
                token.ThrowIfCancellationRequested();
                return _lstData.FindIndex(startIndex, count, match);
            }
        }

        /// <inheritdoc cref="List{T}.FindLast" />
        public T FindLast(Predicate<T> match)
        {
            using (LockObject.EnterReadLock())
                return _lstData.FindLast(match);
        }

        /// <inheritdoc cref="List{T}.FindLast" />
        public async ValueTask<T> FindLastAsync(Predicate<T> match, CancellationToken token = default)
        {
            using (await LockObject.EnterReadLockAsync(token).ConfigureAwait(false))
            {
                token.ThrowIfCancellationRequested();
                return _lstData.FindLast(match);
            }
        }

        /// <inheritdoc cref="List{T}.FindLastIndex(Predicate{T})" />
        public int FindLastIndex(Predicate<T> match)
        {
            using (LockObject.EnterReadLock())
                return _lstData.FindIndex(match);
        }

        /// <inheritdoc cref="List{T}.FindLastIndex(int, Predicate{T})" />
        public int FindLastIndex(int startIndex, Predicate<T> match)
        {
            using (LockObject.EnterReadLock())
                return _lstData.FindIndex(startIndex, match);
        }

        /// <inheritdoc cref="List{T}.FindLastIndex(int, int, Predicate{T})" />
        public int FindLastIndex(int startIndex, int count, Predicate<T> match)
        {
            using (LockObject.EnterReadLock())
                return _lstData.FindLastIndex(startIndex, count, match);
        }

        /// <inheritdoc cref="List{T}.FindLastIndex(Predicate{T})" />
        public async ValueTask<int> FindLastIndexAsync(Predicate<T> match, CancellationToken token = default)
        {
            using (await LockObject.EnterReadLockAsync(token).ConfigureAwait(false))
            {
                token.ThrowIfCancellationRequested();
                return _lstData.FindIndex(match);
            }
        }

        /// <inheritdoc cref="List{T}.FindLastIndex(int, Predicate{T})" />
        public async ValueTask<int> FindLastIndexAsync(int startIndex, Predicate<T> match, CancellationToken token = default)
        {
            using (await LockObject.EnterReadLockAsync(token).ConfigureAwait(false))
            {
                token.ThrowIfCancellationRequested();
                return _lstData.FindIndex(startIndex, match);
            }
        }

        /// <inheritdoc cref="List{T}.FindLastIndex(int, int, Predicate{T})" />
        public async ValueTask<int> FindLastIndexAsync(int startIndex, int count, Predicate<T> match, CancellationToken token = default)
        {
            using (await LockObject.EnterReadLockAsync(token).ConfigureAwait(false))
            {
                token.ThrowIfCancellationRequested();
                return _lstData.FindLastIndex(startIndex, count, match);
            }
        }

        /// <inheritdoc cref="List{T}.GetEnumerator" />
        public IEnumerator<T> GetEnumerator()
        {
            LockingEnumerator<T> objReturn = LockingEnumerator<T>.Get(this);
            objReturn.SetEnumerator(_lstData.GetEnumerator());
            return objReturn;
        }

        public async ValueTask<IEnumerator<T>> GetEnumeratorAsync(CancellationToken token = default)
        {
            LockingEnumerator<T> objReturn = await LockingEnumerator<T>.GetAsync(this, token).ConfigureAwait(false);
            objReturn.SetEnumerator(_lstData.GetEnumerator());
            return objReturn;
        }

        public int IndexOf(object value)
        {
            if (!(value is T objValue))
                return -1;
            return IndexOf(objValue);
        }

        /// <inheritdoc cref="List{T}.IndexOf(T)" />
        public int IndexOf(T item)
        {
            using (LockObject.EnterReadLock())
                return _lstData.IndexOf(item);
        }

        /// <inheritdoc cref="List{T}.IndexOf(T)" />
        public async ValueTask<int> IndexOfAsync(T item, CancellationToken token = default)
        {
            using (await LockObject.EnterReadLockAsync(token).ConfigureAwait(false))
            {
                token.ThrowIfCancellationRequested();
                return _lstData.IndexOf(item);
            }
        }

        public void Insert(int index, object value)
        {
            if (!(value is T objValue))
                return;
            Insert(index, objValue);
        }

        /// <inheritdoc cref="List{T}.Insert" />
        public void Insert(int index, T item)
        {
            using (LockObject.EnterWriteLock())
                _lstData.Insert(index, item);
        }

        /// <inheritdoc cref="List{T}.Insert" />
        public async ValueTask InsertAsync(int index, T item, CancellationToken token = default)
        {
            IAsyncDisposable objLocker = await LockObject.EnterWriteLockAsync(token).ConfigureAwait(false);
            try
            {
                token.ThrowIfCancellationRequested();
                _lstData.Insert(index, item);
            }
            finally
            {
                await objLocker.DisposeAsync().ConfigureAwait(false);
            }
        }

        /// <inheritdoc cref="List{T}.LastIndexOf(T)" />
        public int LastIndexOf(T item)
        {
            using (LockObject.EnterReadLock())
                return _lstData.LastIndexOf(item);
        }

        /// <inheritdoc cref="List{T}.LastIndexOf(T)" />
        public async ValueTask<int> LastIndexOfAsync(T item, CancellationToken token = default)
        {
            using (await LockObject.EnterReadLockAsync(token).ConfigureAwait(false))
            {
                token.ThrowIfCancellationRequested();
                return _lstData.LastIndexOf(item);
            }
        }

        public void Remove(object value)
        {
            if (!(value is T objValue))
                return;
            Remove(objValue);
        }

        /// <inheritdoc />
        public bool Remove(T item)
        {
            using (LockObject.EnterWriteLock())
                return _lstData.Remove(item);
        }

        public async ValueTask<bool> RemoveAsync(T item, CancellationToken token = default)
        {
            IAsyncDisposable objLocker = await LockObject.EnterWriteLockAsync(token).ConfigureAwait(false);
            try
            {
                token.ThrowIfCancellationRequested();
                return _lstData.Remove(item);
            }
            finally
            {
                await objLocker.DisposeAsync().ConfigureAwait(false);
            }
        }

        /// <inheritdoc cref="List{T}.RemoveAt" />
        public void RemoveAt(int index)
        {
            using (LockObject.EnterWriteLock())
                _lstData.RemoveAt(index);
        }

        /// <inheritdoc cref="List{T}.RemoveAt" />
        public async ValueTask RemoveAtAsync(int index, CancellationToken token = default)
        {
            IAsyncDisposable objLocker = await LockObject.EnterWriteLockAsync(token).ConfigureAwait(false);
            try
            {
                token.ThrowIfCancellationRequested();
                _lstData.RemoveAt(index);
            }
            finally
            {
                await objLocker.DisposeAsync().ConfigureAwait(false);
            }
        }

        /// <inheritdoc />
        public T[] ToArray()
        {
            using (LockObject.EnterReadLock())
                return _lstData.ToArray();
        }

        public async ValueTask<T[]> ToArrayAsync(CancellationToken token = default)
        {
            using (await LockObject.EnterReadLockAsync(token).ConfigureAwait(false))
            {
                token.ThrowIfCancellationRequested();
                return _lstData.ToArray();
            }
        }

        private int _intIsDisposed;

        public bool IsDisposed => _intIsDisposed > 0;

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (Interlocked.CompareExchange(ref _intIsDisposed, 1, 0) > 0)
                    return;
                LockObject.Dispose();
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual async ValueTask DisposeAsync(bool disposing)
        {
            if (disposing)
            {
                if (Interlocked.CompareExchange(ref _intIsDisposed, 1, 0) > 0)
                    return;
                await LockObject.DisposeAsync().ConfigureAwait(false);
            }
        }

        /// <inheritdoc />
        public async ValueTask DisposeAsync()
        {
            await DisposeAsync(true).ConfigureAwait(false);
            GC.SuppressFinalize(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public event EventHandler<RemovingOldEventArgs> BeforeRemove
        {
            add
            {
                using (LockObject.EnterWriteLock())
                    _lstData.BeforeRemove += value;
            }
            remove
            {
                using (LockObject.EnterWriteLock())
                    _lstData.BeforeRemove -= value;
            }
        }

        /// <inheritdoc cref="BindingList{T}.AddingNew" />
        public event AddingNewEventHandler AddingNew
        {
            add
            {
                using (LockObject.EnterWriteLock())
                    _lstData.AddingNew += value;
            }
            remove
            {
                using (LockObject.EnterWriteLock())
                    _lstData.AddingNew -= value;
            }
        }

        /// <inheritdoc />
        public ListSortDirection SortDirection => ListSortDirection.Ascending;

        /// <inheritdoc cref="BindingList{T}.ListChanged" />
        public event ListChangedEventHandler ListChanged
        {
            add
            {
                using (LockObject.EnterWriteLock())
                    _lstData.ListChanged += value;
            }
            remove
            {
                using (LockObject.EnterWriteLock())
                    _lstData.ListChanged -= value;
            }
        }

        /// <inheritdoc cref="BindingList{T}.RaiseListChangedEvents" />
        public bool RaiseListChangedEvents
        {
            get
            {
                using (LockObject.EnterReadLock())
                    return _lstData.RaiseListChangedEvents;
            }
            set
            {
                using (LockObject.EnterUpgradeableReadLock())
                {
                    if (_lstData.RaiseListChangedEvents.Equals(value))
                        return;
                    using (LockObject.EnterWriteLock())
                        _lstData.RaiseListChangedEvents = value;
                }
            }
        }

        /// <inheritdoc />
        public void AddIndex(PropertyDescriptor property)
        {
            //do nothing
        }

        /// <inheritdoc />
        public void ApplySort(PropertyDescriptor property, ListSortDirection direction)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc />
        public int Find(PropertyDescriptor property, object key)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc />
        public void RemoveIndex(PropertyDescriptor property)
        {
            //do nothing
        }

        /// <inheritdoc />
        public void RemoveSort()
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc cref="BindingList{T}.AllowNew" />
        public bool AllowNew
        {
            get
            {
                using (LockObject.EnterReadLock())
                    return _lstData.AllowNew;
            }
            set
            {
                using (LockObject.EnterUpgradeableReadLock())
                {
                    if (_lstData.AllowNew.Equals(value))
                        return;
                    using (LockObject.EnterWriteLock())
                        _lstData.AllowNew = value;
                }
            }
        }

        /// <inheritdoc cref="BindingList{T}.AllowEdit" />
        public bool AllowEdit
        {
            get
            {
                using (LockObject.EnterReadLock())
                    return _lstData.AllowEdit;
            }
            set
            {
                using (LockObject.EnterUpgradeableReadLock())
                {
                    if (_lstData.AllowEdit.Equals(value))
                        return;
                    using (LockObject.EnterWriteLock())
                        _lstData.AllowEdit = value;
                }
            }
        }

        /// <inheritdoc cref="BindingList{T}.AllowRemove" />
        public bool AllowRemove
        {
            get
            {
                using (LockObject.EnterReadLock())
                    return _lstData.AllowRemove;
            }
            set
            {
                using (LockObject.EnterUpgradeableReadLock())
                {
                    if (_lstData.AllowRemove.Equals(value))
                        return;
                    using (LockObject.EnterWriteLock())
                        _lstData.AllowRemove = value;
                }
            }
        }

        /// <inheritdoc />
        public bool SupportsChangeNotification => true;

        /// <inheritdoc />
        public bool SupportsSearching => false;

        /// <inheritdoc />
        public bool SupportsSorting => false;

        /// <inheritdoc />
        public bool IsSorted => false;

        /// <inheritdoc />
        public PropertyDescriptor SortProperty => null;

        /// <inheritdoc />
        public object AddNew()
        {
            using (LockObject.EnterWriteLock())
                return _lstData.AddNew();
        }

        /// <inheritdoc cref="BindingList{T}.AddNew()" />
        public async Task<object> AddNewAsync(CancellationToken token = default)
        {
            IAsyncDisposable objLocker = await LockObject.EnterWriteLockAsync(token).ConfigureAwait(false);
            try
            {
                token.ThrowIfCancellationRequested();
                return _lstData.AddNew();
            }
            finally
            {
                await objLocker.DisposeAsync().ConfigureAwait(false);
            }
        }

        /// <inheritdoc />
        public void CancelNew(int itemIndex)
        {
            using (LockObject.EnterWriteLock())
                _lstData.CancelNew(itemIndex);
        }

        /// <inheritdoc />
        public void EndNew(int itemIndex)
        {
            using (LockObject.EnterWriteLock())
                _lstData.EndNew(itemIndex);
        }

        /// <inheritdoc cref="BindingList{T}.ResetBindings" />
        public void ResetBindings()
        {
            using (LockObject.EnterWriteLock())
                _lstData.ResetBindings();
        }

        /// <inheritdoc cref="BindingList{T}.ResetBindings" />
        public async Task ResetBindingsAsync(CancellationToken token = default)
        {
            IAsyncDisposable objLocker = await LockObject.EnterWriteLockAsync(token).ConfigureAwait(false);
            try
            {
                token.ThrowIfCancellationRequested();
                _lstData.ResetBindings();
            }
            finally
            {
                await objLocker.DisposeAsync().ConfigureAwait(false);
            }
        }

        /// <inheritdoc cref="BindingList{T}.ResetItem" />
        public void ResetItem(int position)
        {
            using (LockObject.EnterWriteLock())
                _lstData.ResetItem(position);
        }

        /// <inheritdoc cref="BindingList{T}.ResetItem" />
        public async Task ResetItemAsync(int position, CancellationToken token = default)
        {
            IAsyncDisposable objLocker = await LockObject.EnterWriteLockAsync(token).ConfigureAwait(false);
            try
            {
                token.ThrowIfCancellationRequested();
                _lstData.ResetItem(position);
            }
            finally
            {
                await objLocker.DisposeAsync().ConfigureAwait(false);
            }
        }

        /// <inheritdoc />
        public bool RaisesItemChangedEvents => true;

        /// <summary>
        /// Sorts the elements in a range of elements in an ObservableCollection using the specified
        /// System.Collections.Generic.IComparer`1 generic interface.
        /// </summary>
        /// <param name="index">The starting index of the range to sort.</param>
        /// <param name="length">The number of elements in the range to sort.</param>
        /// <param name="objComparer">The System.Collections.Generic.IComparer`1 generic interface
        /// implementation to use when comparing elements, or null to use the System.IComparable`1 generic
        /// interface implementation of each element.</param>
        public void Sort(int index, int length, IComparer<T> objComparer = null)
        {
            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index));
            if (length < 0)
                throw new ArgumentOutOfRangeException(nameof(length));
            using (LockObject.EnterUpgradeableReadLock())
            {
                if (index + length > _lstData.Count)
                    throw new InvalidOperationException(nameof(length));
                if (length == 0)
                    return;
                IDisposable[] aobjLockers = _lstData[index] is IHasLockObject ? new IDisposable[length] : null;
                T[] aobjSorted = new T[length];
                for (int i = 0; i < length; ++i)
                {
                    T objLoop = _lstData[index + i];
                    aobjSorted[i] = objLoop;
                    if (aobjLockers != null)
                        aobjLockers[i] = (objLoop as IHasLockObject)?.LockObject.EnterHiPrioReadLock();
                }

                Array.Sort(aobjSorted, objComparer);

                if (aobjLockers != null)
                {
                    foreach (IDisposable objLocker in aobjLockers)
                    {
                        objLocker.Dispose();
                    }
                }

                if (!_lstData.RaiseListChangedEvents)
                {
                    using (LockObject.EnterWriteLock())
                    {
                        for (int i = 0; i < aobjSorted.Length; ++i)
                        {
                            _lstData[index + i] = aobjSorted[i];
                        }
                    }
                    return;
                }
                // If at least half of the list was changed, call a reset event instead of a large amount of ItemChanged events
                int intResetThreshold = aobjSorted.Length / 2;
                int intCountChanges = 0;
                // Not BitArray because read/write performance is much more important here than memory footprint
                bool[] ablnItemChanged = new bool[aobjSorted.Length];
                using (LockObject.EnterWriteLock())
                {
                    // We're going to disable events while we work with the list, then call them all at once at the end
                    _lstData.RaiseListChangedEvents = false;
                    try
                    {
                        for (int i = 0; i < aobjSorted.Length; ++i)
                        {
                            T objLoop = aobjSorted[i];
                            if (ReferenceEquals(objLoop, _lstData[index + i]))
                                continue;
                            ablnItemChanged[i] = true;
                            ++intCountChanges;
                            _lstData[index + i] = objLoop;
                        }
                    }
                    finally
                    {
                        _lstData.RaiseListChangedEvents = true;
                    }

                    if (intCountChanges >= intResetThreshold)
                    {
                        _lstData.ResetBindings();
                    }
                    else
                    {
                        for (int i = 0; i < ablnItemChanged.Length; ++i)
                        {
                            if (ablnItemChanged[i])
                                _lstData.ResetItem(index + i);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Sorts the elements in a range of elements in an ObservableCollection using the specified System.Comparison`1.
        /// </summary>
        /// <param name="funcComparison">The System.Comparison`1 to use when comparing elements.</param>
        public void Sort(Comparison<T> funcComparison)
        {
            if (funcComparison == null)
                throw new ArgumentNullException(nameof(funcComparison));
            using (LockObject.EnterUpgradeableReadLock())
            {
                if (_lstData.Count == 0)
                    return;
                IDisposable[] aobjLockers = _lstData[0] is IHasLockObject ? new IDisposable[_lstData.Count] : null;
                T[] aobjSorted = new T[_lstData.Count];
                for (int i = 0; i < _lstData.Count; ++i)
                {
                    T objLoop = _lstData[i];
                    aobjSorted[i] = objLoop;
                    if (aobjLockers != null)
                        aobjLockers[i] = (objLoop as IHasLockObject)?.LockObject.EnterHiPrioReadLock();
                }

                Array.Sort(aobjSorted, funcComparison);

                if (aobjLockers != null)
                {
                    foreach (IDisposable objLocker in aobjLockers)
                    {
                        objLocker.Dispose();
                    }
                }

                if (!_lstData.RaiseListChangedEvents)
                {
                    using (LockObject.EnterWriteLock())
                    {
                        for (int i = 0; i < aobjSorted.Length; ++i)
                        {
                            _lstData[i] = aobjSorted[i];
                        }
                    }
                    return;
                }
                // If at least half of the list was changed, call a reset event instead of a large amount of ItemChanged events
                int intResetThreshold = aobjSorted.Length / 2;
                int intCountChanges = 0;
                // Not BitArray because read/write performance is much more important here than memory footprint
                bool[] ablnItemChanged = new bool[aobjSorted.Length];
                using (LockObject.EnterWriteLock())
                {
                    // We're going to disable events while we work with the list, then call them all at once at the end
                    _lstData.RaiseListChangedEvents = false;
                    try
                    {
                        for (int i = 0; i < aobjSorted.Length; ++i)
                        {
                            T objLoop = aobjSorted[i];
                            if (ReferenceEquals(objLoop, _lstData[i]))
                                continue;
                            ablnItemChanged[i] = true;
                            ++intCountChanges;
                            _lstData[i] = objLoop;
                        }
                    }
                    finally
                    {
                        _lstData.RaiseListChangedEvents = true;
                    }

                    if (intCountChanges >= intResetThreshold)
                    {
                        _lstData.ResetBindings();
                    }
                    else
                    {
                        for (int i = 0; i < ablnItemChanged.Length; ++i)
                        {
                            if (ablnItemChanged[i])
                                _lstData.ResetItem(i);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Sorts the elements in a range of elements in an ObservableCollection using the specified
        /// System.Collections.Generic.IComparer`1 generic interface.
        /// </summary>
        /// <param name="objComparer">The System.Collections.Generic.IComparer`1 generic interface
        /// implementation to use when comparing elements, or null to use the System.IComparable`1 generic
        /// interface implementation of each element.</param>
        public void Sort(IComparer<T> objComparer = null)
        {
            using (LockObject.EnterUpgradeableReadLock())
            {
                if (_lstData.Count == 0)
                    return;
                IDisposable[] aobjLockers = _lstData[0] is IHasLockObject ? new IDisposable[_lstData.Count] : null;
                T[] aobjSorted = new T[_lstData.Count];
                for (int i = 0; i < _lstData.Count; ++i)
                {
                    T objLoop = _lstData[i];
                    aobjSorted[i] = objLoop;
                    if (aobjLockers != null)
                        aobjLockers[i] = (objLoop as IHasLockObject)?.LockObject.EnterHiPrioReadLock();
                }

                Array.Sort(aobjSorted, objComparer);

                if (aobjLockers != null)
                {
                    foreach (IDisposable objLocker in aobjLockers)
                    {
                        objLocker.Dispose();
                    }
                }

                if (!_lstData.RaiseListChangedEvents)
                {
                    using (LockObject.EnterWriteLock())
                    {
                        for (int i = 0; i < aobjSorted.Length; ++i)
                        {
                            _lstData[i] = aobjSorted[i];
                        }
                    }
                    return;
                }
                // If at least half of the list was changed, call a reset event instead of a large amount of ItemChanged events
                int intResetThreshold = aobjSorted.Length / 2;
                int intCountChanges = 0;
                // Not BitArray because read/write performance is much more important here than memory footprint
                bool[] ablnItemChanged = new bool[aobjSorted.Length];
                using (LockObject.EnterWriteLock())
                {
                    // We're going to disable events while we work with the list, then call them all at once at the end
                    _lstData.RaiseListChangedEvents = false;
                    try
                    {
                        for (int i = 0; i < aobjSorted.Length; ++i)
                        {
                            T objLoop = aobjSorted[i];
                            if (ReferenceEquals(objLoop, _lstData[i]))
                                continue;
                            ablnItemChanged[i] = true;
                            ++intCountChanges;
                            _lstData[i] = objLoop;
                        }
                    }
                    finally
                    {
                        _lstData.RaiseListChangedEvents = true;
                    }

                    if (intCountChanges >= intResetThreshold)
                    {
                        _lstData.ResetBindings();
                    }
                    else
                    {
                        for (int i = 0; i < ablnItemChanged.Length; ++i)
                        {
                            if (ablnItemChanged[i])
                                _lstData.ResetItem(i);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Sorts the elements in a range of elements in an ObservableCollection using the specified
        /// System.Collections.Generic.IComparer`1 generic interface.
        /// </summary>
        /// <param name="index">The starting index of the range to sort.</param>
        /// <param name="length">The number of elements in the range to sort.</param>
        /// <param name="objComparer">The System.Collections.Generic.IComparer`1 generic interface
        /// implementation to use when comparing elements, or null to use the System.IComparable`1 generic
        /// interface implementation of each element.</param>
        /// <param name="token">Cancellation token to listen to.</param>
        public async Task SortAsync(int index, int length, IComparer<T> objComparer = null, CancellationToken token = default)
        {
            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index));
            if (length < 0)
                throw new ArgumentOutOfRangeException(nameof(length));
            using (await LockObject.EnterUpgradeableReadLockAsync(token).ConfigureAwait(false))
            {
                token.ThrowIfCancellationRequested();
                if (index + length > _lstData.Count)
                    throw new InvalidOperationException(nameof(length));
                if (length == 0)
                    return;
                Stack<IAsyncDisposable> stkLockers = _lstData[0] is IHasLockObject ? new Stack<IAsyncDisposable>(length) : null;
                T[] aobjSorted = new T[length];
                try
                {
                    for (int i = 0; i < length; ++i)
                    {
                        token.ThrowIfCancellationRequested();
                        T objLoop = _lstData[index + i];
                        aobjSorted[i] = objLoop;
                        if (stkLockers != null && objLoop is IHasLockObject objLoopWithLocker)
                            stkLockers.Push(await objLoopWithLocker.LockObject.EnterHiPrioReadLockAsync(token).ConfigureAwait(false));
                    }

                    token.ThrowIfCancellationRequested();
                    Array.Sort(aobjSorted, objComparer);
                }
                finally
                {
                    if (stkLockers != null)
                    {
                        while (stkLockers.Count > 0)
                        {
                            IAsyncDisposable objTemp = stkLockers.Pop();
                            if (objTemp != null)
                                await objTemp.DisposeAsync().ConfigureAwait(false);
                        }
                    }
                }

                token.ThrowIfCancellationRequested();
                if (!_lstData.RaiseListChangedEvents)
                {
                    IAsyncDisposable objLocker2 = await LockObject.EnterWriteLockAsync(token).ConfigureAwait(false);
                    try
                    {
                        token.ThrowIfCancellationRequested();
                        for (int i = 0; i < aobjSorted.Length; ++i)
                        {
                            _lstData[index + i] = aobjSorted[i];
                        }
                    }
                    finally
                    {
                        await objLocker2.DisposeAsync().ConfigureAwait(false);
                    }
                    return;
                }
                // If at least half of the list was changed, call a reset event instead of a large amount of ItemChanged events
                int intResetThreshold = aobjSorted.Length / 2;
                int intCountChanges = 0;
                // Not BitArray because read/write performance is much more important here than memory footprint
                bool[] ablnItemChanged = new bool[aobjSorted.Length];
                IAsyncDisposable objLocker = await LockObject.EnterWriteLockAsync(token).ConfigureAwait(false);
                try
                {
                    token.ThrowIfCancellationRequested();
                    // We're going to disable events while we work with the list, then call them all at once at the end
                    _lstData.RaiseListChangedEvents = false;
                    try
                    {
                        for (int i = 0; i < aobjSorted.Length; ++i)
                        {
                            T objLoop = aobjSorted[i];
                            if (ReferenceEquals(objLoop, _lstData[index + i]))
                                continue;
                            ablnItemChanged[i] = true;
                            ++intCountChanges;
                            _lstData[index + i] = objLoop;
                        }
                    }
                    finally
                    {
                        _lstData.RaiseListChangedEvents = true;
                    }

                    if (intCountChanges >= intResetThreshold)
                    {
                        _lstData.ResetBindings();
                    }
                    else
                    {
                        for (int i = 0; i < ablnItemChanged.Length; ++i)
                        {
                            if (ablnItemChanged[i])
                                _lstData.ResetItem(index + i);
                        }
                    }
                }
                finally
                {
                    await objLocker.DisposeAsync().ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// Sorts the elements in a range of elements in an ObservableCollection using the specified System.Comparison`1.
        /// </summary>
        /// <param name="funcComparison">The System.Comparison`1 to use when comparing elements.</param>
        /// <param name="token">Cancellation token to listen to.</param>
        public async Task SortAsync(Comparison<T> funcComparison, CancellationToken token = default)
        {
            if (funcComparison == null)
                throw new ArgumentNullException(nameof(funcComparison));
            using (await LockObject.EnterUpgradeableReadLockAsync(token).ConfigureAwait(false))
            {
                token.ThrowIfCancellationRequested();
                if (_lstData.Count == 0)
                    return;
                Stack<IAsyncDisposable> stkLockers = _lstData[0] is IHasLockObject ? new Stack<IAsyncDisposable>(_lstData.Count) : null;
                T[] aobjSorted = new T[_lstData.Count];
                try
                {
                    for (int i = 0; i < _lstData.Count; ++i)
                    {
                        token.ThrowIfCancellationRequested();
                        T objLoop = _lstData[i];
                        aobjSorted[i] = objLoop;
                        if (stkLockers != null && objLoop is IHasLockObject objLoopWithLocker)
                            stkLockers.Push(await objLoopWithLocker.LockObject.EnterHiPrioReadLockAsync(token).ConfigureAwait(false));
                    }

                    token.ThrowIfCancellationRequested();
                    Array.Sort(aobjSorted, funcComparison);
                }
                finally
                {
                    if (stkLockers != null)
                    {
                        while (stkLockers.Count > 0)
                        {
                            IAsyncDisposable objTemp = stkLockers.Pop();
                            if (objTemp != null)
                                await objTemp.DisposeAsync().ConfigureAwait(false);
                        }
                    }
                }
                token.ThrowIfCancellationRequested();
                if (!_lstData.RaiseListChangedEvents)
                {
                    IAsyncDisposable objLocker2 = await LockObject.EnterWriteLockAsync(token).ConfigureAwait(false);
                    try
                    {
                        token.ThrowIfCancellationRequested();
                        for (int i = 0; i < aobjSorted.Length; ++i)
                        {
                            _lstData[i] = aobjSorted[i];
                        }
                    }
                    finally
                    {
                        await objLocker2.DisposeAsync().ConfigureAwait(false);
                    }
                    return;
                }
                // If at least half of the list was changed, call a reset event instead of a large amount of ItemChanged events
                int intResetThreshold = aobjSorted.Length / 2;
                int intCountChanges = 0;
                // Not BitArray because read/write performance is much more important here than memory footprint
                bool[] ablnItemChanged = new bool[aobjSorted.Length];
                IAsyncDisposable objLocker = await LockObject.EnterWriteLockAsync(token).ConfigureAwait(false);
                try
                {
                    token.ThrowIfCancellationRequested();
                    // We're going to disable events while we work with the list, then call them all at once at the end
                    _lstData.RaiseListChangedEvents = false;
                    try
                    {
                        for (int i = 0; i < aobjSorted.Length; ++i)
                        {
                            T objLoop = aobjSorted[i];
                            if (ReferenceEquals(objLoop, _lstData[i]))
                                continue;
                            ablnItemChanged[i] = true;
                            ++intCountChanges;
                            _lstData[i] = objLoop;
                        }
                    }
                    finally
                    {
                        _lstData.RaiseListChangedEvents = true;
                    }

                    if (intCountChanges >= intResetThreshold)
                    {
                        _lstData.ResetBindings();
                    }
                    else
                    {
                        for (int i = 0; i < ablnItemChanged.Length; ++i)
                        {
                            if (ablnItemChanged[i])
                                _lstData.ResetItem(i);
                        }
                    }
                }
                finally
                {
                    await objLocker.DisposeAsync().ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// Sorts the elements in a range of elements in an ObservableCollection using the specified
        /// System.Collections.Generic.IComparer`1 generic interface.
        /// </summary>
        /// <param name="objComparer">The System.Collections.Generic.IComparer`1 generic interface
        /// implementation to use when comparing elements, or null to use the System.IComparable`1 generic
        /// interface implementation of each element.</param>
        /// <param name="token">Cancellation token to listen to.</param>
        public async Task SortAsync(IComparer<T> objComparer = null, CancellationToken token = default)
        {
            using (await LockObject.EnterUpgradeableReadLockAsync(token).ConfigureAwait(false))
            {
                token.ThrowIfCancellationRequested();
                if (_lstData.Count == 0)
                    return;
                Stack<IAsyncDisposable> stkLockers = _lstData[0] is IHasLockObject ? new Stack<IAsyncDisposable>(_lstData.Count) : null;
                T[] aobjSorted = new T[_lstData.Count];
                try
                {
                    for (int i = 0; i < _lstData.Count; ++i)
                    {
                        token.ThrowIfCancellationRequested();
                        T objLoop = _lstData[i];
                        aobjSorted[i] = objLoop;
                        if (stkLockers != null && objLoop is IHasLockObject objLoopWithLocker)
                            stkLockers.Push(await objLoopWithLocker.LockObject.EnterHiPrioReadLockAsync(token).ConfigureAwait(false));
                    }

                    token.ThrowIfCancellationRequested();
                    Array.Sort(aobjSorted, objComparer);
                }
                finally
                {
                    if (stkLockers != null)
                    {
                        while (stkLockers.Count > 0)
                        {
                            IAsyncDisposable objTemp = stkLockers.Pop();
                            if (objTemp != null)
                                await objTemp.DisposeAsync().ConfigureAwait(false);
                        }
                    }
                }
                token.ThrowIfCancellationRequested();
                if (!_lstData.RaiseListChangedEvents)
                {
                    IAsyncDisposable objLocker2 = await LockObject.EnterWriteLockAsync(token).ConfigureAwait(false);
                    try
                    {
                        token.ThrowIfCancellationRequested();
                        for (int i = 0; i < aobjSorted.Length; ++i)
                        {
                            _lstData[i] = aobjSorted[i];
                        }
                    }
                    finally
                    {
                        await objLocker2.DisposeAsync().ConfigureAwait(false);
                    }
                    return;
                }
                // If at least half of the list was changed, call a reset event instead of a large amount of ItemChanged events
                int intResetThreshold = aobjSorted.Length / 2;
                int intCountChanges = 0;
                // Not BitArray because read/write performance is much more important here than memory footprint
                bool[] ablnItemChanged = new bool[aobjSorted.Length];
                IAsyncDisposable objLocker = await LockObject.EnterWriteLockAsync(token).ConfigureAwait(false);
                try
                {
                    token.ThrowIfCancellationRequested();
                    // We're going to disable events while we work with the list, then call them all at once at the end
                    _lstData.RaiseListChangedEvents = false;
                    try
                    {
                        for (int i = 0; i < aobjSorted.Length; ++i)
                        {
                            T objLoop = aobjSorted[i];
                            if (ReferenceEquals(objLoop, _lstData[i]))
                                continue;
                            ablnItemChanged[i] = true;
                            ++intCountChanges;
                            _lstData[i] = objLoop;
                        }
                    }
                    finally
                    {
                        _lstData.RaiseListChangedEvents = true;
                    }

                    if (intCountChanges >= intResetThreshold)
                    {
                        _lstData.ResetBindings();
                    }
                    else
                    {
                        for (int i = 0; i < ablnItemChanged.Length; ++i)
                        {
                            if (ablnItemChanged[i])
                                _lstData.ResetItem(i);
                        }
                    }
                }
                finally
                {
                    await objLocker.DisposeAsync().ConfigureAwait(false);
                }
            }
        }

        public void Move(int intOldIndex, int intNewIndex, CancellationToken token = default)
        {
            using (LockObject.EnterWriteLock(token))
            {
                bool blnOldRaiseListChangedEvents = _lstData.RaiseListChangedEvents;
                try
                {
                    _lstData.RaiseListChangedEvents = false;
                    int intParity = intOldIndex < intNewIndex ? 1 : -1;
                    for (int i = intOldIndex; i != intNewIndex; i += intParity)
                    {
                        token.ThrowIfCancellationRequested();
                        (_lstData[intOldIndex + intParity], _lstData[intOldIndex]) = (
                            _lstData[intOldIndex], _lstData[intOldIndex + intParity]);
                    }
                }
                finally
                {
                    _lstData.RaiseListChangedEvents = blnOldRaiseListChangedEvents;
                }
            }
        }

        public async Task MoveAsync(int intOldIndex, int intNewIndex, CancellationToken token = default)
        {
            IAsyncDisposable objLocker = await LockObject.EnterWriteLockAsync(token).ConfigureAwait(false);
            try
            {
                token.ThrowIfCancellationRequested();
                bool blnOldRaiseListChangedEvents = _lstData.RaiseListChangedEvents;
                try
                {
                    _lstData.RaiseListChangedEvents = false;
                    int intParity = intOldIndex < intNewIndex ? 1 : -1;
                    for (int i = intOldIndex; i != intNewIndex; i += intParity)
                    {
                        token.ThrowIfCancellationRequested();
                        (_lstData[intOldIndex + intParity], _lstData[intOldIndex]) = (
                            _lstData[intOldIndex], _lstData[intOldIndex + intParity]);
                    }
                }
                finally
                {
                    _lstData.RaiseListChangedEvents = blnOldRaiseListChangedEvents;
                }
            }
            finally
            {
                await objLocker.DisposeAsync().ConfigureAwait(false);
            }
        }
    }
}
