using System.Collections.Generic;

// Union-find over troop ids (spec §3.4.1 "no loops"). Built from the current links each frame; a new link between two
// troops already in one chain would close a loop, so it is rejected. Pure C#.
public sealed class ChainTopology
{
    readonly Dictionary<int, int> _parent = new();

    public void Clear() => _parent.Clear();

    public int Find(int id)
    {
        if (!_parent.TryGetValue(id, out int parent))
        {
            _parent[id] = id;
            return id;
        }
        if (parent == id) return id;
        int root = Find(parent);
        _parent[id] = root;
        return root;
    }

    public void Union(int a, int b)
    {
        int rootA = Find(a);
        int rootB = Find(b);
        if (rootA == rootB) return;
        // Deterministic: the lower root wins.
        if (rootA < rootB) _parent[rootB] = rootA;
        else _parent[rootA] = rootB;
    }

    public bool SameChain(int a, int b) => Find(a) == Find(b);

    // True when linking a and b keeps every chain an open path (no loop).
    public bool CanLink(int a, int b) => a != b && !SameChain(a, b);
}
