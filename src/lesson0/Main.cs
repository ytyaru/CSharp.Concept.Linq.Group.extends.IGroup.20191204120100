using System;
using System.Collections.Generic;
using System.Linq;

namespace Concept.Linq.Lesson0 {
    public static class MyExtensions {
        public static IEnumerable<IGrouping<TKey, TSource>> ChunkBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector) {
            return source.ChunkBy(keySelector, EqualityComparer<TKey>.Default);
        }
        public static IEnumerable<IGrouping<TKey, TSource>> ChunkBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, IEqualityComparer<TKey> comparer)
        {
            const bool noMoreSourceElements = true;
            var enumerator = source.GetEnumerator();
            if (!enumerator.MoveNext()) yield break;
            Chunk<TKey, TSource> current = null;
            while (true) {
                var key = keySelector(enumerator.Current);
                current = new Chunk<TKey, TSource>(key, enumerator, value => comparer.Equals(key, keySelector(value)));
                yield return current;
                if (current.CopyAllChunkElements() == noMoreSourceElements) { yield break; }
            }
        }
        class Chunk<TKey, TSource> : IGrouping<TKey, TSource> {
            class ChunkItem {
                public ChunkItem(TSource value) { Value = value; }
                public readonly TSource Value;
                public ChunkItem Next = null;
            }
            private readonly TKey key;
            private IEnumerator<TSource> enumerator;
            private Func<TSource, bool> predicate;
            private readonly ChunkItem head;
            private ChunkItem tail;
            internal bool isLastSourceElement = false;
            private object m_Lock;
            public Chunk(TKey key, IEnumerator<TSource> enumerator, Func<TSource, bool> predicate) {
                this.key = key;
                this.enumerator = enumerator;
                this.predicate = predicate;
                head = new ChunkItem(enumerator.Current);
                tail = head;
                m_Lock = new object();
            }
            private bool DoneCopyingChunk => tail == null;
            private void CopyNextChunkElement() {
                isLastSourceElement = !enumerator.MoveNext();
                if (isLastSourceElement || !predicate(enumerator.Current)) {
                    enumerator = null;
                    predicate = null;
                }
                else { tail.Next = new ChunkItem(enumerator.Current); }
                tail = tail.Next;
            }
            internal bool CopyAllChunkElements() {
                while (true) {
                    lock (m_Lock) {
                        if (DoneCopyingChunk) { return isLastSourceElement; }
                        else { CopyNextChunkElement(); }
                    }
                }
            }
            public TKey Key => key;
            public IEnumerator<TSource> GetEnumerator() {
                ChunkItem current = head;
                while (current != null) {
                    yield return current.Value;
                    lock (m_Lock) {
                        if (current == tail) { CopyNextChunkElement(); }
                    }
                    current = current.Next;
                }
            }
            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
    public class KeyValPair
    {
        public string Key { get; set; }
        public string Value { get; set; }
    }
    class Main {
        public void Run() {
            var s = CreateSource();
            IEnumerable<IGrouping<string, KeyValPair>> query = s.ChunkBy(p => p.Key);
            Show(query);
        }
        private List<KeyValPair> CreateSource() {
            return new List<KeyValPair>() {
                new KeyValPair{ Key = "A", Value = "We" },
                new KeyValPair{ Key = "A", Value = "think" },
                new KeyValPair{ Key = "A", Value = "that" },
                new KeyValPair{ Key = "B", Value = "Linq" },
                new KeyValPair{ Key = "C", Value = "is" },
                new KeyValPair{ Key = "A", Value = "really" },
                new KeyValPair{ Key = "B", Value = "cool" },
                new KeyValPair{ Key = "B", Value = "!" }
            };
        }
        private void Show(in IEnumerable<IGrouping<string, KeyValPair>> query) {
            foreach (var item in query) {
                Console.WriteLine($"Key={item.Key}");
                foreach (var inner in item) {
                    Console.WriteLine($"  {inner.Value}");
                }
            }
        }
    }
}
