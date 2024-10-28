/// <summary>
/// Conceptually works similar to an inverse semaphore, each time it is locked a counter is incremented (release to decrement).
/// The boolean conversion will only yield true if the counter is 0, it is true by default
/// </summary>
public class Restriction
{
    private int _counter = 0;

    public int Counter {
        get => _counter;
        private set
        {
            if (value < 0)
                throw new System.Exception("Trying to call release on an empty restriction");
            _counter = value;
        }
    }

    public static implicit operator bool (Restriction r) => r.Counter == 0;

    public void Lock() => Counter++;

    public void Release() => Counter--;
}
