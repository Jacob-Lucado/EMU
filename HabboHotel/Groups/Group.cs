using Dapper;
using Plus.HabboHotel.Rooms;

namespace Plus.HabboHotel.Groups;

public sealed record GroupAppearance(string Badge, int Colour1, int Colour2);

public class Group
{
    private static readonly GroupAppearance EmptyAppearance = new("", 0, 0);
    private volatile GroupAppearance? _appearance;
    private readonly List<int> _administrators;
    private readonly List<int> _members;
    private readonly List<int> _requests;

    public bool HasForum;

    public Group(int id, string name, string description, string badge, uint roomId, int owner,
        DateTimeOffset? createdAt, int type, int colour1, int colour2, int adminOnlyDeco, bool hasForum,
        GroupMembershipSnapshot membership)
    {
        Id = id;
        Name = name;
        Description = description;
        RoomId = roomId;
        _appearance = new(badge, colour1 == 0 ? 1 : colour1, colour2 == 0 ? 1 : colour2);
        CreatedAt = createdAt?.ToUniversalTime();
        CreatorId = owner;
        HasForum = hasForum;
        Type = (GroupType)type;
        AdminOnlyDeco = adminOnlyDeco;
        ForumEnabled = hasForum;
        _members = membership.Members.ToList();
        _requests = membership.Requests.ToList();
        _administrators = membership.Administrators.ToList();
    }

    public int Id { get; set; }
    public string Name { get; set; }
    public int AdminOnlyDeco { get; set; }
    public GroupAppearance Appearance => _appearance ?? EmptyAppearance;
    public string Badge { get => Appearance.Badge; set => _appearance = Appearance with { Badge = value }; }
    public DateTimeOffset? CreatedAt { get; set; }
    public int CreatorId { get; set; }
    public string Description { get; set; }
    public uint RoomId { get; set; }
    public int Colour1 { get => Appearance.Colour1; set => SetColours(value, Colour2); }
    public int Colour2 { get => Appearance.Colour2; set => SetColours(Colour1, value); }
    public void SetColours(int primary, int secondary) => _appearance = Appearance with { Colour1 = primary, Colour2 = secondary };
    public bool ForumEnabled { get; set; }
    public GroupType Type { get; set; }

    public List<int> GetMembers => _members.ToList();

    public List<int> GetRequests => _requests.ToList();

    public List<int> GetAdministrators => _administrators.ToList();

    public List<int> GetAllMembers
    {
        get
        {
            var members = new List<int>(_administrators.ToList());
            members.AddRange(_members.ToList());

            return members;
        }
    }

    public int MemberCount => _members.Count + _administrators.Count;

    public int RequestCount => _requests.Count;

    public bool IsMember(int id) => _members.Contains(id) || _administrators.Contains(id);

    public bool IsAdmin(int id) => _administrators.Contains(id);

    public bool HasRequest(int id) => _requests.Contains(id);

    public void MakeAdmin(int id)
    {
        if (_members.Contains(id)) {
            _members.Remove(id);
        }

        if (!_administrators.Contains(id)) {
            _administrators.Add(id);
        }
    }

    public void TakeAdmin(int userId)
    {
        if (!_administrators.Contains(userId)) {
            return;
        }

        _administrators.Remove(userId);

        if (!_members.Contains(userId)) {
            _members.Add(userId);
        }
    }

    // Memory publication only. Callers persist the membership or request before publishing it.
    public void PublishJoin(int id)
    {
        if (IsMember(id) || Type == GroupType.Locked && _requests.Contains(id)) {
            return;
        }

        if (IsAdmin(id)) {
            _administrators.Remove(id);
            _members.Add(id);
        }
        else if (Type == GroupType.Locked) {
            _requests.Add(id);
        }
        else {
            _members.Add(id);
        }
    }

    public void DeleteMember(int id)
    {
        _members.Remove(id);
        _administrators.Remove(id);
    }

    public void HandleRequest(int id, bool accepted)
    {
        if (accepted) {
            if (!_members.Contains(id)) {
                _members.Add(id);
            }
        }

        if (_requests.Contains(id)) {
            _requests.Remove(id);
        }
    }

    public void ClearRequests()
    {
        _requests.Clear();
    }

    public void Dispose()
    {
        _requests.Clear();
        _members.Clear();
        _administrators.Clear();
    }

}
