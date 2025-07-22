namespace Tickle.Collections
{
    public unsafe struct LinkedNode<T> where T : unmanaged
    {
        public T Value { get; private set; }
        public LinkedNode<T>* Next { get; private set; }
        public LinkedNode<T>* Previous { get; private set; }

        public LinkedNode(T value)
        {
            Value = value;
            Next = null;
            Previous = null;
        }

        public LinkedNode(T value, LinkedNode<T>* next, LinkedNode<T>* previous)
        {
            Value = value;
            Next = next;
            Previous = previous;
        }

        public static void AddAfter(LinkedNode<T>* self, LinkedNode<T>* newNext)
        {
            if (newNext == null) return;
            newNext->Previous = self;
            newNext->Next = self->Next;
            if (self->Next != null)
                self->Next->Previous = newNext;
            self->Next = newNext;
        }

        public static void Remove(LinkedNode<T>* self)
        {
            if (self == null) return;
            LinkedNode<T>* previous = self->Previous;
            LinkedNode<T>* next = self->Next;
            if (previous != null)
                previous->Next = next;
            if (next != null)
                next->Previous = previous;
            self->Next = null;
            self->Previous = null;
        }
    }
}