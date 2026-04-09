namespace WhereToStayInJapan.Domain.Models;

public class TravelTimeMatrix
{
    private readonly Dictionary<Guid, Dictionary<string, int?>> _data = new();

    public void Set(Guid candidateId, string destinationName, int? durationMins)
    {
        if (!_data.ContainsKey(candidateId))
            _data[candidateId] = new Dictionary<string, int?>();
        _data[candidateId][destinationName] = durationMins;
    }

    public int? Get(Guid candidateId, string destinationName)
    {
        return _data.TryGetValue(candidateId, out var dest)
            && dest.TryGetValue(destinationName, out var val)
            ? val
            : null;
    }

    public double? GetAverage(Guid candidateId)
    {
        if (!_data.TryGetValue(candidateId, out var dest))
            return null;

        var nonNull = dest.Values.Where(v => v.HasValue).Select(v => (double)v!.Value).ToList();
        return nonNull.Count > 0 ? nonNull.Average() : null;
    }
}
