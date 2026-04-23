using Epsilon.Content;

namespace Epsilon.CoreGame;

internal static class RoomNavigationLogic
{
    public static bool IsWalkable(
        Rooms.RoomLayoutDefinition layout,
        int x,
        int y,
        out double height)
    {
        string[] rows = layout.Heightmap.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        height = 0;

        if (y < 0 || y >= rows.Length)
        {
            return false;
        }

        string row = rows[y];
        if (x < 0 || x >= row.Length)
        {
            return false;
        }

        char tile = row[x];
        if (tile == 'x' || tile == 'X')
        {
            return false;
        }

        if (char.IsDigit(tile))
        {
            height = tile - '0';
            return true;
        }

        if (tile is 'a' or 'b' or 'c' or 'd' or 'e' or 'f')
        {
            height = 10 + (tile - 'a');
            return true;
        }

        if (tile is 'A' or 'B' or 'C' or 'D' or 'E' or 'F')
        {
            height = 10 + (tile - 'A');
            return true;
        }

        if (tile == '-')
        {
            height = 0;
            return true;
        }

        return false;
    }

    public static bool IsTileBlocked(
        IReadOnlyList<RoomActorState> actors,
        IReadOnlyDictionary<ItemId, RoomItemSnapshot> itemSnapshots,
        RoomActorKind actorKind,
        long actorId,
        int x,
        int y)
    {
        bool occupiedByOtherActor = actors.Any(other =>
            (other.ActorKind != actorKind || other.ActorId != actorId) &&
            other.Position.X == x &&
            other.Position.Y == y);

        if (occupiedByOtherActor)
        {
            return true;
        }

        return itemSnapshots.Values.Any(item =>
            item.Item.Placement.FloorPosition is { } floorPosition &&
            floorPosition.X == x &&
            floorPosition.Y == y &&
            !(item.Definition?.IsWalkable ?? false));
    }

    public static IReadOnlyList<RoomCoordinate>? FindPath(
        RoomHotelSnapshot room,
        IReadOnlyList<RoomActorState> actors,
        RoomActorState actor,
        int destinationX,
        int destinationY)
    {
        if (room.Layout is null)
        {
            return null;
        }

        if (actor.Position.X == destinationX && actor.Position.Y == destinationY)
        {
            return [actor.Position];
        }

        Queue<RoomCoordinate> frontier = new();
        Dictionary<(int X, int Y), (int X, int Y)?> previous = [];
        Dictionary<(int X, int Y), RoomCoordinate> coordinates = [];
        Dictionary<ItemId, RoomItemSnapshot> itemSnapshots = room.Items
            .GroupBy(snapshot => snapshot.Item.ItemId)
            .ToDictionary(group => group.Key, group => group.Last());

        frontier.Enqueue(actor.Position);
        previous[(actor.Position.X, actor.Position.Y)] = null;
        coordinates[(actor.Position.X, actor.Position.Y)] = actor.Position;

        while (frontier.Count > 0)
        {
            RoomCoordinate current = frontier.Dequeue();

            foreach ((int nextX, int nextY) in EnumerateNeighborTiles(current.X, current.Y))
            {
                if (previous.ContainsKey((nextX, nextY)))
                {
                    continue;
                }

                if (!IsWalkable(room.Layout, nextX, nextY, out double nextHeight))
                {
                    continue;
                }

                if ((nextX != destinationX || nextY != destinationY) &&
                    IsTileBlocked(actors, itemSnapshots, actor.ActorKind, actor.ActorId, nextX, nextY))
                {
                    continue;
                }

                RoomCoordinate next = new(nextX, nextY, nextHeight);
                previous[(nextX, nextY)] = (current.X, current.Y);
                coordinates[(nextX, nextY)] = next;

                if (nextX == destinationX && nextY == destinationY)
                {
                    return ReconstructPath(previous, coordinates, nextX, nextY);
                }

                frontier.Enqueue(next);
            }
        }

        return null;
    }

    public static double ResolveStandingHeight(
        int x,
        int y,
        double floorHeight,
        IEnumerable<RoomItemSnapshot> items)
    {
        double height = floorHeight;

        foreach (RoomItemSnapshot item in items)
        {
            if (item.Item.Placement.FloorPosition is not { } floorPosition ||
                floorPosition.X != x ||
                floorPosition.Y != y ||
                !(item.Definition?.IsWalkable ?? false))
            {
                continue;
            }

            height = Math.Max(height, floorPosition.Z + item.Definition!.StackHeight);
        }

        return height;
    }

    public static int ResolveMovementRotation(RoomCoordinate from, RoomCoordinate to)
    {
        int deltaX = Math.Sign(to.X - from.X);
        int deltaY = Math.Sign(to.Y - from.Y);

        return (deltaX, deltaY) switch
        {
            (0, -1) => 0,
            (1, -1) => 1,
            (1, 0) => 2,
            (1, 1) => 3,
            (0, 1) => 4,
            (-1, 1) => 5,
            (-1, 0) => 6,
            (-1, -1) => 7,
            _ => 0
        };
    }

    public static bool TryResolveRollerStep(int rotation, out int deltaX, out int deltaY)
    {
        (deltaX, deltaY) = rotation switch
        {
            0 => (0, -1),
            2 => (1, 0),
            4 => (0, 1),
            6 => (-1, 0),
            _ => (0, 0)
        };

        return deltaX != 0 || deltaY != 0;
    }

    private static IReadOnlyList<RoomCoordinate> ReconstructPath(
        Dictionary<(int X, int Y), (int X, int Y)?> previous,
        Dictionary<(int X, int Y), RoomCoordinate> coordinates,
        int destinationX,
        int destinationY)
    {
        List<RoomCoordinate> path = [];
        (int X, int Y)? current = (destinationX, destinationY);

        while (current is not null)
        {
            path.Add(coordinates[current.Value]);
            current = previous[current.Value];
        }

        path.Reverse();
        return path;
    }

    private static IEnumerable<(int X, int Y)> EnumerateNeighborTiles(int x, int y)
    {
        for (int offsetY = -1; offsetY <= 1; offsetY++)
        {
            for (int offsetX = -1; offsetX <= 1; offsetX++)
            {
                if (offsetX == 0 && offsetY == 0)
                {
                    continue;
                }

                yield return (x + offsetX, y + offsetY);
            }
        }
    }
}
