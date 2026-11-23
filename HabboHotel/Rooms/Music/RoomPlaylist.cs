namespace Plus.HabboHotel.Rooms.Music;

// The songs a room plays, one after another. The token changes with every song, so a late "song ended" report for an older song is ignored.
public sealed class RoomPlaylist
{
    public const int MaxQueued = 20;
    public const int MaxSuggestions = 10;

    // A client cannot end a song before it has had time to start playing.
    private static readonly TimeSpan MinimumPlayTime = TimeSpan.FromSeconds(5);

    private readonly object _lock = new();
    private readonly List<RoomStream> _queue = [];
    private readonly List<RoomStream> _suggestions = [];
    private DateTimeOffset _startedAt;

    public RoomStream? Current { get; private set; }

    public int Token { get; private set; }

    public int Queued
    {
        get
        {
            lock (_lock) {
                return _queue.Count;
            }
        }
    }

    public IReadOnlyList<RoomStream> Upcoming
    {
        get
        {
            lock (_lock) {
                return [.. _queue];
            }
        }
    }

    public IReadOnlyList<RoomStream> Suggestions
    {
        get
        {
            lock (_lock) {
                return [.. _suggestions];
            }
        }
    }

    // Keeps a song someone without rights suggested. A person has one suggestion at a time, so a new one replaces the old one.
    public bool Suggest(RoomStream stream)
    {
        lock (_lock) {
            var replaced = _suggestions.RemoveAll(s => string.Equals(s.SetBy, stream.SetBy, StringComparison.OrdinalIgnoreCase)) > 0;

            if (!replaced && _suggestions.Count >= MaxSuggestions) {
                return false;
            }

            _suggestions.Add(stream);

            return true;
        }
    }

    // Takes a suggestion out of the list: the one from the named person, or the oldest when no name is given.
    public RoomStream? TakeSuggestion(string? by)
    {
        lock (_lock) {
            var index = string.IsNullOrWhiteSpace(by)
                ? (_suggestions.Count > 0 ? 0 : -1)
                : _suggestions.FindIndex(s => string.Equals(s.SetBy, by.Trim(), StringComparison.OrdinalIgnoreCase));

            if (index < 0) {
                return null;
            }

            var found = _suggestions[index];
            _suggestions.RemoveAt(index);

            return found;
        }
    }

    public void ClearSuggestions()
    {
        lock (_lock) {
            _suggestions.Clear();
        }
    }

    // Plays the song now. Songs already waiting stay in line.
    public void Play(RoomStream stream, DateTimeOffset now)
    {
        lock (_lock) {
            Start(stream, now);
        }
    }

    // Starts the song when nothing plays, otherwise puts it at the end of the line. Returns false when the line is full.
    public bool Add(RoomStream stream, DateTimeOffset now, out bool startedNow)
    {
        lock (_lock) {
            startedNow = Current == null;

            if (startedNow) {
                Start(stream, now);

                return true;
            }

            if (_queue.Count >= MaxQueued) {
                return false;
            }

            _queue.Add(stream);

            return true;
        }
    }

    // Moves on to the next song, or stops when none is waiting.
    public void Skip(DateTimeOffset now)
    {
        lock (_lock) {
            Advance(now);
        }
    }

    // A client says the song with this token has ended. Only the current song, after its minimum play time, moves the line on.
    public bool Ended(int token, DateTimeOffset now)
    {
        lock (_lock) {
            if (Current == null || token != Token || now - _startedAt < MinimumPlayTime) {
                return false;
            }

            Advance(now);

            return true;
        }
    }

    public void ClearQueue()
    {
        lock (_lock) {
            _queue.Clear();
        }
    }

    // Turns the music off and empties the line.
    public void Stop()
    {
        lock (_lock) {
            _queue.Clear();
            Current = null;
            Token++;
        }
    }

    private void Advance(DateTimeOffset now)
    {
        if (_queue.Count == 0) {
            Current = null;
            Token++;

            return;
        }

        var next = _queue[0];
        _queue.RemoveAt(0);
        Start(next, now);
    }

    private void Start(RoomStream stream, DateTimeOffset now)
    {
        Current = stream;
        Token++;
        _startedAt = now;
    }
}
